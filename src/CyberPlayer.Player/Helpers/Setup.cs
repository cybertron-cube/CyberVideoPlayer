using System.Net.Http;
using CyberPlayer.Player.AppSettings;
using CyberPlayer.Player.ViewModels;
using Splat;

namespace CyberPlayer.Player.Helpers;

public static class Setup
{
    public static void Register()
    {
        var container = Locator.CurrentMutable;
        
        container.RegisterLazySingleton(() => Settings.Import(BuildConfig.SettingsPath));
        container.RegisterLazySingleton(() => new HttpClient());
        
        SplatRegistrations.Register<MpvPlayer>();
        SplatRegistrations.Register<MainWindowViewModel>();
        
        SplatRegistrations.SetupIOC();
    }
}