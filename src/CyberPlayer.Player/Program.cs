using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.Diagnostics.CodeAnalysis;
using CyberPlayer.Player.Helpers;
using CyberPlayer.Player.ViewModels;

namespace CyberPlayer.Player
{
    internal class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MpvPlayer))]
        [STAThread]
        public static void Main(string[] args)
        {
            LogHelper.SetupSerilog();
            Setup.Register();
            
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();
    }
}