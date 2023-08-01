using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Avalonia.Interactivity;
using CyberPlayer.Player.ViewModels;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CyberPlayer.Player.Controls;
using CyberPlayer.Player.DecoderVideoViews;
using Cybertron;
using DynamicData.Binding;
using LibMpv.Client;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Avalonia.Threading;

namespace CyberPlayer.Player.Views
{
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>, IParentPanelView
    {
        public Panel MainPanel => MainGrid;

        public MainWindow()
        {
            
            InitializeComponent();

            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
            Opened += MainWindow_Opened;
            
            AddHandler(DragDrop.DropEvent, Drop!);
            AddHandler(DragDrop.DragOverEvent, DragOver!);
            
            _cursorTimer = new Timer(_ =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (WindowState == WindowState.FullScreen && !MenuBar.IsPointerOver && !LowerGrid.IsPointerOver)
                    {
                        Cursor = _noCursor;
                        Debug.WriteLine("Cursor set to none");
                    }
                });
            });
            
#if DEBUG
            Button testButton = new()
            {
                Content = "Test",
            };
            testButton.Click += (object? sender, RoutedEventArgs e) =>
            {
                ViewModel!.MpvPlayer.TrimStartTime = 12;
                ViewModel!.MpvPlayer.TrimEndTime = 22;
            };

            Button loadButton = new()
            {
                Content = "Load",
                Focusable = false
            };
            loadButton.Click += (object? sender, RoutedEventArgs e) =>
            {
                ViewModel!.MpvPlayer.MpvContext.SetPropertyFlag("pause", true); //do not autoplay
                ViewModel!.MpvPlayer.MpvContext.CommandAsync(0, "loadfile", ViewModel!.MpvPlayer.MediaPath, "replace");
                
                //ViewModel!.Duration = ViewModel!.OpenGLMpvContext.GetPropertyDouble("duration");
                //Debug.WriteLine(ViewModel!.Duration);
                //ViewModel!.IsPlaying = true;
            };
            ControlsPanel.Children.Insert(0, loadButton);
            ControlsPanel.Children.Insert(0, testButton);
#endif
        }

        private static void DragOver(object sender, DragEventArgs e)
        {
            e.DragEffects = e.DragEffects & (DragDropEffects.Copy | DragDropEffects.Link);
            if (!e.Data.Contains(DataFormats.Text) && !e.Data.Contains(DataFormats.Files))
            {
                e.DragEffects = DragDropEffects.None;
            }
        }

        private void Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.Contains(DataFormats.Files)) return;
            
            var files = e.Data.GetFiles();

            var mediaPath = files?.FirstOrDefault()?.Path.LocalPath;
            if (mediaPath == null) return;
            
            ViewModel!.MpvPlayer.LoadFile(mediaPath);
        }

        private void MainWindow_Opened(object? sender, EventArgs e)
        {
            SetSeekControlType(SeekControlTypes.Normal);
            SetVideoDecoder(Decoder.Hardware);
            //This var isn't necessary, just makes it so that if you change the value in xaml you don't have to change here
            var foregroundBrush = VolumeSlider.Foreground;
            ViewModel!.WhenPropertyChanged(x => x.MpvPlayer.IsMuted).Subscribe(x =>
            {
                VolumeSlider.Foreground = x.Value ? Brushes.DarkSlateGray : foregroundBrush;
            });
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.T && e.KeyModifiers == KeyModifiers.Control)
            {
                InvertSeekControl();
            }
            else if (e.Key == Key.F && e.KeyModifiers == KeyModifiers.Control)
            {
                FullScreen();
            }
        }

        private IDisposable? _mpvContextBinding;
        private bool _defaultRendererSet;

        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        private void SetVideoDecoder(Decoder decoder)
        {
            if (_defaultRendererSet)
            {
                ViewModel!.MpvPlayer.IsPlaying = false;
                _mpvContextBinding?.Dispose();
                ViewModel!.MpvPlayer.MpvContext = new MpvContext();
            }
            else
            {
                _defaultRendererSet = true;
            }
            
            switch (decoder)
            {
                case Decoder.Native:
                    var nativeVideoView = new NativeVideoView { DataContext = ViewModel!.MpvPlayer };
                    _mpvContextBinding = nativeVideoView.Bind(NativeVideoView.MpvContextProperty, new Binding(nameof(MpvPlayer.MpvContext)));
                    ViewModel!.VideoContent = nativeVideoView;
                    return;
                case Decoder.Software:
                    var softwareVideoView = new SoftwareVideoView { DataContext = ViewModel!.MpvPlayer };
                    _mpvContextBinding = softwareVideoView.Bind(SoftwareVideoView.MpvContextProperty, new Binding(nameof(MpvPlayer.MpvContext)));
                    ViewModel!.VideoContent = softwareVideoView;
                    return;
                case Decoder.Hardware:
                    var hardwareVideoView = new OpenGlVideoView { DataContext = ViewModel!.MpvPlayer };
                    _mpvContextBinding = hardwareVideoView.Bind(OpenGlVideoView.MpvContextProperty, new Binding(nameof(MpvPlayer.MpvContext)));
                    ViewModel!.VideoContent = hardwareVideoView;
                    return;
            }
        }

        private readonly List<IDisposable> _currentSeekControlBindings = new(5);

        private enum SeekControlTypes
        {
            Normal,
            Trim
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        private void SetSeekControlType(SeekControlTypes type)
        {
            _currentSeekControlBindings.DisposeAndClear();
            TemplatedControl newSlider;
            switch (type)
            {
                case SeekControlTypes.Normal:
                    newSlider = new CustomSlider { Margin = new Thickness(10, 0), DataContext = ViewModel!.MpvPlayer };
                    _currentSeekControlBindings.Add(newSlider.Bind(CustomSlider.ValueProperty, new Binding(nameof(MpvPlayer.SeekValue))));
                    _currentSeekControlBindings.Add(newSlider.Bind(CustomSlider.MaximumProperty, new Binding(nameof(MpvPlayer.Duration))));
                    _currentSeekControlBindings.Add(newSlider.Bind(CustomSlider.IsDraggingProperty, new Binding(nameof(MpvPlayer.IsSeeking))));
                    break;
                case SeekControlTypes.Trim:
                    newSlider = new TimelineControl { DataContext = ViewModel!.MpvPlayer };
                    _currentSeekControlBindings.Add(newSlider.Bind(TimelineControl.SeekValueProperty, new Binding(nameof(MpvPlayer.SeekValue))));
                    _currentSeekControlBindings.Add(newSlider.Bind(TimelineControl.MaximumProperty, new Binding(nameof(MpvPlayer.Duration))));
                    _currentSeekControlBindings.Add(newSlider.Bind(TimelineControl.IsSeekDraggingProperty, new Binding(nameof(MpvPlayer.IsSeeking))));
                    _currentSeekControlBindings.Add(newSlider.Bind(TimelineControl.LowerValueProperty, new Binding(nameof(MpvPlayer.TrimStartTime))));
                    _currentSeekControlBindings.Add(newSlider.Bind(TimelineControl.UpperValueProperty, new Binding(nameof(MpvPlayer.TrimEndTime))));
                    ((TimelineControl)newSlider).SnapThreshold = 5; //TODO Probably bind this and change depending on duration (or make setting)
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            newSlider.Focusable = false;
            
            ViewModel!.SeekContent = newSlider;
        }

        private void InvertSeekControl()
        {
            SetSeekControlType(ViewModel!.SeekContent is TimelineControl ? SeekControlTypes.Normal : SeekControlTypes.Trim);
        }
        
        private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
#if !DEBUG
            if (string.IsNullOrWhiteSpace(ViewModel!.MpvPlayer.MediaPath)) return;
            
            Dispatcher.UIThread.Post(() =>
            {
                ViewModel!.MpvPlayer.LoadFile();
            });
#endif
        }
        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            ViewModel!.MpvPlayer.UpdateSliderTaskCTS.Cancel();
            ViewModel!.MpvPlayer.MpvContext.Dispose();
            ViewModel!.Settings.Export(BuildConfig.SettingsPath);
        }

        private void VideoPanel_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                ViewModel!.MpvPlayer.PlayPause();
            }
        }

        private IStorageFolder? _lastFolderLocation; //TODO maybe setting is useful for linux
        
        private async void OpenFileMenuItem_OnClick(object? sender, RoutedEventArgs e)
        {
            var result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                { AllowMultiple = false, Title = "Pick a video file", SuggestedStartLocation = _lastFolderLocation});
            
            var mediaPath = result.SingleOrDefault()?.Path.LocalPath;
            if (mediaPath == null) return;

            _lastFolderLocation = await result.Single().GetParentAsync();
            
            ViewModel!.MpvPlayer.LoadFile(mediaPath);
        }

        private void VideoPanel_OnDoubleTapped(object? sender, TappedEventArgs e)
        {
            FullScreen();
        }

        private readonly Cursor _noCursor = new(StandardCursorType.None);
        
        private void FullScreen()
        {
            if (WindowState == WindowState.FullScreen)
            {
                WindowState = WindowState.Normal;
                
                MenuBar.ZIndex = 0;
                MenuBar.Classes.Remove("FadeFullscreen");
                LowerGrid.Classes.Remove("FadeFullscreen");
                
                Grid.SetRow(VideoPanel, 1);
                Grid.SetRowSpan(VideoPanel, 1);
                
                PointerPressed -= MainWindow_PointerPressed;
                PointerMoved -= MainWindow_PointerMoved;
                Cursor = Cursor.Default;
            }
            else
            {
                MenuBar.ZIndex = 1;
                MenuBar.Classes.Add("FadeFullscreen");
                LowerGrid.Classes.Add("FadeFullscreen");
                
                Grid.SetRow(VideoPanel, 0);
                Grid.SetRowSpan(VideoPanel, MainGrid.RowDefinitions.Count);

                PointerPressed += MainWindow_PointerPressed;
                PointerMoved += MainWindow_PointerMoved;
                
                WindowState = WindowState.FullScreen;
            }
        }

        private void MainWindow_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (MenuBar.IsPointerOver || LowerGrid.IsPointerOver) return;
            
            _ignorePointerPress = true;
        }

        private bool _ignorePointerPress;
        private readonly Timer _cursorTimer;
        
        private void MainWindow_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (MenuBar.IsPointerOver || LowerGrid.IsPointerOver) return;
            
            if (_ignorePointerPress)
            {
                _ignorePointerPress = false;
                return;
            }
            
            if (Cursor != Cursor.Default)
            {
                Cursor = Cursor.Default;
                Debug.WriteLine("Cursor set to default");
            }

            _cursorTimer.Change(1000, Timeout.Infinite);
        }
    }
}