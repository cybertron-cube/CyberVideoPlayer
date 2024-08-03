using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
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
                    var activatable = Current?.TryGetFeature<IActivatableLifetime>();
                    if (activatable != null) activatable.Activated += ActivatableOnActivated;
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
            string mediaPath;

            switch (e)
            {
                case ProtocolActivatedEventArgs { Kind: ActivationKind.OpenUri } protocolArgs:
                    Log.Information("App activated via Uri: {Uri}\nLocal Path: {Path}", protocolArgs.Uri, protocolArgs.Uri.LocalPath);
                    mediaPath = protocolArgs.Uri.LocalPath;
                    break;
                case FileActivatedEventArgs { Kind: ActivationKind.File } fileArgs:
                    var allPaths = string.Join(", ", fileArgs.Files.Select(x => x.Path.LocalPath));
                    Log.Information("App activated via file, local path/s: {Paths}", allPaths);
                    mediaPath = fileArgs.Files[0].Path.LocalPath;
                    break;
                default:
                    Log.Verbose("App activated, Kind: {Kind}", e.Kind);
                    return;
            }
            
            Dispatcher.UIThread.Post(() =>
            {
                ViewModelLocator.Main.MpvPlayer.LoadFile(mediaPath);
            }, DispatcherPriority.Input);
        }
    }
}