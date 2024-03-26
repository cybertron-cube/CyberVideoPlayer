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
            var server = new NamedPipeServerStream(BuildConfig.Guid);
            var cts = new CancellationTokenSource();

            Task.Run(() => StartServerPipe(server), cts.Token);
            
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                cts.Cancel();
            };
        
            AppDomain.CurrentDomain.UnhandledException += (_, _) =>
            {
                cts.Cancel();
            };
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

    private static async Task StartServerPipe(NamedPipeServerStream serverPipe)
    {
        while (true)
        {
            await serverPipe.WaitForConnectionAsync();
            var sr = new StreamReader(serverPipe);
            var filePath = await sr.ReadToEndAsync();
            
            var player = Locator.Current.GetService<MpvPlayer>();
            var mainWindow = Locator.Current.GetService<MainWindow>();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                mainWindow?.Activate();
                player?.LoadFile(filePath);
            });
        
            serverPipe.Disconnect();
        }
    }
#endif
    
    public static void Register()
    {
        var container = Locator.CurrentMutable;
        var settings = Settings.Import(BuildConfig.SettingsPath);
        if (!string.IsNullOrWhiteSpace(settings.LibMpvDir))
            libmpv.RootPath = settings.LibMpvDir;

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