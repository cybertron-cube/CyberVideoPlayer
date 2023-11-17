using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media.Transformation;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using CyberPlayer.Player.ViewModels;
using DynamicData.Binding;

namespace CyberPlayer.Player.Views;

public partial class VideoInfoWindow : ReactiveWindow<VideoInfoViewModel>
{
    private readonly TransformOperations _openTransform = TransformOperations.Parse("rotate(-180deg)");
    private bool _listOpen;
    private IDisposable? _textBinding;
    
    public VideoInfoWindow()
    {
        InitializeComponent();
        Opened += VideoInfoWindow_Opened;
    }

    private void VideoInfoWindow_Opened(object? sender, EventArgs e)
    {
        ViewModel!.WhenPropertyChanged(x => x.JsonTreeView)
            .Subscribe(_ => ChangeView());

        ViewModel!.WhenPropertyChanged(x => x.CurrentFormat)
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
                    });
                }
                else
                {
                    ViewModel!.JsonTreeView = false;
                    JsonTreeCheck.IsVisible = false;
                }
            });

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
            ArrowShape.RenderTransform = _openTransform;
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
}