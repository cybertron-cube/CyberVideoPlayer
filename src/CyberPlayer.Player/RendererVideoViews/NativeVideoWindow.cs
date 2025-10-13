using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CyberPlayer.Player.Views;
using LibMpv.Context;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;

namespace CyberPlayer.Player.RendererVideoViews;

public class NativeVideoWindow : Window
{
    private readonly MainWindow _parent;
    private readonly CompositeDisposable _disposables;
    private bool _ignoreOne;
    
    public NativeVideoWindow(MainWindow parent, MpvContext mpvContext)
    {
        _parent = parent;
        
        Content = new NativeVideoView
        {
            MpvContext = mpvContext
        };
        SystemDecorations = SystemDecorations.None;
        ShowInTaskbar = false;
        
        Loaded += (_, _) =>
        {
            IsVisible = false;
            _parent.Activate();
        };

        var parentEvents = _parent.Events();
        _disposables = new CompositeDisposable
        {
            parentEvents.PositionChanged.Subscribe(_ => UpdatePosition()),
            // If there is a window behind the video player, that window is focused, then you focus the main window again
            // the main window will focus without the video window being brought forward with it
            parentEvents.Activated.Skip(1).Subscribe(_ => UpdateFocus()),
            // Video panel should always resize when main window resizes because it is proportionally sized
            _parent.ObservableForProperty(x => x.VideoPanel.Bounds).Subscribe(_ => UpdateSize()),
            // Make window visible on first size change, otherwise you get a black box flicker for a second
            _parent.ObservableForProperty(x => x.VideoPanel.Bounds).Take(1).Subscribe(_ => FirstSize()),
            this.Events().Closing.Subscribe(_ => _disposables?.Dispose())
        };
    }

    private void FirstSize()
    {
        Dispatcher.UIThread.Post(() =>
        {
            _parent.TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
            _parent.Background = Brushes.Transparent;
            _parent.VideoPanel.Background = Brushes.Transparent;
            IsVisible = true;
            _parent.Activate();
        });
    }
    
    private void UpdateFocus()
    {
        // We activate the parent below which would cause infinite recursion if we don't check
        if (_ignoreOne)
        {
            _ignoreOne = false;
            return;
        }
        
        // Post the action so that the input events on the main window go through (like the video panel context menu)
        Dispatcher.UIThread.Post(() =>
        {
            Activate();
            
            Dispatcher.UIThread.Post(() =>
            {
                _ignoreOne = true;
                _parent.Activate();
            }, DispatcherPriority.MaxValue);
            
        }, DispatcherPriority.MaxValue);
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
