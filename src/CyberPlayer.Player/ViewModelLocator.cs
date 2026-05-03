using CyberPlayer.Player.ViewModels;
using Splat;

namespace CyberPlayer.Player
{
    public static class ViewModelLocator
    {
        public static MainWindowViewModel Main => Locator.Current.GetService<MainWindowViewModel>()!;
        
        public static ProgressViewModel Progress => Locator.Current.GetService<ProgressViewModel>()!;

        public static MessagePopupViewModel Message => Locator.Current.GetService<MessagePopupViewModel>()!;

        public static JsonTreeViewModel JsonTree => Locator.Current.GetService<JsonTreeViewModel>()!;

        public static VideoInfoViewModel VideoInfo => Locator.Current.GetService<VideoInfoViewModel>()!;
    }
}