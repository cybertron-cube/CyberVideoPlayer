using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Threading;
using CyberPlayer.Player.AppSettings;
using CyberPlayer.Player.Models;
using CyberPlayer.Player.RendererVideoViews;
using CyberPlayer.Player.Services;
using Cybertron;
using LibMpv.Client;
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
    
    private MpvContext _mpvContext = new();

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

        if (_settings.Renderer != Renderer.Native)
            MpvContext.SetOptionString("vo", "libmpv");
        
        MpvContext.FileLoaded += MpvContext_FileLoaded;
        MpvContext.EndFile += MpvContext_EndFile;
        ObserveProperties();

        _seekTimeCode = new TimeCode(0);
        _durationTimeCode = new TimeCode(1);
        _trimStartTimeCode = new TimeCode(0);
        _trimEndTimeCode = new TimeCode(1);
        _timeCodeStartIndex = 0;
        _timeCodeLength = _settings.TimeCodeLength;
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
            }),
            _mpvContext.ObserveProperty<bool>(MpvProperties.Paused).Skip(1).Subscribe(isPaused =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    IsPlaying = !isPaused;
                });
            })
        };
    }

    private void MpvContext_EndFile(object? sender, MpvEndFileEventArgs e)
    {
        // In mpv paused is not set to true when reaching the end of file
        // but we still want the pause/play button to show the play symbol
        if (e.Reason == mpv_end_file_reason.MPV_END_FILE_REASON_EOF)
            MpvContext.SetPropertyFlag(MpvProperties.Paused, true);
        
        IsFileLoaded = false;
        SetSliderValueNoSeek(Duration);
    }

    // WARNING: Events are running on a different thread!
    private void MpvContext_FileLoaded(object? sender, EventArgs e)
    {
        _log.Information("File \"{FilePath}\" loaded", MediaPath);
        IsFileLoaded = true;
        IsPlaying = true;
        
        if (!double.IsNaN(_lastSeekValue)) //loading from seeking after hitting the end of the video
        {
            Seek();
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
                ResizeAndCenterWindow();
        }
    }
    
    public ReactiveCommand<string, Unit> FrameStepCommand { get; }
    
    public ReactiveCommand<double, Unit> SeekCommand { get; }
    
    public ReactiveCommand<double, Unit> VolumeCommand { get; }
    
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
            _trimStartTimeCode.SetExactUnits(value, TimeCodeUnit.Second);
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
            _trimEndTimeCode.SetExactUnits(value, TimeCodeUnit.Second);
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
            _durationTimeCode.SetExactUnits(value, TimeCodeUnit.Second);
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
                        MpvContext.SetPropertyFlag(MpvProperties.Paused, true);
                        return;
                    case false when SeekValue - Duration < 0:
                        if (!double.IsNaN(_lastSeekValue))
                        {
                            _lastSeekValue = double.NaN;
                            return;
                        }
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
            _seekTimeCode.SetExactUnits(value, TimeCodeUnit.Second);
            
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

    public double VideoHeight { get; private set; }

    public void SetWindowSize()
    {
        if (SelectedVideoTrack is null) return;
        
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
        
        var videoSourceHeight = (int)SelectedVideoTrack.VideoDemuxHeight!;
        _log.Verbose("videoSourceHeight: {A}", videoSourceHeight);
        
        if (videoSourceHeight + panelHeightDiff + systemDecorations >= maxHeight)
            VideoHeight = maxHeight - systemDecorations - panelHeightDiff;
        else
            VideoHeight = videoSourceHeight;
        
        VideoHeight /= scaling;
        _log.Verbose("VideoHeight: {A}", VideoHeight);
        
        // Calculate aspect ratio
        var displayAspectRatio = (double)SelectedVideoTrack.VideoDemuxWidth! / (double)SelectedVideoTrack.VideoDemuxHeight!;
        _log.Verbose("displayAspectRatio: {A}", displayAspectRatio);
        // Account for sample/pixel aspect ratio if needed
        if (SelectedVideoTrack.VideoDemuxPar != null)
        {
            displayAspectRatio *= (double)SelectedVideoTrack.VideoDemuxPar;
            _log.Verbose("VideoDemuxPar: {A}", (double)SelectedVideoTrack.VideoDemuxPar);
        }
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
            mainWindow.Width = desiredWidth;
            mainWindow.Height = desiredHeight;
        });
    }
    
    public static void CenterWindow() => Dispatcher.UIThread.Post(ViewLocator.Main.CenterWindow);

    public void ResizeAndCenterWindow()
    {
        if (_settings.AutoResize)
            SetWindowSize();
        if (_settings.AutoCenter)
            CenterWindow();
        if (_settings.AutoFocus)
            Dispatcher.UIThread.Post(ViewLocator.Main.Activate);
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
                return;
            }
            MpvContext.Command(MpvCommands.Cycle, "pause");
        }
        else if (File.Exists(MediaPath))
        {
            LoadFile();
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

    public void LoadFile()
    {
        MpvContext.Command(MpvCommands.LoadFile, MediaPath, "replace");
        MpvContext.SetPropertyFlag(MpvProperties.Paused, false);
    }
    
    public void LoadFile(string mediaPath)
    {
        if (File.Exists(mediaPath))
        {
            MediaPath = mediaPath;
            LoadFile();
        }
        else
        {
            ViewModelLocator.Main.ShowErrorMessage(_log, "Attempted to load a non-existent file in the path \"{0}\"", mediaPath);
        }
    }
    
    private void SetSliderValueNoSeek(double val)
    {
        _seekValue = val;
        _seekTimeCode.SetExactUnits(val, TimeCodeUnit.Second);
        this.RaisePropertyChanged(nameof(SeekValue));
        this.RaisePropertyChanged(nameof(SeekTimeCodeString));
    }

    private static WindowState GetMainWindowState() => Dispatcher.UIThread.Invoke(() => ViewLocator.Main.WindowState);
}