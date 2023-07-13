using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CyberPlayer.Player.AppSettings;
using CyberPlayer.Player.ViewModels;
using CyberPlayer.Player.Views;

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
            var settings = Settings.Import(BuildConfig.SettingsPath);
            
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindowVm = new MainWindowViewModel(settings);
                if (desktop.Args!.Length != 0 && File.Exists(desktop.Args[0]))
                {
                    mainWindowVm.MediaPath = desktop.Args[0];
                }
                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainWindowVm,
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}