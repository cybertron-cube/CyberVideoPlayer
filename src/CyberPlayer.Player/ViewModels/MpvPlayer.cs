using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using CyberPlayer.Player.AppSettings;
using CyberPlayer.Player.Models;
using Cybertron;
using LibMpv.Client;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace CyberPlayer.Player.ViewModels;

public class MpvPlayer : ViewModelBase
{
    private readonly Settings _settings;

    private readonly ILogger _log;
    
    private MpvContext _mpvContext;

    public MpvContext MpvContext
    {
        get => _mpvContext;
        set
        {
            this.RaiseAndSetIfChanged(ref _mpvContext, value);
            MpvContext.FileLoaded += MpvContext_FileLoaded;
            MpvContext.EndFile += MpvContext_EndFile;
        }
    }
    
    public MpvPlayer(ILogger logger, Settings settings)
    {
        _log = logger.ForContext<MpvPlayer>();
        _settings = settings;

        _mpvContext = new MpvContext();
        MpvContext.FileLoaded += MpvContext_FileLoaded;
        MpvContext.EndFile += MpvContext_EndFile;

        _seekTimeCode = new TimeCode(0);
        _durationTimeCode = new TimeCode(1);
        _trimStartTimeCode = new TimeCode(0);
        _trimEndTimeCode = new TimeCode(1);
        _timeCodeStartIndex = 0;
        _timeCodeLength = _settings.TimeCodeLength;
        WindowWidth = double.NaN;
        WindowHeight = double.NaN;
        TrackListJson = string.Empty;

        FrameStepCommand = ReactiveCommand.Create<string>(FrameStep);
        SeekCommand = ReactiveCommand.Create<double>(Seek);
        VolumeCommand = ReactiveCommand.Create<double>(ChangeVolume);

        UpdateSliderTaskCts = new CancellationTokenSource();
        Task.Run(() => UpdateSliderValueLoop(UpdateSliderTaskCts.Token));
    }

    private void MpvContext_EndFile(object? sender, MpvEndFileEventArgs e)
    {
        if (_replacingFile)
        {
            _replacingFile = false;
        }
        else
        {
            IsPlaying = false;
        }
        
        IsFileLoaded = false;
        SetSliderValueNoSeek(Duration);
    }

    private void MpvContext_FileLoaded(object? sender, EventArgs e)
    {
        Debug.WriteLine("File Loaded");
        IsFileLoaded = true;
        if (_reloadFile)
        {
            _reloadFile = false;
            IsPlaying = true;
        }
        else if (!double.IsNaN(_lastSeekValue))
        {
            Seek();
            MpvContext.SetPropertyFlag(MpvProperties.Paused, true);
        }
        else //loading new file
        {
            Duration = MpvContext.GetPropertyDouble(MpvProperties.Duration);
            TrimEndTime = Duration;
            TrimStartTime = 0;
            if (_durationTimeCode.Hours == 0)
            {
                _timeCodeStartIndex = 3;
                _timeCodeLength = _settings.TimeCodeLength - _timeCodeStartIndex;
            }
            else
            {
                _timeCodeStartIndex = 0;
                _timeCodeLength = _settings.TimeCodeLength;
            }
            this.RaisePropertyChanged(nameof(TrimStartTimeCodeString));
            this.RaisePropertyChanged(nameof(TrimEndTimeCodeString));
            this.RaisePropertyChanged(nameof(DurationTimeCodeString));
            this.RaisePropertyChanged(nameof(SeekTimeCodeString));
            
            GetTracks();
            if (GetMainWindowState() == WindowState.Normal)
                SetWindowSize();
        }
        Debug.WriteLine(Duration);
    }
    
    public ReactiveCommand<string, Unit> FrameStepCommand { get; }
    
    public ReactiveCommand<double, Unit> SeekCommand { get; }
    
    public ReactiveCommand<double, Unit> VolumeCommand { get; }

    private readonly ManualResetEvent _updateSliderMre = new(false);
    
    private bool _initialFileLoaded = false;
    
    private bool _reloadFile = false;
    
    private bool _replacingFile = false;
    
    private double _lastSeekValue = double.NaN;
    
    private int _timeCodeStartIndex;
    
    private int _timeCodeLength;

    private double _trimStartTime;
    
    public double TrimStartTime
    {
        get => _trimStartTime;
        set
        {
            if (value - _trimStartTime == 0) return;
            _trimStartTime = value;
            _trimStartTimeCode.SetExactUnits(value, TimeCode.TimeUnit.Second);
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(TrimStartTimeCodeString));
        }
    }

    private readonly TimeCode _trimStartTimeCode;

    public TimeCode TrimStartTimeCode => _trimStartTimeCode;
    
    public string TrimStartTimeCodeString => _trimStartTimeCode.FormattedString.Substring(_timeCodeStartIndex, _timeCodeLength);

    private double _trimEndTime = 1;
    
    public double TrimEndTime
    {
        get => _trimEndTime;
        set
        {
            if (value - _trimEndTime == 0) return;
            _trimEndTime = value;
            _trimEndTimeCode.SetExactUnits(value, TimeCode.TimeUnit.Second);
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(TrimEndTimeCodeString));
        }
    }

    private readonly TimeCode _trimEndTimeCode;

    public TimeCode TrimEndTimeCode => _trimEndTimeCode;

    public string TrimEndTimeCodeString => _trimEndTimeCode.FormattedString.Substring(_timeCodeStartIndex, _timeCodeLength);

    [Reactive]
    public bool IsFileLoaded { get; set; }
    
    public CancellationTokenSource UpdateSliderTaskCts { get; }

    private double _duration = 1;
    
    public double Duration
    {
        get => _duration;
        set
        {
            if (value - _duration == 0) return;
            _duration = value;
            _durationTimeCode.SetExactUnits(value, TimeCode.TimeUnit.Second);
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(DurationTimeCodeString));
        }
    }

    private readonly TimeCode _durationTimeCode;

    public string DurationTimeCodeString => _durationTimeCode.FormattedString.Substring(_timeCodeStartIndex, _timeCodeLength);
    
    private bool _isPlaying = false;
    
    public bool IsPlaying
    {
        get => _isPlaying;
        set
        {
            if (_isPlaying == value) return;
            _isPlaying = value;
            this.RaisePropertyChanged();
            if (value)
            {
                _updateSliderMre.Set();
            }
            else
            {
                _updateSliderMre.Reset();
            }
        }
    }

    private bool _wasPlaying;
    
    private bool _isSeeking;
    public bool IsSeeking
    {
        get => _isSeeking;
        set
        {
            if (value == _isSeeking) return;
            _isSeeking = value;
            this.RaisePropertyChanged();
            if (IsFileLoaded)
            {
                switch (value)
                {
                    case true:
                        _wasPlaying = IsPlaying;
                        IsPlaying = false;
                        MpvContext.SetPropertyFlag(MpvProperties.Paused, true);
                        return;
                    case false when SeekValue - Duration < 0:
                        if (!double.IsNaN(_lastSeekValue))
                        {
                            _lastSeekValue = double.NaN;
                            return;
                        }
                        IsPlaying = _wasPlaying;
                        MpvContext.SetPropertyFlag(MpvProperties.Paused, !_wasPlaying);
                        return;
                }
            }
            else
            {
                if (!value) return;
                _lastSeekValue = SeekValue;
                MpvContext.Command(MpvCommands.LoadFile, MediaPath, "replace");
            }
        }
    }
    
#if DEBUG
    private string _mediaPath = @"D:\Editing\My Clips\Dynasty Double Canswap.mp4";
    //D:\Editing\My Clips\Dynasty Double Canswap.mp4
    //H:\makemkv\12 Monkeys- Season Two (Disc 1)\12 Monkeys- Season Two (Disc 1)_t05.mkv
    //H:\makemkv\John Wick\John Wick-FPL_MainFeature_t05.mkv
#else
    private string _mediaPath = string.Empty;
#endif
    public string MediaPath
    {
        get => _mediaPath;
        set
        {
            this.RaiseAndSetIfChanged(ref _mediaPath, value);
            this.RaisePropertyChanged(nameof(MediaName));
        }
    }

    public string MediaName => Path.GetFileName(MediaPath);
    
    private double _seekValue = 0;
    
    public double SeekValue
    {
        get => _seekValue;
        set
        {
            if (value - _seekValue == 0) return;
            
            _seekValue = value;
            _seekTimeCode.SetExactUnits(value, TimeCode.TimeUnit.Second);
            
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(SeekTimeCodeString));
            
            if (IsFileLoaded)
            {
                Seek();
            }
        }
    }

    private readonly TimeCode _seekTimeCode;

    public string SeekTimeCodeString => _seekTimeCode.FormattedString.Substring(_timeCodeStartIndex, _timeCodeLength);

    private double _volumeValue = 100; //TODO THIS SHOULD PERSIST THROUGH RESTARTING APPLICATION???

    public double VolumeValue
    {
        get => _volumeValue;
        set //TODO Have interacting with this automatically unmute sound if muted?
        {
            this.RaiseAndSetIfChanged(ref _volumeValue, value);
            if (IsFileLoaded)
            {
                MpvContext.SetPropertyString(MpvProperties.Volume, value.ToString("0"));
            }
        }
    }

    private bool _isMuted;

    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            if (_isMuted == value) return;
            _isMuted = value;
            this.RaisePropertyChanged();
            MpvContext.SetPropertyFlag(MpvProperties.Muted, value);
        }
    }

    [Reactive]
    public IEnumerable<TrackInfo>? AudioTrackInfos { get; set; }

    private TrackInfo? _selectedAudioTrack;

    public TrackInfo? SelectedAudioTrack
    {
        get => _selectedAudioTrack;
        set
        {
            if (value == _selectedAudioTrack) return;
            if (_selectedAudioTrack != null) _selectedAudioTrack.Selected = false;
            if (value != null)
            {
                value.Selected = true;
                MpvContext.SetPropertyString(MpvProperties.AudioTrackId, value.Id.ToString());
            }
            _selectedAudioTrack = value;
            this.RaisePropertyChanged();
        }
    }
    
    [Reactive]
    public IEnumerable<TrackInfo>? VideoTrackInfos { get; set; }

    private TrackInfo? _selectedVideoTrack;

    public TrackInfo? SelectedVideoTrack
    {
        get => _selectedVideoTrack;
        set
        {
            if (value == _selectedVideoTrack) return;
            if (_selectedVideoTrack != null) _selectedVideoTrack.Selected = false;
            if (value != null)
            {
                value.Selected = true;
                MpvContext.SetPropertyString(MpvProperties.VideoTrackId, value.Id.ToString());
            }
            _selectedVideoTrack = value;
            this.RaisePropertyChanged();
        }
    }
    
    [Reactive]
    public string TrackListJson { get; set; }

    [Reactive]
    public double WindowWidth { get; set; }
    
    [Reactive]
    public double WindowHeight { get; set; }

    public int VideoHeight { get; private set; }

    public void SetWindowSize()
    {
        //Mac scaling is unique
        var maxWidth = OperatingSystem.IsMacOS() ? Screens.Primary!.WorkingArea.Width
            : Screens.Primary!.WorkingArea.Width / RenderScaling;
        var maxHeight = OperatingSystem.IsMacOS() ? Screens.Primary.WorkingArea.Height
                : (int)(Screens.Primary.WorkingArea.Height / RenderScaling);
        
        var panelHeightDifference = 0;
        Dispatcher.UIThread.Invoke(() =>
        {
            //INCLUDES SYSTEM DECORATIONS
            //The height of the entire window without the video panel
            panelHeightDifference = (int)PanelHeightDifference;
        });
        
        //Get the height of the video scaled for the correct aspect ratio
        //Sometimes the property is attempted to be retrieved before available
        //(Even though this method is called in the fileloaded event the exception still occurs on windows)
        var videoSourceHeight = 0;
        var getDemuxHeightResult = ExecuteAndRetry(() =>
        {
            videoSourceHeight = (int)_mpvContext.GetPropertyLong("video-params/dh");
        }, exception => _log.Error(exception, ""));

        if (getDemuxHeightResult == false) throw new Exception("Could not retrieve video height from mpv");

        //Calculate the height of the video and the height of the entire window
        double desiredHeight;
        
        if (SelectedVideoTrack!.VideoDemuxHeight + panelHeightDifference >= maxHeight)
        {
            VideoHeight = maxHeight - panelHeightDifference;
            desiredHeight = maxHeight - SystemDecorations;
        }
        else
        {
            VideoHeight = videoSourceHeight;
            desiredHeight = videoSourceHeight + panelHeightDifference - SystemDecorations;
        }
        
        //Calculate aspect ratio
        var displayAspectRatio = (double)SelectedVideoTrack.VideoDemuxWidth! / (double)SelectedVideoTrack.VideoDemuxHeight!;
        //Account for sample/pixel aspect ratio if needed
        if (SelectedVideoTrack.VideoDemuxPar != null)
            displayAspectRatio *= (double)SelectedVideoTrack.VideoDemuxPar;
        
        var desiredWidth = VideoHeight * displayAspectRatio;
        
        if (desiredWidth > maxWidth)
        {
            //change height to match new width
            desiredHeight = maxWidth / displayAspectRatio;
            desiredWidth = maxWidth;
        }

        //Find centered x and y pixel points
        //Assumes default taskbar locations for the most part (bottom and left side work properly)
        var x = OperatingSystem.IsMacOS() ? (maxWidth - desiredWidth) / 2 + Screens.Primary.Bounds.Width - maxWidth
            : (maxWidth * RenderScaling - desiredWidth * RenderScaling) / 2 + Screens.Primary.Bounds.Width / RenderScaling - maxWidth;
        
        //This seems to be a tad inaccurate on mac (on the lower side) but not crazy noticeable
        // - would have to find height of top bar and bottom bar in order to correct this
        //On linux RenderScaling seems to always be one
        // - if scaling is set to 200%, the window will not be centered vertically correctly (more towards bottom)
        var y = OperatingSystem.IsMacOS() ? (maxHeight - desiredHeight + SystemDecorations) / 2 :
            OperatingSystem.IsWindows() ? (maxHeight * RenderScaling - (desiredHeight + SystemDecorations) * RenderScaling) / 2
            : (maxHeight - (desiredHeight + SystemDecorations)) / 2 + Screens.Primary.Bounds.Height - maxHeight;
        
        Dispatcher.UIThread.Invoke(() =>
        {
            WindowWidth = desiredWidth;
            WindowHeight = desiredHeight;
        });
        
        //Position updates too early on linux occasionally
        Dispatcher.UIThread.Post(() =>
        {
            MainWindow.Position = new PixelPoint((int)x, (int)y);
        });
        
        //with windows the width seems to be one pixel too much?
    }
    
    ///Made to help with calls to mpv since information retrieval can be a bit wonky
    private static bool ExecuteAndRetry(Action work, Action<Exception>? onCaughtException = null, int retryCount = 2, int delay = 100)
    {
        for (int i = 0; i <= retryCount; i++)
        {
            try
            {
                work.Invoke();
                return true;
            }
            catch (Exception e)
            {
                onCaughtException?.Invoke(e);
                Thread.Sleep(delay);
            }
        }

        return false;
    }

    private void ChangeVolume(double offset)
    {
        var newVolume = VolumeValue + offset;
        VolumeValue = newVolume switch
        {
            > 100 => 100,
            < 0 => 0,
            _ => newVolume
        };
    }

    private void FrameStep(string param)
    {
        if (IsPlaying)
        {
            IsPlaying = false;
        }

        Dispatcher.UIThread.Invoke(() =>
        {
            MpvContext.Command(param);
        });
        
        Task.Run(() =>
        {
            Thread.Sleep(_settings.FrameStepUpdateDelay);
            Dispatcher.UIThread.Post(UpdateSliderValue);
        });
    }

    private void GetTracks()
    {
        TrackListJson = MpvContext.GetPropertyString(MpvProperties.TrackList);
        var trackInfos = JsonSerializer.Deserialize(TrackListJson, TrackInfoJsonContext.Default.TrackInfoArray);
        AudioTrackInfos = trackInfos!.Where(x => x.Type == "audio");
        SelectedAudioTrack = AudioTrackInfos.FirstOrDefault();
        VideoTrackInfos = trackInfos!.Where(x => x.Type == "video");
        SelectedVideoTrack = VideoTrackInfos.FirstOrDefault();
    }
    
    public void PlayPause()
    {
        if (IsFileLoaded)
        {
            if (SeekValue - Duration >= 0)
            {
                MpvContext.Command(MpvCommands.Seek, "0", "absolute");
                SetSliderValueNoSeek(0);
                MpvContext.SetPropertyFlag(MpvProperties.Paused, false);
                IsPlaying = true;
                return;
            }
            MpvContext.Command(MpvCommands.Cycle, "pause");
            IsPlaying = !IsPlaying;
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(MediaPath))
            {
                _reloadFile = true;
                LoadFile();
            }
        }
    }

    private void Seek()
    {
        Dispatcher.UIThread.Post(() =>
        {
            MpvContext.CommandAsync(0, MpvCommands.Seek, SeekValue.ToString("F3"), "absolute");
        });
    }
    
    private void Seek(double offset)
    {
        var newSeekValue = SeekValue + offset;
        var wasPlaying = false;

        if (IsPlaying)
        {
            Dispatcher.UIThread.Invoke(PlayPause);
            wasPlaying = true;
        }
        
        if (0 < newSeekValue && newSeekValue < Duration)
        {
            
        }
        else if (newSeekValue >= Duration)
        {
            Dispatcher.UIThread.Post(() =>
            {
                SeekValue = Duration;
            });
            return;
        }
        else
        {
            newSeekValue = 0;
        }
        
        Dispatcher.UIThread.Post(() =>
        {
            SeekValue = newSeekValue;
            if (wasPlaying)
                PlayPause();
        });
    }

    public void LoadFile(string? mediaPath = null)
    {
        if (mediaPath != null)
            MediaPath = mediaPath;
        
        if (!_reloadFile)
            _replacingFile = true;
        
        MpvContext.Command(MpvCommands.LoadFile, MediaPath, "replace");

        if (_replacingFile)
        {
            MpvContext.SetPropertyFlag(MpvProperties.Paused, false);
            IsPlaying = true; //TODO Make setting
            return;
        }

        if (!_initialFileLoaded)
        {
            _initialFileLoaded = true;
            IsPlaying = true; //TODO Make setting
        }
    }
    
    private void SetSliderValueNoSeek(double val)
    {
        _seekValue = val;
        _seekTimeCode.SetExactUnits(val, TimeCode.TimeUnit.Second);
        this.RaisePropertyChanged(nameof(SeekValue));
        this.RaisePropertyChanged(nameof(SeekTimeCodeString));
    }

    private void UpdateSliderValue()
    {
        SetSliderValueNoSeek(MpvContext.GetPropertyDouble(MpvProperties.TimePosition));
    }
    
    private async Task UpdateSliderValueLoop(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        double timePos;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                _updateSliderMre.WaitOne();
                timePos = MpvContext.GetPropertyDouble(MpvProperties.TimePosition);
                if (timePos >= 0)
                    await Dispatcher.UIThread.InvokeAsync(Callback);
                await Task.Delay(_settings.SeekRefreshRate, ct);
            }
            catch (MpvException mpvException)
            {
                //Dirty way to stop thread from aborting when file is unloaded
                //This exception should only be caught once when unloading while the video is playing
                //Unloaded event resets the mre but this method will be on the mpvcontext line throwing an exception already by the time the mre is reset
                // (this thread is paused after the file is unloaded, therefore causing the exception)
                Debug.WriteLine(mpvException);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                throw;
            }
        }
        ct.ThrowIfCancellationRequested();
        return;

        void Callback()
        {
            if (!IsSeeking) SetSliderValueNoSeek(timePos);
        }
    }
}