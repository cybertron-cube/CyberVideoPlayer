using Avalonia;
using Avalonia.ReactiveUI;
using System;
using CyberPlayer.Player.AppSettings;
using CyberPlayer.Player.Helpers;

namespace CyberPlayer.Player;

internal class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var (settings, importException) = Settings.Import(BuildConfig.SettingsPath);
        LogHelper.SetupSerilog(settings, importException);
            
        if (!settings.MultipleAppInstances)
            Setup.CheckInstance(args);
            
        Setup.Register(settings);
            
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        if (settings.MultipleAppInstances)
            return;
        
        Setup.GlobalMutex.ReleaseMutex();
        Setup.GlobalMutex.Dispose();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();
}
