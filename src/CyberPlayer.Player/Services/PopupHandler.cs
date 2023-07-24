using System.Threading.Tasks;
using CyberPlayer.Player.Controls;
using CyberPlayer.Player.Views;

namespace CyberPlayer.Player.Services;

public class PopupHandler
{
    private readonly ContentPopup _popup;
    private readonly IParentPanelView _view;
    
    public PopupHandler(ContentPopup popup, IParentPanelView view)
    {
        _popup = popup;
        _view = view;
    }

    public void Open() => _popup.Open();

    public async Task OpenAsync() => await _popup.OpenAsync();
    
    public void Close()
    {
        _view.MainPanel.Children.Remove(_popup);
    }

    public async Task CloseAsync()
    {
        await _popup.CloseAsync();
        _view.MainPanel.Children.Remove(_popup);
    }
}