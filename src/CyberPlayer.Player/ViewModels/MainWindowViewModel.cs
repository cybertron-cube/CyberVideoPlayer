using ReactiveUI;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CyberPlayer.Player.AppSettings;
using Cybertron;
using Cybertron.CUpdater;
using Splat;
using CyberPlayer.Player.Business;
using CyberPlayer.Player.Models;
using CyberPlayer.Player.Services;
using CyberPlayer.Player.Views;
using Cybertron.CUpdater.Github;
using LibMpv.Client;
using ReactiveUI.Fody.Helpers;
using Serilog;
using ILogger = Serilog.ILogger;

namespace CyberPlayer.Player.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly ILogger _log;
    private IStorageFolder? _lastFolderLocation;
    
    public Settings Settings { get; }
    
    public MpvPlayer MpvPlayer { get; }
        
    public ReactiveCommand<Unit, Unit> CheckForUpdatesCommand { get; }
        
    public ReactiveCommand<Unit, Unit> MediaPickerCommand { get; }
        
    public ReactiveCommand<string, Unit> OpenWebLinkCommand { get; }
        
    public ReactiveCommand<EventArgs?, Unit> ExitAppCommand { get; }
    
    public ReactiveCommand<VideoInfoType, Unit> ViewVideoInfoCommand { get; }
    
    public ReactiveCommand<Unit, Unit> CenterResizeCommand { get; }
    
    public ReactiveCommand<Unit, Unit> CenterCommand { get; }
    
    public ReactiveCommand<Unit, Unit> ResizeCommand { get; }
    
    [Reactive]
    public object? VideoContent { get; set; }

    [Reactive]
    public object? SeekContent { get; set; }

#if DEBUG
    //For previewer
    public MainWindowViewModel()
    {
        Settings = new Settings();
        libmpv.RootPath = AppDomain.CurrentDomain.BaseDirectory;
        MpvPlayer = new MpvPlayer(Log.ForContext<MpvPlayer>(), Settings);
        
        //ToString() returns $"Track {Id}: {Codec}, {AudioDemuxSampleRate} Hz, {AudioDemuxChannels}";
        var selected = new TrackInfo()
            { Id = 0, Codec = "codec", AudioDemuxSampleRate = 0, AudioDemuxChannels = "channels" };
        MpvPlayer.AudioTrackInfos = new[]
        {
            selected,
            new TrackInfo() { Id = 1, Codec = "codec", AudioDemuxSampleRate = 1, AudioDemuxChannels = "channels" },
            new TrackInfo() { Id = 2, Codec = "codec", AudioDemuxSampleRate = 2, AudioDemuxChannels = "channels" }
        };
        MpvPlayer.SelectedAudioTrack = selected;
            
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

        AppExiting.Subscribe(_ =>
        {
            MpvPlayer.MpvContext.Dispose();
            Settings.Export(BuildConfig.SettingsPath);
        });
        
        CheckForUpdatesCommand = ReactiveCommand.CreateFromTask(CheckForUpdates);
        MediaPickerCommand = ReactiveCommand.CreateFromTask(MediaPicker);
        OpenWebLinkCommand = ReactiveCommand.Create<string>(GenStatic.OpenWebLink);
        ExitAppCommand = ReactiveCommand.Create<EventArgs?>(ExitApp);
        ViewVideoInfoCommand = ReactiveCommand.Create<VideoInfoType>(this.ShowVideoInfo);
        CenterResizeCommand = ReactiveCommand.Create(() => { MpvPlayer.SetWindowSize(); MpvPlayer.CenterWindow(); });
        CenterCommand = ReactiveCommand.Create(MpvPlayer.CenterWindow);
        ResizeCommand = ReactiveCommand.Create(MpvPlayer.SetWindowSize);
        
        CheckForUpdatesCommand.ThrownExceptions.Subscribe(HandleCommandExceptions);
        ViewVideoInfoCommand.ThrownExceptions.Subscribe(HandleCommandExceptions);
        CenterResizeCommand.ThrownExceptions.Subscribe(HandleCommandExceptions);
        CenterCommand.ThrownExceptions.Subscribe(HandleCommandExceptions);
        ResizeCommand.ThrownExceptions.Subscribe(HandleCommandExceptions);
    }

    private async void HandleCommandExceptions(Exception ex)
    {
        _log.Error(ex, "{Message}", ex.Message);
        await this.ShowMessagePopupAsync(MessagePopupButtons.Ok, "An error occured", ex.Message, new PopupParams());
    }

    private bool UpdaterAssetResolver(GithubAsset githubAsset)
    {
        return githubAsset.name.Contains(BuildConfig.AssetIdentifierPlatform)
               && githubAsset.name.Contains(BuildConfig.AssetIdentifierArchitecture)
               && !githubAsset.name.Contains("setup");
    }
    
    private async Task CheckForUpdates()
    {
        _log.Information("Checking for updates...");
        var result = await Updater.GithubCheckForUpdatesAsync(
            "CyberVideoPlayer",
            "https://api.github.com/repos/cybertron-cube/CyberVideoPlayer",
            BuildConfig.Version,
            UpdaterAssetResolver,
            Settings.UpdaterIncludePreReleases,
            Locator.Current.GetService<HttpClient>()!
            );
            
        if (result.UpdateAvailable)
        {
            _log.Information("Latest github release found\nTagName: {TagName}\nBody:\n{Body}",
                result.TagName,
                result.Body);
            
            var msgBoxResult = await this.ShowMessagePopupAsync(MessagePopupButtons.YesNo,
                "Would you like to update?",
                TempWebLinkFix(result.Body),
                new PopupParams(PopupSize: 0.7));

            if (msgBoxResult != MessagePopupResult.Yes) return;

            if (result.DownloadLink == null)
            {
                await this.ShowMessagePopupAsync(MessagePopupButtons.Ok,
                    "An error occurred",
                    $"This build was not included in release {result.TagName}",
                    new PopupParams());
                return;
            }
            
            var updaterPath = GenStatic.GetFullPathFromRelative(BuildConfig.UpdaterPath);
            GenStatic.Platform.ExecutablePath(ref updaterPath);
            
            var tempScript = Updater.StartUpdater(updaterPath,
                result.DownloadLink, 
                GenStatic.GetFullPathFromRelative(),
                BuildConfig.WildCardPreservables,
                BuildConfig.Preservables);
            
            _log.Information("Wrote temporary updater script to \"{ScriptPath}\"", tempScript);
            
            ExitApp();
        }
        else
        {
            await this.ShowMessagePopupAsync(MessagePopupButtons.Ok,
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
            
        using (var ffmpeg = new FFmpeg(MpvPlayer.MediaPath, Settings))
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
        _log.Information("FFmpeg result: {ExitCode}, {ErrorMessage}", result.ExitCode, result.ErrorMessage);
            
        //TODO CHECK IF FILE ALREADY EXISTS - ffmpeg args contain -y so will overwrite but should make prompt
        //TODO subscribe to progress change event to update progressbar
        //TODO show error if not zero
    }

    public void ExportWindow()
    {
        var viewModel = new ExportWindowViewModel(MpvPlayer, Settings);
        viewModel.AudioTrackInfos = MpvPlayer.AudioTrackInfos;
        var view = new ExportWindow();
        view.ViewModel = viewModel;
        view.Show();
    }

    public async Task Export(string args)
    {
        FFmpeg.FFmpegResult result;
        CancellationTokenSource cts = new();
        var dialog = this.GetProgressPopup(new PopupParams());
        dialog.ProgressLabel = "Exporting...";
        var closed = false;
        dialog.Closing.Subscribe(x =>
        {
            if (x)
            {
                cts.Cancel();
                closed = true;
            }
        });
            
        using (var ffmpeg = new FFmpeg(MpvPlayer.MediaPath, Settings))
        {
            ffmpeg.ProgressChanged += progress =>
            {
                dialog.ProgressValue = progress;
                Debug.WriteLine("PROGRESS: " + progress);
            };

            await dialog.OpenAsync();
                
            result = await ffmpeg.FFmpegCommandAsync(MpvPlayer.TrimStartTimeCode,
                MpvPlayer.TrimEndTimeCode,
                "CustomCommand",
                args,
                cts.Token);
        }

        if (!closed)
        {
            await dialog.CloseAsync();
        }
            
        Debug.WriteLine(result.ExitCode);
        Debug.WriteLine(result.ErrorMessage);
        _log.Information("FFmpeg result: {ExitCode} , {ErrorMessage}", result.ExitCode, result.ErrorMessage);
            
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

        try
        {
            _lastFolderLocation = await result.Single().GetParentAsync();
        }
        catch (Exception e)
        {
            _log.Warning(e, "Could not save previous folder location for open file dialog");
        }
            
        MpvPlayer.LoadFile(mediaPath);
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