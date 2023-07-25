using System;

namespace CyberPlayer.Player.ViewModels;

public interface IDialogContent
{
    public IObservable<bool> CloseDialog
    {
        get;
    }
}