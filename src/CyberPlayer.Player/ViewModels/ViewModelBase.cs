using System;
using Avalonia.Controls;
using CyberPlayer.Player.Views;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using Splat;

namespace CyberPlayer.Player.ViewModels;

public class ViewModelBase : ReactiveObject
{
    protected static readonly IObservable<EventArgs?> AppExiting;

    private static readonly Window MainWindow;

    static ViewModelBase()
    {
        MainWindow = Locator.Current.GetService<MainWindow>()!;
        AppExiting = MainWindow.Events().Closed;
    }

    protected static void ExitApp(EventArgs? args = null)
    {
        MainWindow.Close();
    }
}