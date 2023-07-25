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
                .Register<MainWindowViewModel, MainWindow>()
                .Register<ProgressViewModel, ProgressView>()
                .Register<MessagePopupViewModel, MessagePopupView>();
            
            container.Register(() => viewLocator);
        }

        public static MainWindowViewModel Main => Locator.Current.GetService<MainWindowViewModel>()!;
        
        public static ProgressViewModel Progress => Locator.Current.GetService<ProgressViewModel>()!;

        public static MessagePopupViewModel Message => Locator.Current.GetService<MessagePopupViewModel>()!;
    }
}