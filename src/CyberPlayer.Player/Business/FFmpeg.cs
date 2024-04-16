using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CyberPlayer.Player.AppSettings;
using Cybertron;
using Serilog;
using CyberPlayer.Player.Helpers;
using Serilog.Core;

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
        
        var ffmpegPath = File.Exists(settings.FFmpegPath) ?
            settings.FFmpegPath
            : Path.Combine(settings.FFmpegPath, "ffmpeg");
        
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

        var ffprobePath = File.Exists(settings.FFprobePath) ?
            settings.FFprobePath
            : Path.Combine(settings.FFprobePath, "ffprobe");
        
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
        _startTimeMs = startTime.GetExactUnits(TimeCode.TimeUnit.Millisecond);
        _endTimeMs = endTime.GetExactUnits(TimeCode.TimeUnit.Millisecond);
        _spanTimeMs = _endTimeMs - _startTimeMs;
        
        SetArgsWithDefaults(args);
        
        _log.Information("Starting {CommandName} of video {VideoPath} from {StartTime} to {EndTime}",
            commandName, _videoPath, startTime.FormattedString, endTime.FormattedString);
        
        _ffmpegProcess.Start();
        
        var outputTask = ReadStreamAsync(_ffmpegProcess.StandardOutput, "FF-STDOUT", ProgressChanged is null ? null : OnNewLineStandardOutput);
        var errorTask = ReadStreamAsync(_ffmpegProcess.StandardError, "FF-STDERR", OnNewLineStandardError);
        var exitTask = WaitFFmpegAsync(commandName, ct);
        
        await Task.WhenAll(outputTask, errorTask, exitTask);
        
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
        if (_ffmpegProcess is { HasStarted: true, HasExited: false })
            QuitFFmpegProcess();
        _ffmpegProcess.Dispose();
        
        if (_ffprobeProcess is { HasStarted: true, HasExited: false })
            _ffprobeProcess.Kill();
        _ffprobeProcess.Dispose();
        
        _log.Dispose();
        
        GC.SuppressFinalize(this);
    }

    private async Task WaitFFmpegAsync(string commandName, CancellationToken ct)
    {
        try
        {
            await _ffmpegProcess.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            _log.Information("{Command} canceled", commandName);
            await QuitFFmpegProcessAsync();
        }
    }

    private async Task QuitFFmpegProcessAsync()
    {
        await _ffmpegProcess.StandardInput.WriteAsync('q');
        await _ffmpegProcess.StandardInput.FlushAsync();
        try
        {
            await _ffmpegProcess.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (TimeoutException ex)
        {
            _log.Error(ex, "FFmpeg would not gracefully exit, the process will now be killed");
            _ffmpegProcess.Kill();
        }
    }
    
    private void QuitFFmpegProcess()
    {
        _ffmpegProcess.StandardInput.Write('q');
        _ffmpegProcess.StandardInput.Flush();
        try
        {
            _ffmpegProcess.WaitForExit(TimeSpan.FromSeconds(5));
        }
        catch (TimeoutException ex)
        {
            _log.Error(ex, "FFmpeg would not gracefully exit, the process will now be killed");
            _ffmpegProcess.Kill();
        }
    }
    
    private async Task ReadStreamAsync(StreamReader stream, string name, Action<string>? onNewLine = null)
    {
        var buffer = new char[4096];
        var sb = new StringBuilder();
        int charRead;
        while ((charRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            _log.Verbose("[{StreamName}] Chars Read: {Chars} | Buffer Length: {Length}", name, charRead, buffer.Length);
            if (charRead >= buffer.Length * 0.8)
            {
                _log.Warning("[{StreamName}] Buffer is approaching overflow -> Chars Read: {Chars} | Buffer Length: {Length}", name, charRead, buffer.Length);
            }
            
            for (int i = 0; i < charRead; i++)
            {
                if (buffer[i] == '\n')
                {
                    var line = buffer[i - 1] == '\r' ? sb.ToStringTrimEnd("\r") : sb.ToString();
                    sb.Clear();
                    _log.Information("[{StreamName}] {Data}", name, line);
                    onNewLine?.Invoke(line);
                }
                else
                {
                    sb.Append(buffer[i]);
                }
            }
        }
    }
    
    private void OnNewLineStandardError(string line)
    {
        _lastStdErrLine = line;
    }
    
    private void OnNewLineStandardOutput(string line)
    {
        // EX: out_time_ms=659434000
        if (line.Contains("out_time_ms"))
        {
            // Ignore the last 3 characters since for some reason ffmpeg outputs microseconds instead of milliseconds
            var currentTimeMs = Convert.ToDouble(line.Split('=')[1][..^3]);
            if (currentTimeMs < 0) return;
            var progress = currentTimeMs / _spanTimeMs;
            Dispatcher.UIThread.Post(() => ProgressChanged!.Invoke(progress));
        }
        else if (line.Contains("progress=end"))
        {
            Dispatcher.UIThread.Post(() => ProgressChanged!.Invoke(1));
        }
    }
    
    private void SetArgsWithDefaults(string args)
    {
        _ffmpegProcess.StartInfo.Arguments = $"-progress pipe:1 -y {args}";
        _log.Information("FFmpeg arguments: {Args}", _ffmpegProcess.StartInfo.Arguments);
    }
}