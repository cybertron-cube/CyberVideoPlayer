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

    protected static Screens Screens => MainWindow.Screens;

    protected static double PanelHeightDifference => GetPanelHeightDifference();

    //protected static bool MenuBarEnabled => MainWindow.MenuBar.IsVisible;

    private static readonly MainWindow MainWindow;

    static ViewModelBase()
    {
        MainWindow = Locator.Current.GetService<MainWindow>()!;
        AppExiting = MainWindow.Events().Closed;
    }

    protected static void ExitApp(EventArgs? args = null)
    {
        MainWindow.Close();
    }

    private static double GetPanelHeightDifference()
    {
        //Mac decorations = 28
        //Find out ubuntu decorations
        //
        
        //MenuBar = 33 ?? could be enabled/disabled
        //Seek/Info Bar = 110
        
        double panelHeightDifference;
        if (MainWindow.MenuBar.IsVisible)
        {
            panelHeightDifference = MainWindow.MainGrid.RowDefinitions[0].Height.Value +
                   MainWindow.MainGrid.RowDefinitions[2].Height.Value;
        }
        else
        {
            panelHeightDifference = MainWindow.MainGrid.RowDefinitions[2].Height.Value;
        }

        if (OperatingSystem.IsMacOS())
        {
            panelHeightDifference += 28;
        }

        return panelHeightDifference;
    }
}