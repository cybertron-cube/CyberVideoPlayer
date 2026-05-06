using ReactiveUI.SourceGenerators;

namespace CyberPlayer.Player.ViewModels;

public partial class SeekViewModel : ViewModelBase
{
    [Reactive]
    private MpvPlayer _mpvPlayer;

    public SeekViewModel(MpvPlayer mpvPlayer)
    {
        _mpvPlayer = mpvPlayer;
    }
}
