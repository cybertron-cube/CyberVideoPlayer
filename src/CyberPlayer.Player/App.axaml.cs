using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

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

                var mainWindow = ViewLocator.Main;
                mainWindow.DataContext = mainWindowVm;
                
                desktop.MainWindow = mainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}