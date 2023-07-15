using System.Net.Http;
using CyberPlayer.Player.AppSettings;
using CyberPlayer.Player.ViewModels;
using CyberPlayer.Player.Views;
using HanumanInstitute.MvvmDialogs.Avalonia;
using Splat;

namespace CyberPlayer.Player
{
    public static class ViewModelLocator
    {
        static ViewModelLocator()
        {
            var container = Locator.CurrentMutable;

            var viewLocator = new StrongViewLocator()
                .Register<MainWindowViewModel, MainWindow>();
            
            container.Register(() => viewLocator);
            container.RegisterLazySingleton(() => Settings.Import(BuildConfig.SettingsPath));
            
            container.RegisterLazySingleton(() => new HttpClient());
            
            SplatRegistrations.Register<MpvPlayer>();
            SplatRegistrations.Register<MainWindowViewModel>();
            
            SplatRegistrations.SetupIOC();
        }

        public static MainWindowViewModel Main => Locator.Current.GetService<MainWindowViewModel>()!;
    }
}