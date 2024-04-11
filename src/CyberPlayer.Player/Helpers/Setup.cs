using System;
using System.IO;
using System.IO.Pipes;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CyberPlayer.Player.AppSettings;
using CyberPlayer.Player.ViewModels;
using CyberPlayer.Player.Views;
using LibMpv.Client;
using Serilog;
using Splat;

namespace CyberPlayer.Player.Helpers;

public static class Setup
{
#if SINGLE
    public static readonly Mutex GlobalMutex = new(true, BuildConfig.MutexId);
    
    public static void CheckInstance(string[] args)
    {
        if (GlobalMutex.WaitOne(TimeSpan.Zero, true))
        {
            var cts = new CancellationTokenSource();

            var serverPipeTask = Task.Factory.StartNew(StartServerPipe, cts.Token,
                TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach).Unwrap();

            void OnExit(object? o, EventArgs eventArgs)
            {
                cts.Cancel();
                serverPipeTask.GetAwaiter().GetResult();
            }

            AppDomain.CurrentDomain.ProcessExit += OnExit;
            AppDomain.CurrentDomain.UnhandledException += OnExit;
        }
        else
        {
            var filePath = args[0];
            if (File.Exists(filePath))
            {
                var client = new NamedPipeClientStream(BuildConfig.Guid);
                var sw = new StreamWriter(client);
                client.Connect(TimeSpan.FromSeconds(5));
                sw.Write(filePath);
                sw.Flush();
                sw.Close();
                client.Dispose();
            }
            
            Environment.Exit(0);
        }
    }

    private static async Task StartServerPipe(object? cancellationToken)
    {
        if (cancellationToken is not CancellationToken ct)
            throw new ArgumentException($"Must be of type {typeof(CancellationToken)}", nameof(cancellationToken));
        
        var serverPipe = new NamedPipeServerStream(BuildConfig.Guid);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                await serverPipe.WaitForConnectionAsync(ct);
                var sr = new StreamReader(serverPipe);
                var filePath = await sr.ReadToEndAsync(ct);

                var player = Locator.Current.GetService<MpvPlayer>();
                var mainWindow = Locator.Current.GetService<MainWindow>();
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    mainWindow?.Activate();
                    player?.LoadFile(filePath);
                }, DispatcherPriority.Input, ct);

                serverPipe.Disconnect();
            }
        }
        catch (OperationCanceledException) { }
    }
#endif
    
    public static void Register()
    {
        var settings = Settings.Import(BuildConfig.SettingsPath);
        if (!string.IsNullOrWhiteSpace(settings.LibMpvDir))
            libmpv.RootPath = settings.LibMpvDir;
        Log.Information("Using libmpv from path: \"{LibPath}\"", libmpv.RootPath);

        var container = Locator.CurrentMutable;
        
        container.RegisterConstant(settings);
        container.RegisterLazySingleton(() => new HttpClient());
        
        container.RegisterLazySingleton(() => new MainWindow());
        container.Register(() => new ProgressView());
        container.Register(() => new MessagePopupView());
        container.Register(() => new VideoInfoWindow());
        
        container.Register(() => new ProgressViewModel());
        container.Register(() => new MessagePopupViewModel());
        
        SplatRegistrations.RegisterLazySingleton<MpvPlayer>();
        SplatRegistrations.RegisterLazySingleton<MainWindowViewModel>();
        SplatRegistrations.RegisterLazySingleton<MediaInfoViewModel>();
        SplatRegistrations.RegisterLazySingleton<FFprobeInfoViewModel>();
        SplatRegistrations.RegisterLazySingleton<MpvInfoViewModel>();
        
        SplatRegistrations.SetupIOC();
    }
}