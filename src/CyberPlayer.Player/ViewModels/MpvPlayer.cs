using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Text.Json;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Threading;
using CyberPlayer.Player.AppSettings;
using CyberPlayer.Player.Models;
using Cybertron;
using LibMpv.Context;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace CyberPlayer.Player.ViewModels;

public class MpvPlayer : ViewModelBase
{
    private readonly Settings _settings;

    private readonly ILogger _log;

    private CompositeDisposable? _disposables;
    
    private MpvContext _mpvContext;

    public MpvContext MpvContext
    {
        get => _mpvContext;
        set
        {
            _disposables?.Dispose();
            this.RaiseAndSetIfChanged(ref _mpvContext, value);
            _mpvContext.FileLoaded += MpvContext_FileLoaded;
            _mpvContext.EndFile += MpvContext_EndFile;
            ObserveProperties();
        }
    }
    
    public MpvPlayer(ILogger logger, Settings settings)
    {
        _log = logger.ForContext<MpvPlayer>();
        _settings = settings;

        _mpvContext = new MpvContext();
        MpvContext.FileLoaded += MpvContext_FileLoaded;
        MpvContext.EndFile += MpvContext_EndFile;
        ObserveProperties();

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
    }

    private void ObserveProperties()
    {
        _disposables?.Dispose();
        _disposables = new CompositeDisposable
        {
            _mpvContext.ObserveProperty<double>(MpvProperties.TimePosition).Subscribe(x =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (!IsSeeking)
                        SetSliderValueNoSeek(x);
                });
            })
        };
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
        set => this.RaiseAndSetIfChanged(ref _isPlaying, value);
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
    private string _mediaPath = BuildConfig.GetTestMedia();
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

    public double VideoHeight { get; private set; }

    public void SetWindowSize()
    {
        var mainWindow = ViewLocator.Main;
        var screen = mainWindow.GetMainWindowScreen();
        
        if (screen is null) throw new NullReferenceException();
        
        var scaling = mainWindow.DesktopScaling;
        _log.Verbose("scaling: {A}", scaling);
        
        // ClientSize = Only our part of the window
        // FrameSize = The entire window, including system decorations
        // If window size doesn't set properly this is likely the culprit
        // This is because the platform may not supply the FrameSize to us
        var systemDecorations = mainWindow.FrameSize == null ? 0
            : (int)((mainWindow.FrameSize.Value.Height - mainWindow.ClientSize.Height) * scaling);
        _log.Verbose("systemDecorations: {A}", systemDecorations);
        
        // Screen working area is not scaled so no need to unscale these values
        var maxWidth = screen.WorkingArea.Width;
        var maxHeight = screen.WorkingArea.Height;
        _log.Verbose("maxWidth: {A}", maxWidth);
        _log.Verbose("maxHeight: {A}", maxHeight);
        
        double panelHeightDiff = 0;
        Dispatcher.UIThread.Invoke(() =>
        {
            panelHeightDiff = mainWindow.MenuBar.IsVisible
                ? (mainWindow.MainGrid.RowDefinitions[0].ActualHeight + mainWindow.MainGrid.RowDefinitions[2].ActualHeight) * scaling
                : mainWindow.MainGrid.RowDefinitions[2].ActualHeight * scaling;
        });
        _log.Verbose("panelHeightDiff: {A}", panelHeightDiff);
        
        // Get the height of the video scaled for the correct aspect ratio
        // Sometimes the property is attempted to be retrieved before available
        // (Even though this method is called in the fileloaded event the exception still occurs on windows)
        long videoSourceHeight = 0;
        var getDemuxHeightResult = ExecuteAndRetry(() =>
        {
            videoSourceHeight = _mpvContext.GetPropertyLong("video-params/dh");
        }, exception => _log.Error(exception, ""));
        _log.Verbose("videoSourceHeight: {A}", videoSourceHeight);
        
        if (getDemuxHeightResult == false) throw new Exception("Could not retrieve video height from mpv");
        
        if (videoSourceHeight + panelHeightDiff + systemDecorations >= maxHeight)
            VideoHeight = maxHeight - systemDecorations - panelHeightDiff;
        else
            VideoHeight = videoSourceHeight;
        
        VideoHeight /= scaling;
        _log.Verbose("VideoHeight: {A}", VideoHeight);
        
        // Calculate aspect ratio
        var displayAspectRatio = (double)SelectedVideoTrack!.VideoDemuxWidth! / (double)SelectedVideoTrack.VideoDemuxHeight!;
        // Account for sample/pixel aspect ratio if needed
        if (SelectedVideoTrack.VideoDemuxPar != null)
        {
            displayAspectRatio *= (double)SelectedVideoTrack.VideoDemuxPar;
            _log.Verbose("VideoDemuxPar: {A}", (double)SelectedVideoTrack.VideoDemuxPar);
        };
        _log.Verbose("displayAspectRatio: {A}", displayAspectRatio);
        
        var desiredWidth = VideoHeight * displayAspectRatio;
        _log.Verbose("desiredWidth: {A}", desiredWidth);
        
        if (desiredWidth > maxWidth)
        {
            VideoHeight = maxWidth / scaling / displayAspectRatio;
            desiredWidth = maxWidth;
            _log.Verbose("VideoHeight: {A}", VideoHeight);
            _log.Verbose("desiredWidth: {A}", desiredWidth);
        }
        
        var desiredHeight = panelHeightDiff / scaling + VideoHeight;
        _log.Verbose("desiredHeight: {A}", desiredHeight);
        
        Dispatcher.UIThread.Invoke(() =>
        {
            WindowWidth = desiredWidth;
            WindowHeight = desiredHeight;
        });
        
        Dispatcher.UIThread.Post(() =>
        {
            mainWindow.CenterWindow();
        });
    }
    
    ///Made to help with calls to mpv since information retrieval can be a bit wonky
    private static bool ExecuteAndRetry(Action work, Action<Exception>? onCaughtException = null, int tryCount = 3, int delay = 100)
    {
        for (int i = 0; i < tryCount; i++)
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
}