using HanumanInstitute.MvvmDialogs.Avalonia;

namespace CyberPlayer.Player;

public class ViewLocator : ViewLocatorBase
{
    protected override string GetViewName(object viewModel) =>
        viewModel.GetType().FullName!.Replace("ViewModel", "View");
}