using Avalonia;
using Avalonia.ReactiveUI;
using System;
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
        LogHelper.SetupSerilog();
            
#if SINGLE
        Setup.CheckInstance(args);
#endif
            
        Setup.Register();
            
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
            
#if SINGLE
        Setup.GlobalMutex.ReleaseMutex();
        Setup.GlobalMutex.Dispose();
#endif
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();
}