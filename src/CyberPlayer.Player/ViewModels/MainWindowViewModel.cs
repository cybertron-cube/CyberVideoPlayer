using Avalonia.Threading;
using LibMpv.Client;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using AvaloniaMessageBox;
using CyberPlayer.Player.AppSettings;
using Cybertron;
using Cybertron.CUpdater;
using static Cybertron.TimeCode;
using CyberPlayer.Player.Business;

namespace CyberPlayer.Player.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public Settings Settings;
        
        private MpvContext _mpvContext = new();

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
        public MainWindowViewModel(Settings settings)
        {
            Settings = settings;
            
            MpvContext.FileLoaded += MpvContext_FileLoaded;
            MpvContext.EndFile += MpvContext_EndFile;

            _seekTimeCode = new TimeCode(0);
            _durationTimeCode = new TimeCode(1);
            _trimStartTimeCode = new TimeCode(0);
            _trimEndTimeCode = new TimeCode(1);
            _timeCodeStartIndex = 0;
            _timeCodeLength = Settings.TimeCodeLength;

            FrameStepCommand = ReactiveCommand.Create<string>(FrameStep);
            SeekCommand = ReactiveCommand.Create<double>(Seek);
            CheckForUpdatesCommand = ReactiveCommand.CreateFromTask(CheckForUpdates);
            //ExitAppCommand = ReactiveCommand.Create<EventArgs?>(ExitApp);

            ShowMessageBox = new Interaction<MessageBoxParams, MessageBoxResult>();

            UpdateSliderTaskCTS = new CancellationTokenSource();
            Task.Run(() => UpdateSliderValueLoop(UpdateSliderTaskCTS.Token));
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
            //SeekValue = Duration;
        }

        private void MpvContext_FileLoaded(object? sender, EventArgs e)
        {
            Debug.WriteLine("File Loaded");
            IsFileLoaded = true;
            //VolumeValue = _volumeValue; //MpvContext.SetPropertyString("volume", VolumeValue.ToString("0"));
            if (_reloadFile)
            {
                _reloadFile = false;
                IsPlaying = true;
            }
            else if (!double.IsNaN(_lastSeekValue))
            {
                Debug.WriteLine("CALLED 2");
                //SeekValue = _lastSeekValue;
                Seek();
                //_lastSeekValue = double.NaN;
                MpvContext.SetPropertyFlag(MpvProperties.Paused, true);
            }
            else //loading new file
            {
                Duration = MpvContext.GetPropertyDouble(MpvProperties.Duration);
                TrimEndTime = Duration;
                if (_durationTimeCode.Hours == 0)
                {
                    _timeCodeStartIndex = 3;
                    _timeCodeLength = Settings.TimeCodeLength - _timeCodeStartIndex;
                }
                else
                {
                    _timeCodeStartIndex = 0;
                    _timeCodeLength = Settings.TimeCodeLength;
                }
                this.RaisePropertyChanged(nameof(TrimStartTimeCodeString));
                this.RaisePropertyChanged(nameof(TrimEndTimeCodeString));
                this.RaisePropertyChanged(nameof(DurationTimeCodeString));
                this.RaisePropertyChanged(nameof(SeekTimeCodeString));
            }
            Debug.WriteLine(Duration);
        }
        
        public ReactiveCommand<string, Unit> FrameStepCommand { get; }
        public ReactiveCommand<double, Unit> SeekCommand { get; }
        
        public ReactiveCommand<Unit, Unit> CheckForUpdatesCommand { get; }
        
        //public ReactiveCommand<EventArgs?, Unit> ExitAppCommand { get; }

        public Interaction<MessageBoxParams, MessageBoxResult> ShowMessageBox;

        private readonly ManualResetEvent _updateSliderMRE = new(false);
        private bool _initialFileLoaded = false;
        private bool _reloadFile = false;
        private bool _replacingFile = false;
        private double _lastSeekValue = double.NaN;
        private int _timeCodeStartIndex;
        private int _timeCodeLength;


        private object _videoContent;

        public object VideoContent
        {
            get => _videoContent;
            set => this.RaiseAndSetIfChanged(ref _videoContent, value);
        }

        private object _seekContent;
        public object SeekContent
        {
            get => _seekContent;
            set => this.RaiseAndSetIfChanged(ref _seekContent, value);
        }

        private double _trimStartTime;
        public double TrimStartTime
        {
            get => _trimStartTime;
            set
            {
                if (value - _trimStartTime == 0) return;
                _trimStartTime = value;
                _trimStartTimeCode.SetExactUnits(value, TimeUnit.Second);
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(TrimStartTimeCodeString));
            }
        }

        private TimeCode _trimStartTimeCode;
        
        public string TrimStartTimeCodeString => _trimStartTimeCode.FormattedString.Substring(_timeCodeStartIndex, _timeCodeLength);

        private double _trimEndTime = 1;
        
        public double TrimEndTime
        {
            get => _trimEndTime;
            set
            {
                if (value - _trimEndTime == 0) return;
                _trimEndTime = value;
                _trimEndTimeCode.SetExactUnits(value, TimeUnit.Second);
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(TrimEndTimeCodeString));
            }
        }

        private TimeCode _trimEndTimeCode;

        public string TrimEndTimeCodeString => _trimEndTimeCode.FormattedString.Substring(_timeCodeStartIndex, _timeCodeLength);

        private bool _isFileLoaded;
        public bool IsFileLoaded
        {
            get => _isFileLoaded;
            set => this.RaiseAndSetIfChanged(ref _isFileLoaded, value);
        }
        public CancellationTokenSource UpdateSliderTaskCTS { get; }

        private double _duration = 1;
        public double Duration
        {
            get => _duration;
            set
            {
                if (value - _duration == 0) return;
                _duration = value;
                _durationTimeCode.SetExactUnits(value, TimeUnit.Second);
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(DurationTimeCodeString));
            }
        }

        private TimeCode _durationTimeCode;

        public string DurationTimeCodeString => _durationTimeCode.FormattedString.Substring(_timeCodeStartIndex, _timeCodeLength);
        
        private bool _isPlaying = false;
        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (_isPlaying == value) return;
                _isPlaying = value;
                this.RaisePropertyChanged(nameof(IsPlaying));
                if (value)
                {
                    _updateSliderMRE.Set();
                }
                else
                {
                    _updateSliderMRE.Reset();
                }
                //this.RaiseAndSetIfChanged(ref _isPlaying, value);
            }
        }

        private bool _wasPlaying;
        //private long _isSeeking = 0;
        private bool _isSeeking;
        public bool IsSeeking
        {
            //get => Interlocked.Read(ref _isSeeking) == 1;
            get => _isSeeking;
            set
            {
                //if (value != (Interlocked.Read(ref _isSeeking) == 1))
                if (value == _isSeeking) return;
                //Interlocked.Exchange(ref _isSeeking, Convert.ToInt64(value));
                _isSeeking = value;
                this.RaisePropertyChanged();
                if (IsFileLoaded)
                {
                    switch (value)
                    {
                        case true:
                            /*if (!double.IsNaN(_lastSeekValue))
                            {
                                _lastSeekValue = double.NaN;
                                return;
                            }*/
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
#else
        private string _mediaPath = string.Empty;
#endif
        //D:\Editing\My Clips\Dynasty Double Canswap.mp4
        //H:\makemkv\12 Monkeys- Season Two (Disc 1)\12 Monkeys- Season Two (Disc 1)_t05.mkv
        //H:\makemkv\John Wick\John Wick-FPL_MainFeature_t05.mkv
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
                _seekTimeCode.SetExactUnits(value, TimeUnit.Second);
                
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(SeekTimeCodeString));
                
                if (IsFileLoaded)
                {
                    Seek();
                }
            }
        }

        private TimeCode _seekTimeCode;

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
            set => this.RaiseAndSetIfChanged(ref _isMuted, value);
        }

        private void FrameStep(string param)
        {
            if (IsPlaying)
            {
                IsPlaying = false;
            }
            MpvContext.CommandAsync(0, param);
            UpdateSliderValue();
        }

        public void Mute()
        {
            MpvContext.SetPropertyFlag(MpvProperties.Muted, !IsMuted);
            IsMuted = !IsMuted;
        }
        
        public void PlayPause()
        {
            if (IsFileLoaded)
            {
                if (SeekValue - Duration == 0)
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
                //MpvContext.SetPropertyFlag(MpvProperties.Paused, false);
            }
        }

        private void Seek()
        {
            Dispatcher.UIThread.Post(() =>
            {
                MpvContext.CommandAsync(0, MpvCommands.Seek, SeekValue.ToString("F3"), "absolute");
                //MpvContext.Command(MpvCommands.Seek, SeekValue.ToString(), "absolute");
            });
        }
        
        private void Seek(double offset)
        {
            var newSeekValue = SeekValue + offset;
            if (0 < newSeekValue && newSeekValue < Duration)
            {
                SeekValue = newSeekValue;
            }
            else if (newSeekValue > Duration)
            {
                SeekValue = Duration;
            }
            else
            {
                SeekValue = 0;
            }
        }

        private async Task CheckForUpdates()
        {
            //WARNING No socket reuse
            //HttpClient is instantiated here because this is an app that will likely have a short lifetime
            //This method also will likely not be called as much and not more than once
            var httpClient = new HttpClient();

            //TODO Get body value from json for markdown show changes
            var result = await Updater.GithubCheckForUpdatesAsync("CyberVideoPlayer",
                new[] { BuildConfig.AssetIdentifierInstance, BuildConfig.AssetIdentifierPlatform },
                "https://api.github.com/repos/cybertron-cube/CyberVideoPlayer",
                BuildConfig.Version.ToString(),
                httpClient,
                Settings.UpdaterIncludePreReleases);

            httpClient.Dispose();
            
            if (result.UpdateAvailable)
            {
                //TODO Make popup

                var msgBoxResult = await ShowMessageBox.Handle(new MessageBoxParams
                {
                    Title = "Update Available",
                    Message = "Would you like to update?",
                    Buttons = MessageBoxButtons.YesNo,
                    StartupLocation = WindowStartupLocation.CenterOwner
                });

                if (msgBoxResult != MessageBoxResult.Yes) return;
                
                var updaterPath = GenStatic.GetFullPathFromRelative(BuildConfig.UpdaterPath);
                GenStatic.GetOSRespectiveExecutablePath(ref updaterPath);
                Updater.StartUpdater(updaterPath, result.DownloadLink, GenStatic.GetFullPathFromRelative(), BuildConfig.Preservables);

                ExitApp();
            }
            else
            {
                await ShowMessageBox.Handle(new MessageBoxParams
                {
                    Title = "No updates found",
                    Message = "No updates were found",
                    Buttons = MessageBoxButtons.Ok,
                    StartupLocation = WindowStartupLocation.CenterOwner
                });
            }
        }

        public async void Trim()
        {
            FFmpeg.FFmpegResult result;
            CancellationToken ct = new();
            
            using (var ffmpeg = new FFmpeg.FFmpeg(MediaPath))
            {
                result = await ffmpeg.TrimAsync(_trimStartTimeCode, _trimEndTimeCode, ct);
            }

            Debug.WriteLine(result.ExitCode);
            Debug.WriteLine(result.ErrorMessage);
            
            //TODO CHECK IF FILE ALREADY EXISTS - ffmpeg args contain -y so will overwrite but should make prompt
            //TODO subscribe to progress change event to update progressbar
            //TODO show error if not zero
        }

        public void LoadFile(string? mediaPath = null)
        {
            if (mediaPath != null)
                MediaPath = mediaPath;
            
            if (!_reloadFile && IsFileLoaded)
                _replacingFile = true;
            
            MpvContext.Command("loadfile", MediaPath, "replace");

            if (_replacingFile)
            {
                IsPlaying = true; //TODO Make setting
                return;
            }

            if (!_initialFileLoaded)
            {
                _initialFileLoaded = true;
                IsPlaying = true; //TODO Make setting
            }
        }

        private void ExitApp(EventArgs? e = null)
        {
            if (e == null)
            {
                var app = (IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime;
                app.Shutdown();
                return;
                //This method will be called again by MainWindow with event args
            }
            //TODO save settings
            //TODO Do anything else needed to when shutting down normally 
        }
        
        private void SetSliderValueNoSeek(double val)
        {
            _seekValue = val;
            //_seekTimeCode.TotalSeconds = (int)val;
            _seekTimeCode.SetExactUnits(val, TimeUnit.Second);
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
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    _updateSliderMRE.WaitOne();
                    double result = MpvContext.GetPropertyDouble(MpvProperties.TimePosition);
                    if (result >= 0)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (!IsSeeking)
                                SetSliderValueNoSeek(result);
                        });
                    }
                    await Task.Delay(Settings.SeekRefreshRate, ct);
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
        }
    }
}