using System;
using Avalonia.Controls;
using Avalonia.Threading;
using CyberPlayer.Player.Views;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;

namespace CyberPlayer.Player.ViewModels;

public class ViewModelBase : ReactiveObject
{
    protected static readonly IObservable<EventArgs?> AppExiting;

    private static readonly MainWindow MainWindow;

    protected static WindowState GetMainWindowState() => Dispatcher.UIThread.Invoke(() => MainWindow.WindowState);
    
    static ViewModelBase()
    {
        MainWindow = ViewLocator.Main;
        AppExiting = MainWindow.Events().Closing;
    }

    protected static void ExitApp(EventArgs? args = null)
    {
        MainWindow.Close();
    }
}