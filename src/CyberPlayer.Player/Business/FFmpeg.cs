using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cybertron;
using static Cybertron.TimeCode;

namespace CyberPlayer.Player.Business;

public class FFmpeg : IDisposable
{
    public event Action<double>? ProgressChanged;
    
    private readonly string _videoPath;
    private string? _lastStdErrLine;
    private readonly Process _ffmpegProcess;
    private double _startTimeMs = double.NaN;
    private double _endTimeMs = double.NaN;
    
    public FFmpeg(string ffmpegPath, string videoPath)
    {
        _videoPath = videoPath;
        
        _ffmpegProcess = new Process
        {
            EnableRaisingEvents = true,
            StartInfo = new ProcessStartInfo(ffmpegPath)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardInput = false,
                RedirectStandardOutput = true
            }
        };
        
        _ffmpegProcess.OutputDataReceived += FFmpegProcessOnOutputDataReceived;
        _ffmpegProcess.ErrorDataReceived += FFmpegProcessOnErrorDataReceived;
    }

    public FFmpeg(string videoPath)
    {
        var ffmpegPath = GenStatic.GetFullPathFromRelative(Path.Combine("ffmpeg", "ffmpeg"));
        GenStatic.GetOSRespectiveExecutablePath(ref ffmpegPath);
        
        _videoPath = videoPath;
        
        _ffmpegProcess = new Process
        {
            EnableRaisingEvents = true,
            StartInfo = new ProcessStartInfo(ffmpegPath)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardInput = false,
                RedirectStandardOutput = true
            }
        };
        
        _ffmpegProcess.OutputDataReceived += FFmpegProcessOnOutputDataReceived;
        _ffmpegProcess.ErrorDataReceived += FFmpegProcessOnErrorDataReceived;
    }

    public struct FFmpegResult
    {
        public readonly int ExitCode;
        public readonly string? ErrorMessage;

        public FFmpegResult(int exitCode, string? errorMessage)
        {
            ExitCode = exitCode;
            ErrorMessage = errorMessage;
        }
        
        public FFmpegResult(int exitCode)
        {
            ExitCode = exitCode;
        }
    }

    public FFmpegResult Trim(TimeCode startTime, TimeCode endTime)
    {
        _startTimeMs = startTime.GetExactUnits(TimeUnit.Millisecond);
        _endTimeMs = endTime.GetExactUnits(TimeUnit.Millisecond);

        SetTrimArgs(startTime.FormattedString, endTime.FormattedString,
            GenStatic.AppendFileName(_videoPath, "-trim"));
        Debug.WriteLine(_ffmpegProcess.StartInfo.Arguments);

        _ffmpegProcess.Start();
        _ffmpegProcess.BeginOutputReadLine();
        _ffmpegProcess.BeginErrorReadLine();
        _ffmpegProcess.WaitForExit();
        
        return new FFmpegResult(_ffmpegProcess.ExitCode, _lastStdErrLine);
    }
    
    public async Task<FFmpegResult> TrimAsync(TimeCode startTime, TimeCode endTime, CancellationToken ct)
    {
        _startTimeMs = startTime.GetExactUnits(TimeUnit.Millisecond);
        _endTimeMs = endTime.GetExactUnits(TimeUnit.Millisecond);

        SetTrimArgs(startTime.FormattedString, endTime.FormattedString,
            GenStatic.AppendFileName(_videoPath, "-trim"));
        
        _ffmpegProcess.Start();
        _ffmpegProcess.BeginOutputReadLine();
        _ffmpegProcess.BeginErrorReadLine();
        await _ffmpegProcess.WaitForExitAsync(ct);

        return new FFmpegResult(_ffmpegProcess.ExitCode, _lastStdErrLine);
    }

    public void Transcode(string extension)
    {
        throw new NotImplementedException();
    }

    private void FFmpegProcessOnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null) return;
        
        if (e.Data.Contains("out_time_ms="))
        {
            var currentTimeMs = double.Parse(e.Data.Substring(12, 5));
            var progress = (currentTimeMs - _startTimeMs) / _endTimeMs;
            ProgressChanged?.Invoke(progress);
        }
        else if (e.Data.Contains("progress=end"))
        {
            ProgressChanged?.Invoke(1);
        }
    }

    private void FFmpegProcessOnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null) return;

        _lastStdErrLine = e.Data;
    }

    public void Dispose()
    {
        _ffmpegProcess.Dispose();
        GC.SuppressFinalize(this);
    }

    private void SetTrimArgs(string startTimeCode, string endTimeCode, string videoDestination)
    {
        _ffmpegProcess.StartInfo.Arguments =
            $"-progress pipe:1 -y -ss {startTimeCode} -to {endTimeCode} -i \"{_videoPath}\" -map 0 -codec copy -avoid_negative_ts make_zero \"{videoDestination}\"";
        Debug.WriteLine(_ffmpegProcess.StartInfo.Arguments);
    }
}