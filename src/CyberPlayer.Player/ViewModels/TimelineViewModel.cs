using ReactiveUI.SourceGenerators;

namespace CyberPlayer.Player.ViewModels;

public partial class TimelineViewModel : ViewModelBase
{
    [Reactive]
    private MpvPlayer _mpvPlayer;

    public TimelineViewModel(MpvPlayer mpvPlayer)
    {
        _mpvPlayer = mpvPlayer;
    }
}
