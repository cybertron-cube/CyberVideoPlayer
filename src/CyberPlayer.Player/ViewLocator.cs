using CyberPlayer.Player.Views;
using HanumanInstitute.MvvmDialogs.Avalonia;
using Splat;

namespace CyberPlayer.Player;

public class ViewLocator : ViewLocatorBase
{
    protected override string GetViewName(object viewModel) =>
        viewModel.GetType().FullName!.Replace("ViewModel", "View");

    public static MainWindow Main => Locator.Current.GetService<MainWindow>()!;
    public static ProgressView Progress => Locator.Current.GetService<ProgressView>()!;
}