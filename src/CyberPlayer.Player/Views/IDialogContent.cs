using System;

namespace CyberPlayer.Player.Views;

public interface IDialogContent
{
    public IObservable<bool> CloseDialog
    {
        get;
    }
}