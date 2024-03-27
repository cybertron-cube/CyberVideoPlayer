using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ReactiveMarbles.ObservableEvents;

namespace CyberPlayer.Player.Views;

public class VideoOverlayWindow : Window
{
    private readonly CompositeDisposable? _disposables;
    
    public VideoOverlayWindow(MainWindow parent)
    {
        const int systemDecorationsTop = 28; //only for testing
        
        var panel = new Border
        {
            Background = Brushes.Green,
            //CornerRadius = new CornerRadius(0, 0, 10, 10)
        };
        Content = panel;
        SystemDecorations = SystemDecorations.None;
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        Background = Brushes.Transparent;
        Position = parent.Position.WithY(parent.Position.Y + systemDecorationsTop);
        Height = parent.MainGrid.RowDefinitions[0].ActualHeight + parent.MainGrid.RowDefinitions[1].ActualHeight;
        Width = parent.FrameSize!.Value.Width;
        ShowInTaskbar = false;
        
        //maximize/minimize/fullscreen
        //native menu?
        
        panel.ContextMenu = parent.VideoPanel.ContextMenu;
        panel.ContextMenu.DataContext = parent.ViewModel;
        
        panel.PointerPressed += parent.VideoPanel_OnPointerPressed;
        //panel.DoubleTapped += VideoPanel_OnDoubleTapped;

        var parentEvents = parent.Events();
        
        var pointerEnteredVideoPanel = Observable.FromEventPattern<PointerEventArgs>(action => parent.VideoPanel.AddHandler(PointerEnteredEvent, action),
            action => parent.VideoPanel.RemoveHandler(PointerEnteredEvent, action));
        
        _disposables = new CompositeDisposable
        {
            parentEvents.PositionChanged.Subscribe(x =>
            {
                var newPosition = x.Point.WithY(x.Point.Y + systemDecorationsTop);
                if (parent._menuBarActivated)
                {
                    newPosition += new PixelPoint(0, (int)parent.MainGrid.RowDefinitions[0].ActualHeight);
                }

                Position = newPosition;
            }),
            parentEvents.SizeChanged.Subscribe(x =>
            {
                Width = x.NewSize.Width;
            
                var newHeight = parent.MainGrid.Bounds.Height - parent.MainGrid.RowDefinitions[2].ActualHeight;
                if (parent._menuBarActivated)
                {
                    newHeight -= parent.MainGrid.RowDefinitions[0].ActualHeight;
                }
            
                Height = newHeight;
            }),
            //On macOS sometimes this window somehow is put below the parent when interacting with the parent
            pointerEnteredVideoPanel.Subscribe(_ => { Activate(); parent.Activate(); }),
            this.Events().Closing.Subscribe(_ => _disposables?.Dispose())
        };
    }
}