using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Avalonia.Interactivity;
using CyberPlayer.Player.ViewModels;
using System.Diagnostics;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using AvaloniaMessageBox;
using CyberPlayer.Player.Controls;
using CyberPlayer.Player.DecoderVideoViews;
using Cybertron;
using DynamicData.Binding;
using LibMpv.Client;
using ReactiveUI;

namespace CyberPlayer.Player.Views
{
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        public MainWindow()
        {
            
            InitializeComponent();

            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
            Opened += MainWindow_Opened;
            
            AddHandler(DragDrop.DropEvent, Drop!);
            AddHandler(DragDrop.DragOverEvent, DragOver!);

            this.WhenActivated(d =>
            {
                d(ViewModel!.ShowMessageBox.RegisterHandler(DoShowMessageBoxAsync));
            });
            
            //this.Events
#if DEBUG
            /*Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(500);
                    //Debug.WriteLine(SeekSlider.IsPointerOver);
                    //Debug.WriteLine(testimage.IsPointerOver);
                    Dispatcher.UIThread.Post(() =>
                    {
                        Debug.WriteLine(ViewModel!.IsSeeking);
                    });
                }
            });*/

            Button testButton = new()
            {
                Content = "Test",
            };
            testButton.Click += (object? sender, RoutedEventArgs e) =>
            {
                //Debug.WriteLine(SeekSlider.Height);
                //Debug.WriteLine(SeekSlider.IsDragging);
                //this.WindowState = this.WindowState != WindowState.FullScreen ? WindowState.FullScreen : WindowState.Normal;

                /*var test = new Settings();
                var text = XmlConvert.SerializeObject(test);
                var newObject = XmlConvert.DeserializeObject<Settings>(text);
                Debug.WriteLine(text);*/
                
                //XmlConvert.Export(ViewModel!.Settings, App.SettingsPath);

                var ffmpegPath = GenStatic.GetFullPathFromRelative(Path.Combine("ffmpeg", "ffmpeg"));
                Debug.WriteLine(ffmpegPath);
                Debug.WriteLine(GenStatic.GetOSRespectiveExecutablePath(ffmpegPath));

                ViewModel!.TrimStartTime = 12;
                ViewModel!.TrimEndTime = 22;

                //SetVideoDecoder(Renderer.Software);
                //Key.rig
            };

            Button loadButton = new()
            {
                Content = "Load",
                Focusable = false
            };
            loadButton.Click += (object? sender, RoutedEventArgs e) =>
            {
                ViewModel!.MpvContext.SetPropertyFlag("pause", true); //do not autoplay
                ViewModel!.MpvContext.CommandAsync(0, "loadfile", ViewModel!.MediaPath, "replace");
                
                //ViewModel!.Duration = ViewModel!.OpenGLMpvContext.GetPropertyDouble("duration");
                //Debug.WriteLine(ViewModel!.Duration);
                //ViewModel!.IsPlaying = true;
            };
            ControlsPanel.Children.Insert(0, loadButton);
            ControlsPanel.Children.Insert(0, testButton);
#endif
        }

        private async Task DoShowMessageBoxAsync(InteractionContext<MessageBoxParams, MessageBoxResult> interaction)
        {
            var msgBox = MessageBox.GetMessageBox(interaction.Input);
            var result = await msgBox.ShowDialog(this);
            interaction.SetOutput(result);
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
            if (files == null) return;
                
            if (files.Count() == 1)
            {
                var mediaPath = files.Single().Path.LocalPath;
                ViewModel!.LoadFile(mediaPath);
            }
        }

        private void MainWindow_Opened(object? sender, EventArgs e)
        {
            SetSeekControlType(SeekControlTypes.Normal);
            SetVideoDecoder(Decoder.Hardware);
            //This var isn't necessary, just makes it so that if you change the value in xaml you don't have to change here
            var foregroundBrush = VolumeSlider.Foreground;
            ViewModel!.WhenPropertyChanged(x => x.IsMuted).Subscribe(x =>
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
        }

        private IDisposable? _mpvContextBinding;
        private bool _defaultRendererSet;
        
        private void SetVideoDecoder(Decoder decoder)
        {
            if (_defaultRendererSet)
            {
                ViewModel!.IsPlaying = false;
                _mpvContextBinding?.Dispose();
                
                //ViewModel!.MpvContext.Dispose();
                //Thread.Sleep(1000);
                //ViewModel!.MpvContext = new();
                //Thread.Sleep(1000);

                
                
                //ViewModel!.VideoContent = null;
                
                //Native will call dispose later (don't want to cause object disposed exception)
                //if (ViewModel!.VideoContent is not NativeVideoView)
                //{
                //    ViewModel!.MpvContext.Dispose();
                //}
                ViewModel!.MpvContext = new MpvContext();
                
                //<local:OpenGlVideoView Name="VideoView" MpvContext="{Binding MpvContext}"/>
            }
            else
            {
                _defaultRendererSet = true;
            }
            
            switch (decoder)
            {
                case Decoder.Native:
                    var nativeVideoView = new NativeVideoView();
                    _mpvContextBinding = nativeVideoView.Bind(NativeVideoView.MpvContextProperty, new Binding(nameof(ViewModel.MpvContext)));
                    ViewModel!.VideoContent = nativeVideoView;
                    return;
                case Decoder.Software:
                    var softwareVideoView = new SoftwareVideoView();
                    _mpvContextBinding = softwareVideoView.Bind(SoftwareVideoView.MpvContextProperty, new Binding(nameof(ViewModel.MpvContext)));
                    ViewModel!.VideoContent = softwareVideoView;
                    return;
                case Decoder.Hardware:
                    var hardwareVideoView = new OpenGlVideoView();
                    _mpvContextBinding = hardwareVideoView.Bind(OpenGlVideoView.MpvContextProperty, new Binding(nameof(ViewModel.MpvContext)));
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
        
        private void SetSeekControlType(SeekControlTypes type)
        {
            _currentSeekControlBindings.DisposeAndClear();
            TemplatedControl newSlider;
            switch (type)
            {
                case SeekControlTypes.Normal:
                    newSlider = new CustomSlider();
                    newSlider.Margin = new Thickness(10, 0);
                    //newSlider.Classes.Add("Fluent");
                    _currentSeekControlBindings.Add(newSlider.Bind(CustomSlider.ValueProperty, new Binding(nameof(ViewModel.SeekValue))));
                    _currentSeekControlBindings.Add(newSlider.Bind(CustomSlider.MaximumProperty, new Binding(nameof(ViewModel.Duration))));
                    _currentSeekControlBindings.Add(newSlider.Bind(CustomSlider.IsDraggingProperty, new Binding(nameof(ViewModel.IsSeeking))));
                    break;
                case SeekControlTypes.Trim:
                    newSlider = new TimelineControl();
                    _currentSeekControlBindings.Add(newSlider.Bind(TimelineControl.SeekValueProperty, new Binding(nameof(ViewModel.SeekValue))));
                    _currentSeekControlBindings.Add(newSlider.Bind(TimelineControl.MaximumProperty, new Binding(nameof(ViewModel.Duration))));
                    _currentSeekControlBindings.Add(newSlider.Bind(TimelineControl.IsSeekDraggingProperty, new Binding(nameof(ViewModel.IsSeeking))));
                    _currentSeekControlBindings.Add(newSlider.Bind(TimelineControl.LowerValueProperty, new Binding(nameof(ViewModel.TrimStartTime))));
                    _currentSeekControlBindings.Add(newSlider.Bind(TimelineControl.UpperValueProperty, new Binding(nameof(ViewModel.TrimEndTime))));
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
        
        private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
#if !DEBUG
            if (string.IsNullOrWhiteSpace(ViewModel!.MediaPath)) return;
            
            await Task.Delay(10);
            ViewModel!.MpvContext.SetPropertyFlag("pause", false); //autoplay
            ViewModel!.MpvContext.Command("loadfile", ViewModel!.MediaPath, "replace");
            ViewModel!.IsPlaying = true;
#endif
        }
        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            ViewModel!.UpdateSliderTaskCTS.Cancel();
            ViewModel!.MpvContext.Dispose();
            XmlConvert.Export(ViewModel!.Settings, BuildConfig.SettingsPath);
        }

        private void VideoPanel_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                ViewModel!.PlayPause();
            }
        }

        private async void MenuItem_OnClick(object? sender, RoutedEventArgs e)
        {
            var result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                { AllowMultiple = false, Title = "Pick a video file" });
            
            var mediaPath = result.SingleOrDefault()?.Path.LocalPath;
            if (mediaPath == null) return;
            
            ViewModel!.LoadFile(mediaPath);
        }
    }
}