using System;
using CyberPlayer.Player.Controls;
using CyberPlayer.Player.ViewModels;
using CyberPlayer.Player.Views;

namespace CyberPlayer.Player.Services;

public class ProgressPopupHandler : PopupHandler
{
    private readonly ProgressViewModel _content;

    public double ProgressValue
    {
        get => _content.ProgressValue;
        set => _content.ProgressValue = value;
    }

    public string ProgressLabel
    {
        get => _content.LabelText;
        set => _content.LabelText = value;
    }

    public IObservable<bool> Closing => _content.CloseDialog;

    public ProgressPopupHandler(ContentPopup popup, IParentPanelView view) : base(popup, view)
    {
        _content = (ProgressViewModel)((ProgressView)popup.Content!).DataContext!;
    }
}