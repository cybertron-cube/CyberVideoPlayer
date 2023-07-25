using ReactiveUI;
using System;
using System.Diagnostics;
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
using Splat;
using CyberPlayer.Player.Business;
using CyberPlayer.Player.Services;
using ILogger = Serilog.ILogger;

namespace CyberPlayer.Player.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly ILogger _log;
        public readonly Settings Settings;
        
        private MpvPlayer _mpvPlayer;

        public MpvPlayer MpvPlayer
        {
            get => _mpvPlayer;
            set => this.RaiseAndSetIfChanged(ref _mpvPlayer, value);
        }

#if DEBUG
        //For previewer
        public MainWindowViewModel()
        {
            Settings = new Settings();
            _mpvPlayer = new MpvPlayer(Settings);
            
            CheckForUpdatesCommand = ReactiveCommand.CreateFromTask(CheckForUpdates);
            //ExitAppCommand = ReactiveCommand.Create<EventArgs?>(ExitApp);
        }
#endif
        
        [DependencyInjectionConstructor]
        public MainWindowViewModel(ILogger logger, Settings settings, MpvPlayer mpvPlayer)
        {
            _log = logger.ForContext<MainWindowViewModel>();
            Settings = settings;
            _mpvPlayer = mpvPlayer;
            
            CheckForUpdatesCommand = ReactiveCommand.CreateFromTask(CheckForUpdates);
            //ExitAppCommand = ReactiveCommand.Create<EventArgs?>(ExitApp);
        }
        
        public ReactiveCommand<Unit, Unit> CheckForUpdatesCommand { get; }
        
        //public ReactiveCommand<EventArgs?, Unit> ExitAppCommand { get; }
        
        private object? _videoContent;

        public object? VideoContent
        {
            get => _videoContent;
            set => this.RaiseAndSetIfChanged(ref _videoContent, value);
        }

        private object? _seekContent;
        
        public object? SeekContent
        {
            get => _seekContent;
            set => this.RaiseAndSetIfChanged(ref _seekContent, value);
        }

        private async Task CheckForUpdates()
        {
            var result = await Updater.GithubCheckForUpdatesAsync("CyberVideoPlayer",
                new[] { BuildConfig.AssetIdentifierInstance, BuildConfig.AssetIdentifierPlatform },
                "https://api.github.com/repos/cybertron-cube/CyberVideoPlayer",
                BuildConfig.Version.ToString(),
                Locator.Current.GetService<HttpClient>()!,
                Settings.UpdaterIncludePreReleases);
            
            if (result.UpdateAvailable)
            {
                var msgBoxResult = await this.ShowMessagePopup(MessagePopupButtons.YesNo,
                    "Would you like to update?",
                    result.Body,
                    new PopupParams(PopupSize: 0.7));

                if (msgBoxResult != MessagePopupResult.Yes) return;

                if (result.DownloadLink == null)
                {
                    await this.ShowMessagePopup(MessagePopupButtons.Ok,
                        "An error occurred",
                        $"This build was not included in release {result.TagName}",
                        new PopupParams());
                    return;
                }
                
                var updaterPath = GenStatic.GetFullPathFromRelative(BuildConfig.UpdaterPath);
                GenStatic.GetOSRespectiveExecutablePath(ref updaterPath);
                Updater.StartUpdater(updaterPath,
                    result.DownloadLink, 
                    GenStatic.GetFullPathFromRelative(),
                    BuildConfig.WildCardPreservables,
                    BuildConfig.Preservables);

                ExitApp();
            }
            else
            {
                await this.ShowMessagePopup(MessagePopupButtons.Ok,
                    "No updates found",
                    "No updates found",
                    new PopupParams());
            }
        }

        public async void Trim()
        {
            FFmpeg.FFmpegResult result;
            CancellationTokenSource cts = new();
            var dialog = this.GetProgressPopup(new PopupParams());
            dialog.ProgressLabel = "Trimming...";
            var closed = false;
            dialog.Closing.Subscribe(x =>
            {
                if (x)
                {
                    cts.Cancel();
                    closed = true;
                }
            });
            
            using (var ffmpeg = new FFmpeg(MpvPlayer.MediaPath))
            {
                ffmpeg.ProgressChanged += progress =>
                {
                    dialog.ProgressValue = progress;
                    Debug.WriteLine("PROGRESS: " + progress);
                };

                await dialog.OpenAsync();
                
                result = await ffmpeg.TrimAsync(MpvPlayer.TrimStartTimeCode, MpvPlayer.TrimEndTimeCode, cts.Token);
            }

            if (!closed)
            {
                await dialog.CloseAsync();
            }
            
            Debug.WriteLine(result.ExitCode);
            Debug.WriteLine(result.ErrorMessage);
            
            //TODO CHECK IF FILE ALREADY EXISTS - ffmpeg args contain -y so will overwrite but should make prompt
            //TODO subscribe to progress change event to update progressbar
            //TODO show error if not zero
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
    }
}