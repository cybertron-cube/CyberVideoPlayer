using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CyberPlayer.Player.AppSettings;
using Cybertron;
using Serilog;
using CyberPlayer.Player.Helpers;
using Serilog.Core;
using static Cybertron.TimeCode;

namespace CyberPlayer.Player.Business;

public class FFmpeg : IDisposable
{
    public const string ProbeFormatArgs = "-v quiet -show_format -show_streams -print_format ";
    public event Action<double>? ProgressChanged;
    
    private readonly Logger _log;
    private readonly Settings _settings;
    private readonly string _videoPath;
    private string? _lastStdErrLine;
    private readonly CustomProcess _ffmpegProcess;
    private readonly CustomProcess _ffprobeProcess;
    private double _startTimeMs = double.NaN;
    private double _endTimeMs = double.NaN;
    private double _spanTimeMs = double.NaN;

    public FFmpeg(string videoPath, Settings settings)
    {
        _videoPath = videoPath;
        _settings = settings;
        
        var timeStamp = DateTime.Now.ToString(LogHelper.DateTimeFormat);
        var filePath = Path.Combine(BuildConfig.LogDirectory, $"ffmpeg_output_{timeStamp}.log");
        _log = new LoggerConfiguration()
            .ConfigureDefaults(filePath, settings.LogLevel)
            .CreateLogger();
        _log.CleanupLogFiles(BuildConfig.LogDirectory, "ffmpeg_output*.log", 10);
        
        var ffmpegPath = string.IsNullOrWhiteSpace(settings.FFmpegDir) ?
            GenStatic.GetFullPathFromRelative(Path.Combine("ffmpeg", "ffmpeg"))
            : Path.Combine(settings.FFmpegDir, "ffmpeg");
        GenStatic.GetOSRespectiveExecutablePath(ref ffmpegPath);
        _log.Information("Using ffmpeg path {Path}", ffmpegPath);
        
        _ffmpegProcess = new CustomProcess
        {
            EnableRaisingEvents = true,
            StartInfo = new ProcessStartInfo(ffmpegPath)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            }
        };
        
        _ffmpegProcess.OutputDataReceived += FFmpegProcessOnOutputDataReceived;
        _ffmpegProcess.ErrorDataReceived += FFmpegProcessOnErrorDataReceived;

        var ffprobePath = string.IsNullOrWhiteSpace(settings.FFprobeDir) ?
            GenStatic.GetFullPathFromRelative(Path.Combine("ffmpeg", "ffprobe"))
            : Path.Combine(settings.FFprobeDir, "ffprobe");
        GenStatic.GetOSRespectiveExecutablePath(ref ffprobePath);
        _log.Information("Using ffprobe path {Path}", ffprobePath);
        
        _ffprobeProcess = new CustomProcess
        {
            EnableRaisingEvents = true,
            StartInfo = new ProcessStartInfo(ffprobePath)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            }
        };
    }

    public readonly record struct FFmpegResult(int ExitCode, string? ErrorMessage = null);

    public void Transcode(string extension)
    {
        throw new NotImplementedException();
    }
    
    public async Task<FFmpegResult> TrimAsync(TimeCode startTime, TimeCode endTime, CancellationToken ct) =>
        await FFmpegCommandAsync(startTime, endTime,
            "trim",
            $"-ss {startTime.FormattedString} -to {endTime.FormattedString} -i \"{_videoPath}\" -map 0 -codec copy {_settings.ExtraTrimArgs} \"{GenStatic.AppendFileName(_videoPath, "-trim")}\"",
            ct);

    public async Task<FFmpegResult> FFmpegCommandAsync(TimeCode startTime, TimeCode endTime, string commandName, string args, CancellationToken ct)
    {
        _startTimeMs = startTime.GetExactUnits(TimeUnit.Millisecond);
        _endTimeMs = endTime.GetExactUnits(TimeUnit.Millisecond);
        _spanTimeMs = _endTimeMs - _startTimeMs;

        SetArgs(args);
        
        _log.Information("Starting {CommandName} of video {VideoPath} from {StartTime} to {EndTime}",
            commandName, _videoPath, startTime.FormattedString, endTime.FormattedString);
        
        _ffmpegProcess.Start();
        _ffmpegProcess.BeginOutputReadLine();
        _ffmpegProcess.BeginErrorReadLine();
        try
        {
            await _ffmpegProcess.WaitForExitAsync(ct);
        }
        catch (TaskCanceledException)
        {
            _log.Information($"{commandName} canceled");
            await _ffmpegProcess.StandardInput.WriteAsync('q');
            await _ffmpegProcess.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
        }

        _log.Information("Exit code: {ExitCode}", _ffmpegProcess.ExitCode);
        return new FFmpegResult(_ffmpegProcess.ExitCode, _lastStdErrLine);
    }

    public string Probe(string args = "")
    {
        _ffprobeProcess.StartInfo.Arguments =
            $"{args} \"{_videoPath}\"";
        _log.Information("FFprobe arguments: {Args}", _ffprobeProcess.StartInfo.Arguments);
        
        _ffprobeProcess.Start();
        var stdout = _ffprobeProcess.StandardOutput.ReadToEnd();
        var stderr = _ffprobeProcess.StandardError.ReadToEnd();
        return stdout == string.Empty ? stderr : stdout;
    }

    public string ProbeFormat(string format) => Probe($"{ProbeFormatArgs}{format}");

    public void Dispose()
    {
        //TODO Should make sure the ffmpegprocess is closed after this
        if (_ffmpegProcess is { HasStarted: true, HasExited: false })
            _ffmpegProcess.StandardInput.Write('q');
        _ffmpegProcess.Dispose();
        
        if (_ffprobeProcess is { HasStarted: true, HasExited: false })
            _ffprobeProcess.Kill();
        _ffprobeProcess.Dispose();
        
        GC.SuppressFinalize(this);
    }

    private void FFmpegProcessOnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null) return;
        
        if (e.Data.Contains("out_time="))
        {
            var currentTimeMs = TimeCode.GetExactUnits(TimeUnit.Millisecond, e.Data.Split('=')[1].Replace("-", ""));
            
            var progress = currentTimeMs / _spanTimeMs;
            ProgressChanged?.Invoke(progress);
        }
        else if (e.Data.Contains("progress=end"))
        {
            ProgressChanged?.Invoke(1);
        }
        
        _log.Information(e.Data);
    }

    private void FFmpegProcessOnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null) return;

        _lastStdErrLine = e.Data;
        _log.Error(_lastStdErrLine);
    }

    private void SetArgs(string args)
    {
        _ffmpegProcess.StartInfo.Arguments =
            $"-progress pipe:1 -y {args}";
        _log.Information("FFmpeg arguments: {Args}", _ffmpegProcess.StartInfo.Arguments);
    }
}