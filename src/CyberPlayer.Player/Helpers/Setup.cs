using System.Net.Http;
using CyberPlayer.Player.AppSettings;
using CyberPlayer.Player.ViewModels;
using CyberPlayer.Player.Views;
using Splat;

namespace CyberPlayer.Player.Helpers;

public static class Setup
{
    public static void Register()
    {
        var container = Locator.CurrentMutable;
        
        container.RegisterConstant(Settings.Import(BuildConfig.SettingsPath));
        container.RegisterLazySingleton(() => new HttpClient());
        
        container.RegisterLazySingleton(() => new MainWindow());
        container.Register(() => new ProgressView());
        container.Register(() => new MessagePopupView());
        
        container.Register(() => new ProgressViewModel());
        container.Register(() => new MessagePopupViewModel());
        
        SplatRegistrations.RegisterLazySingleton<MpvPlayer>();
        SplatRegistrations.RegisterLazySingleton<MainWindowViewModel>();
        
        SplatRegistrations.SetupIOC();
    }
}