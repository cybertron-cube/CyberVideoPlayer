using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using CyberPlayer.Player.ViewModels;
using DynamicData.Binding;

namespace CyberPlayer.Player.Views;

public partial class VideoInfoWindow : ReactiveWindow<VideoInfoViewModel>
{
    private readonly TransformOperations _openFormatBoxTransform = TransformOperations.Parse("rotate(-180deg)");
    private readonly TransformOperations _animateCheckInScale = TransformOperations.Parse("scale(3, 3)");
    private readonly TransformOperations _animateCheckOutScale = TransformOperations.Parse("scale(0.8, 0.8)");
    private readonly TransformOperations _animateCircleInScale = TransformOperations.Parse("scale(5, 5)");
    private readonly TransformOperations _animateCircleOutScale = TransformOperations.Parse("scale(0, 0)");
    
    private readonly TransformOperationsTransition _animateCheckOutRenderTransformTransition = new()
    {
        Property = RenderTransformProperty,
        Duration = TimeSpan.FromSeconds(1),
        Delay = TimeSpan.FromSeconds(0.2),
        Easing = new LinearEasing()
    };
    private readonly DoubleTransition _animateCheckOutStrokeTransition = new()
    {
        Property = Shape.StrokeDashOffsetProperty,
        Duration = TimeSpan.FromSeconds(0.2),
        Delay = TimeSpan.FromSeconds(0.2),
        Easing = new LinearEasing()
    };
    private readonly TransformOperationsTransition _animateCircleOutRenderTransformTransition = new()
    {
        Property = RenderTransformProperty,
        Duration = TimeSpan.FromSeconds(0.8),
        Easing = new ElasticEaseIn()
    };
    private readonly TransformOperationsTransition _animateCheckInRenderTransformTransition;
    private readonly DoubleTransition _animateCheckInStrokeTransition;
    private readonly TransformOperationsTransition _animateCircleInRenderTransformTransition;
    
    private bool _listOpen;
    private IDisposable? _textBinding;
    private IDisposable? _exportSubscription;
    private IDisposable? _jsonTreeViewSubscription;
    private IDisposable? _currentFormatSubscription;

    public VideoInfoWindow()
    {
        InitializeComponent();
        Opened += VideoInfoWindow_Opened;
        Closing += (_, _) =>
        {
            _textBinding?.Dispose();
            _exportSubscription?.Dispose();
            _jsonTreeViewSubscription?.Dispose();
            _currentFormatSubscription?.Dispose();
        };
        
        _animateCheckInRenderTransformTransition = (TransformOperationsTransition)CheckPath.Transitions![0];
        _animateCheckInStrokeTransition = (DoubleTransition)CheckPath.Transitions[1];
        _animateCircleInRenderTransformTransition = (TransformOperationsTransition)CirclePath.Transitions![0];
    }

    private void VideoInfoWindow_Opened(object? sender, EventArgs e)
    {
        _jsonTreeViewSubscription = ViewModel!.WhenPropertyChanged(x => x.JsonTreeView)
            .Subscribe(_ => ChangeView());

        _currentFormatSubscription = ViewModel!.WhenPropertyChanged(x => x.CurrentFormat)
            .Subscribe(x =>
            {
                //When activating JsonTreeView there must be a delay so that the rawtext will change
                //... before the conversion to nodes start
                //... otherwise an exception will occur because a conversion from non-json to nodes is attempted
                //When deactivating it's the opposite, change to a textbox before rawtext is changed
                if (x.Value?.ToLower() == "json")
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        ViewModel!.JsonTreeView = true;
                        JsonTreeCheck.IsVisible = true;
                        TextWrapCheck.IsVisible = false;
                    });
                }
                else
                {
                    ViewModel!.JsonTreeView = false;
                    JsonTreeCheck.IsVisible = false;
                    TextWrapCheck.IsVisible = true;
                }
            });

        _exportSubscription = ViewModel!.ExportFinished.Subscribe(_ => ExportFinishedAnimation());

        FormatBox.Margin = new Thickness(0, 0, 0, FormatButton.Bounds.Height + 4);
    }

    private void ChangeView()
    {
        _textBinding?.Dispose();
        if (ViewModel!.JsonTreeView)
        {
            var view = new JsonTreeView();
            var viewModel = new JsonTreeViewModel();
            viewModel.RawText = ViewModel!.RawText;
            _textBinding = ViewModel!.WhenPropertyChanged(x => x.RawText).Subscribe(_ => viewModel.RawText = ViewModel!.RawText);
            view.DataContext = viewModel;
            ContentControl.Content = view;
        }
        else
        {
            var textBox = new TextBox
            {
                Text = ViewModel!.RawText,
                IsReadOnly = true
            };
            _textBinding = textBox.Bind(TextBox.TextProperty, new Binding(nameof(VideoInfoViewModel.RawText)));
            ContentControl.Content = textBox;
        }
    }

    private void ExportFinishedAnimation()
    {
        CirclePath.RenderTransform = _animateCircleInScale;
        CheckPath.RenderTransform = _animateCheckInScale;
        CheckPath.StrokeDashOffset = 0;
            
        Dispatcher.UIThread.Post(async () =>
        {
            await Task.Delay(2000);
            
            CheckPath.Transitions![0] = _animateCheckOutRenderTransformTransition;
            CheckPath.Transitions[1] = _animateCheckOutStrokeTransition;
            CirclePath.Transitions![0] = _animateCircleOutRenderTransformTransition;
            
            CheckPath.RenderTransform = _animateCheckOutScale;
            CheckPath.StrokeDashOffset = 13;
            CirclePath.RenderTransform = _animateCircleOutScale;
            
            await Task.Delay(1000);
            
            CheckPath.Transitions![0] = _animateCheckInRenderTransformTransition;
            CheckPath.Transitions[1] = _animateCheckInStrokeTransition;
            CirclePath.Transitions![0] = _animateCircleInRenderTransformTransition;
        });
    }
    
    private void FormatButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_listOpen)
        {
            FormatBox.Height = 0;
            ArrowShape.RenderTransform = null;
            _listOpen = false;
        }
        else
        {
            FormatBox.Height = Bounds.Height / 3;
            ArrowShape.RenderTransform = _openFormatBoxTransform;
            _listOpen = true;
        }
    }
    
    private void FormatBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (FormatBox.SelectedItem is string format) ViewModel!.CurrentFormat = format;

        FormatBox.SelectedItem = null;
            
        FormatBox.Height = 0;
        ArrowShape.RenderTransform = null;
        _listOpen = false;
    }

    private void TextWrapCheck_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        var textBox = (TextBox)ContentControl.Content!;
        switch (TextWrapCheck.IsChecked)
        {
            case true:
                textBox.TextWrapping = TextWrapping.Wrap;
                //TODO surely there is a better way to force wrap text update?
                textBox.Width = Bounds.Width - 1;
                Dispatcher.UIThread.Post(() => textBox.Width = Bounds.Width);
                break;
            case false:
                textBox.TextWrapping = TextWrapping.NoWrap;
                break;
            default:
                return;
        }
    }
}