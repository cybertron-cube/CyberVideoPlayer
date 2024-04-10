using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Serilog;

namespace CyberPlayer.Player
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindowVm = ViewModelLocator.Main;
                if (desktop.Args!.Length != 0 && File.Exists(desktop.Args[0]))
                {
                    mainWindowVm.MpvPlayer.MediaPath = desktop.Args[0];
                }
                
                if (OperatingSystem.IsMacOS())
                {
                    var activatable = (IActivatableApplicationLifetime)ApplicationLifetime;
                    activatable.Activated += ActivatableOnActivated;
                }

                var mainWindow = ViewLocator.Main;
                mainWindow.DataContext = mainWindowVm;
                
                desktop.MainWindow = mainWindow;
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private static void ActivatableOnActivated(object? sender, ActivatedEventArgs e)
        {
            if (e is not ProtocolActivatedEventArgs { Kind: ActivationKind.OpenUri } protocolArgs) return;
            
            Log.Information($"App activated via Uri: {protocolArgs.Uri}\nLocal Path: {protocolArgs.Uri.LocalPath}");
            
            Dispatcher.UIThread.Post(() =>
            {
                ViewModelLocator.Main.MpvPlayer.LoadFile(protocolArgs.Uri.LocalPath);
            }, DispatcherPriority.Input);
        }
    }
}