using CyberPlayer.Player.ViewModels;
using CyberPlayer.Player.Views;
using ReactiveUI;
using Splat;

namespace CyberPlayer.Player;

public static class ViewLocator
{
    public static MainWindow Main =>
        (MainWindow)Locator.Current.GetService<IViewLocator>()!.ResolveView<MainWindowViewModel>()!;
}
