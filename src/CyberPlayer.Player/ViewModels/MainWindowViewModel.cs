using ReactiveUI;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CyberPlayer.Player.AppSettings;
using Cybertron;
using Cybertron.CUpdater;
using Splat;
using CyberPlayer.Player.Business;
using CyberPlayer.Player.Services;
using ReactiveUI.Fody.Helpers;
using ILogger = Serilog.ILogger;

namespace CyberPlayer.Player.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly ILogger _log;
        public readonly Settings Settings;
        
        [Reactive]
        public MpvPlayer MpvPlayer { get; set; }

#if DEBUG
        //For previewer
        public MainWindowViewModel()
        {
            Settings = new Settings();
            MpvPlayer = new MpvPlayer(Settings);
            
            CheckForUpdatesCommand = ReactiveCommand.CreateFromTask(CheckForUpdates);
            MediaPickerCommand = ReactiveCommand.CreateFromTask(MediaPicker);
            //ExitAppCommand = ReactiveCommand.Create<EventArgs?>(ExitApp);
        }
#endif
        
        [DependencyInjectionConstructor]
        public MainWindowViewModel(ILogger logger, Settings settings, MpvPlayer mpvPlayer)
        {
            _log = logger.ForContext<MainWindowViewModel>();
            Settings = settings;
            MpvPlayer = mpvPlayer;
            
            CheckForUpdatesCommand = ReactiveCommand.CreateFromTask(CheckForUpdates);
            MediaPickerCommand = ReactiveCommand.CreateFromTask(MediaPicker);
            //ExitAppCommand = ReactiveCommand.Create<EventArgs?>(ExitApp);
        }
        
        public ReactiveCommand<Unit, Unit> CheckForUpdatesCommand { get; }
        
        public ReactiveCommand<Unit, Unit> MediaPickerCommand { get; }
        
        //public ReactiveCommand<EventArgs?, Unit> ExitAppCommand { get; }
        
        [Reactive]
        public object? VideoContent { get; set; }

        [Reactive]
        public object? SeekContent { get; set; }

        private IStorageFolder? _lastFolderLocation;

        private async Task CheckForUpdates()
        {
            _log.Information("Checking for updates...");
            var result = await Updater.GithubCheckForUpdatesAsync("CyberVideoPlayer",
                new[] { BuildConfig.AssetIdentifierInstance, BuildConfig.AssetIdentifierPlatform },
                "https://api.github.com/repos/cybertron-cube/CyberVideoPlayer",
                BuildConfig.Version.ToString(),
                Locator.Current.GetService<HttpClient>()!,
                Settings.UpdaterIncludePreReleases);
            
            _log.Information("Latest github release found\nTagName: {TagName}\nBody:\n{Body}",
                result.TagName,
                result.Body);
            
            if (result.UpdateAvailable)
            {
                var msgBoxResult = await this.ShowMessagePopup(MessagePopupButtons.YesNo,
                    "Would you like to update?",
                    TempWebLinkFix(result.Body),
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
                    "",
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

        private async Task MediaPicker()
        {
            var result = await this.OpenFileDialog(new FilePickerOpenOptions
                { AllowMultiple = false, Title = "Pick a video file", SuggestedStartLocation = _lastFolderLocation });
            
            var mediaPath = result.SingleOrDefault()?.Path.LocalPath;
            if (mediaPath == null) return;

            _lastFolderLocation = await result.Single().GetParentAsync();
            
            MpvPlayer.LoadFile(mediaPath);
        }

        private void ExitApp(EventArgs? e = null)
        {
            if (e == null)
            {
                var app = (IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!;
                app.Shutdown();
                return;
                //This method will be called again by MainWindow with event args
            }
            //TODO save settings
            //TODO Do anything else needed to when shutting down normally 
        }
        
        private static string TempWebLinkFix(string markdown)
        {
            var regex = new Regex(@"\*\*Full Changelog\*\*: (?<url>https://github\.com/.*)");
            Match match = regex.Match(markdown);
            if (match.Success)
            {
                var url = match.Groups["url"].Value;
                var oldValue = match.Groups[0].Value;
                var newValue = "[%{color:blue}**Full Changelog**%](" + url + ")";
                markdown = markdown.Replace(oldValue, newValue);
            }
            return markdown;
        }
    }
}