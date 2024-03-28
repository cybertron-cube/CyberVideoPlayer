using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ReactiveMarbles.ObservableEvents;
using Serilog;

namespace CyberPlayer.Player.Views;

public class VideoOverlayWindow : Window
{
    private readonly CompositeDisposable? _disposables;

    private readonly MainWindow _parent;
    
    public VideoOverlayWindow(MainWindow parent)
    {
        _parent = parent;
        
        var panel = new Border
        {
            Background = Brushes.Green,
            Opacity = 0.3
            //CornerRadius = new CornerRadius(0, 0, 10, 10)
        };
        Content = panel;
        SystemDecorations = SystemDecorations.None;
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        Background = Brushes.Transparent;
        
        Log.Debug("Parent: {ParentPosition}", _parent.Position);
        UpdatePosition();
        UpdateSize();
        
        ShowInTaskbar = false;
        
        //maximize/minimize/fullscreen
        //native menu?
        
        panel.ContextMenu = _parent.VideoPanel.ContextMenu;
        panel.ContextMenu.DataContext = _parent.ViewModel;
        
        panel.PointerPressed += _parent.VideoPanel_OnPointerPressed;
        //panel.DoubleTapped += VideoPanel_OnDoubleTapped;

        var parentEvents = _parent.Events();
        
        var pointerEnteredVideoPanel = Observable.FromEventPattern<PointerEventArgs>(action => _parent.VideoPanel.AddHandler(PointerEnteredEvent, action),
            action => _parent.VideoPanel.RemoveHandler(PointerEnteredEvent, action));

        _disposables = new CompositeDisposable
        {
            parentEvents.PositionChanged.Subscribe(_ => UpdatePosition()),
            // Video panel should always resize when main window resizes because it is proportionally sized
            parentEvents.SizeChanged.Subscribe(x => UpdateSize()),
            // On macOS sometimes this window is somehow put below the parent when interacting with the parent
            pointerEnteredVideoPanel.Subscribe(_ => { Activate(); _parent.Activate(); }),
            this.Events().Closing.Subscribe(_ => _disposables?.Dispose())
        };
    }

    private void UpdatePosition()
    {
        Position = _parent.VideoPanel.PointToScreen(new Point(0, 0));
    }

    private void UpdateSize()
    {
        Width = _parent.VideoPanel.Bounds.Width;
        Height = _parent.VideoPanel.Bounds.Height;
    }
}