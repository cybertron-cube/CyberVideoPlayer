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
using Serilog;

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
        Background = Brushes.Black;
        ShowInTaskbar = false;
        
        UpdatePosition();
        //Dispatcher.UIThread.Post(_parent.Activate);
        Dispatcher.UIThread.Post(UpdateSize); //Try starting player with file
        /*Dispatcher.UIThread.Post(() =>
        {
            SystemDecorations = SystemDecorations.None;
            UpdatePosition();
            UpdateSize();
        });*/

        var parentEvents = _parent.Events();
        _disposables = new CompositeDisposable
        {
            parentEvents.PositionChanged.Subscribe(_ => UpdatePosition()),
            // If there is a window behind the video player, that window is focused, then you focus the main window again
            // the main window will focus without the video window being brought forward with it
            parentEvents.Activated.Skip(1).Subscribe(_ => UpdateFocus()),
            // Video panel should always resize when main window resizes because it is proportionally sized
            _parent.ObservableForProperty(x => x.ClientSize).Subscribe(_ => UpdateSize()),
            this.Events().Closing.Subscribe(_ => _disposables?.Dispose())
        };
    }
    
    private void UpdateFocus()
    {
        // Post the action so that the input events on the main window go through (like the video panel context menu)
        Dispatcher.UIThread.Post(() =>
        {
            if (_ignoreOne)
            {
                _ignoreOne = false;
                return;
            }

            Activate();
            Dispatcher.UIThread.Post(() => _parent.Activate(), DispatcherPriority.MaxValue);
            _ignoreOne = true;
        }, DispatcherPriority.MaxValue);
    }

    private void UpdatePosition()
    {
        Position = _parent.VideoPanel.PointToScreen(new Point(0, 0));
    }

    private void UpdateSize()
    {
        Width = _parent.ClientSize.Width;
        Height = _parent.ClientSize.Height - _parent.MainGrid.RowDefinitions[0].ActualHeight - _parent.MainGrid.RowDefinitions[2].ActualHeight;
    }
}
