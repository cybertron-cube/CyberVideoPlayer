using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CyberPlayer.Player.Controls;
using CyberPlayer.Player.ViewModels;
using CyberPlayer.Player.Views;
using HanumanInstitute.MvvmDialogs.Avalonia;
using ReactiveUI;
using Serilog;
using Serilog.Core;
using Splat;
using ILogger = Serilog.ILogger;

namespace CyberPlayer.Player.Services;

public static class DialogService
{
    private static readonly Dictionary<VideoInfoType, Window?> VideoInfoActive = new()
    {
        { VideoInfoType.MediaInfo , null },
        { VideoInfoType.FFprobe , null },
        { VideoInfoType.Mpv , null }
    };
    
    public static async Task<IReadOnlyList<IStorageFile>> OpenFileDialog(this ViewModelBase viewModel, FilePickerOpenOptions options)
    {
        var view = (Window)GetView(viewModel);
        var storageProvider = view.StorageProvider;
        
        return await storageProvider.OpenFilePickerAsync(options);
    }

    public static async Task<IStorageFile?> SaveFileDialog(this ViewModelBase viewModel, FilePickerSaveOptions options)
    {
        var view = (Window)GetView(viewModel);
        var storageProvider = view.StorageProvider;

        return await storageProvider.SaveFilePickerAsync(options);
    }

    public static void ShowVideoInfo(this MainWindowViewModel viewModel, VideoInfoType videoInfoType)
    {
        if (VideoInfoActive[videoInfoType] != null) return;
        
        var videoInfoView = new VideoInfoWindow();
        
        var videoInfoViewModel = videoInfoType switch
        {
            VideoInfoType.MediaInfo => (VideoInfoViewModel)Locator.Current.GetService<MediaInfoViewModel>()!,
            VideoInfoType.FFprobe => Locator.Current.GetService<FFprobeInfoViewModel>()!,
            VideoInfoType.Mpv => Locator.Current.GetService<MpvInfoViewModel>()!,
            _ => throw new ArgumentOutOfRangeException(nameof(videoInfoType), videoInfoType, null)
        };
        
        videoInfoView.DataContext = videoInfoViewModel;
        videoInfoView.Closed += (_, _) => VideoInfoActive[videoInfoType] = null;
        VideoInfoActive[videoInfoType] = videoInfoView;
        videoInfoView.Show();
    }
    
    [MessageTemplateFormatMethod("messageTemplate")]
    public static void ShowErrorMessage(this ViewModelBase viewModel, ILogger log, Exception? exception, string messageTemplate, params object?[]? propertyValues)
    {
        log.Error(exception, messageTemplate, propertyValues);
        var message = propertyValues == null ? messageTemplate : string.Format(messageTemplate, propertyValues);
        ShowMessagePopup(viewModel, "An error occurred", message, new PopupParams(CloseOnClickAway: true));
    }
    
    [MessageTemplateFormatMethod("messageTemplate")]
    public static void ShowErrorMessage(this ViewModelBase viewModel, ILogger log, string messageTemplate, params object?[]? propertyValues)
    {
        ShowErrorMessage(viewModel, log, null, messageTemplate, propertyValues);
    }
    
    public static async Task<MessagePopupResult> ShowMessagePopupAsync(this ViewModelBase viewModel, MessagePopupButtons buttons, string title, string message, PopupParams popupParams)
    {
        var content = Locator.Current.GetService<MessagePopupView>()!;
        var dataContext = Locator.Current.GetService<MessagePopupViewModel>()!;
        if (string.IsNullOrWhiteSpace(message))
        {
            content.MainGrid.Children.Remove(content.MarkdownBorder);
            Grid.SetRow(content.Label, 1);
            content.Label.Margin = new Thickness(0, -2, 0, 5);
        }
        else
        {
            dataContext.Message = message;
        }
        dataContext.Title = title;
        content.DataContext = dataContext;

        var result = MessagePopupResult.Cancel;

        switch (buttons)
        {
            case MessagePopupButtons.YesNo:
                content.ButtonPanel.Children.Add(new Button
                {
                    Content = "Yes"
                });
                content.ButtonPanel.Children.Add(new Button
                {
                    Content = "No"
                });
                break;
            case MessagePopupButtons.Ok:
                content.ButtonPanel.Children.Add(new Button
                {
                    Content = "Ok"
                });
                break;
            case MessagePopupButtons.OkCancel:
                content.ButtonPanel.Children.Add(new Button
                {
                    Content = "Ok"
                });
                content.ButtonPanel.Children.Add(new Button
                {
                    Content = "Cancel"
                });
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(buttons), buttons, null);
        }

        var popup = GetPopup(viewModel, content, popupParams);
        
        foreach (Button button in content.ButtonPanel.Children)
        {
            button.Click += (_, _) =>
            {
                result = (MessagePopupResult)Enum.Parse(typeof(MessagePopupResult), (string)button.Content!);
                dataContext.Close = true;
            };
        }
        
        await popup.popup.OpenAsync();
        await dataContext.CloseDialog.TakeUntil(x => x);
        
        return result;
    }
    
    public static void ShowMessagePopup(this ViewModelBase viewModel, string title, string message, PopupParams popupParams)
    {
        var content = Locator.Current.GetService<MessagePopupView>()!;
        var dataContext = Locator.Current.GetService<MessagePopupViewModel>()!;
        if (string.IsNullOrWhiteSpace(message))
        {
            content.MainGrid.Children.Remove(content.MarkdownBorder);
            Grid.SetRow(content.Label, 1);
            content.Label.Margin = new Thickness(0, -2, 0, 5);
        }
        else
        {
            dataContext.Message = message;
        }
        dataContext.Title = title;
        content.DataContext = dataContext;

        var button = new Button
        {
            Content = "Ok"
        };
        button.Click += (_, _) => dataContext.Close = true;
        content.ButtonPanel.Children.Add(button);

        var popup = GetPopup(viewModel, content, popupParams);

        var disposables = new CompositeDisposable(2);
        
        disposables.Add(popup.popup.WhenAnyValue(x => x.IsOpen).Skip(1)
            .Subscribe(isOpen =>
        {
            if (isOpen) return;
            
            disposables.Add(popup.popup.WhenAnyValue(x => x.IsAnimating).Skip(1)
                .Subscribe(animating =>
            {
                if (animating) return;
                
                popup.attachedView.MainPanel.Children.Remove(popup.popup);
                disposables.Dispose();
            }));
        }));
        
        popup.popup.Open();
    }
    
    public static ProgressPopupHandler GetProgressPopup(this ViewModelBase viewModel, PopupParams popupParams)
    {
        var content = Locator.Current.GetService<ProgressView>()!;
        content.DataContext = Locator.Current.GetService<ProgressViewModel>();
        var popup = GetPopup(viewModel, content, popupParams);
        return new ProgressPopupHandler(popup.popup, popup.attachedView);
    }
    
    public static PopupHandler ShowPopup<TContent>(this ViewModelBase viewModel, PopupParams popupParams)
        where TContent : Control, new()
    {
        var popup = GetPopup<TContent>(viewModel, popupParams);
        popup.popup.Open();
        return new PopupHandler(popup.popup, popup.attachedView);
    }
    
    public static PopupHandler ShowPopup(this ViewModelBase viewModel, object content, PopupParams popupParams)
    {
        var popup = GetPopup(viewModel, content, popupParams);
        popup.popup.Open();
        return new PopupHandler(popup.popup, popup.attachedView);
    }
    
    public static TReturnType ShowPopup<TContent, TReturnType>(this ViewModelBase viewModel, Func<TReturnType> work, PopupParams popupParams)
        where TContent : Control, new()
    {
        var popup = GetPopup<TContent>(viewModel, popupParams);
        popup.popup.Open();
        var result = work.Invoke();
        popup.popup.Close();
        popup.attachedView.MainPanel.Children.Remove(popup.popup);
        return result;
    }
    
    public static async Task<TReturnType> ShowPopupAsync<TContent, TReturnType>(this ViewModelBase viewModel, Func<TReturnType> work, PopupParams popupParams)
        where TContent : Control, new()
    {
        var popup = GetPopup<TContent>(viewModel, popupParams);
        await popup.popup.OpenAsync();
        var result = await Task.Run(work.Invoke);
        await popup.popup.CloseAsync();
        popup.attachedView.MainPanel.Children.Remove(popup.popup);
        return result;
    }

    private static (IParentPanelView attachedView, ContentPopup popup) GetPopup<TContent>(ViewModelBase viewModel, PopupParams popupParams)
        where TContent : Control, new()
    {
        var content = (Control)Activator.CreateInstance(typeof(TContent))!;
        return GetPopup(viewModel, content, popupParams);
    }
    
    private static (IParentPanelView attachedView, ContentPopup popup) GetPopup(ViewModelBase viewModel, object content, PopupParams popupParams)
    {
        var view = (IParentPanelView)GetView(viewModel);
        var contentPopup = MakePopup(content, popupParams);
        
        //If content has a static viewmodel association, datacontext needs to be set before adding
        //This is because if datacontext is null when being added to the control, it will be auto set to the viewmodel of uppermost parent (likely mainwindowviewmodel in this case)
        
        //((ProgressView)content).DataContext = new ProgressViewModel();
        view.MainPanel.Children.Add(contentPopup);
        if (view.MainPanel is Grid grid)
        {
            Grid.SetRowSpan(contentPopup, grid.RowDefinitions.Count);
            Grid.SetColumnSpan(contentPopup, grid.ColumnDefinitions.Count);
        }
        return (view, contentPopup);
    }

    private static object GetView(ViewModelBase viewModel)
    {
        if (viewModel is VideoInfoViewModel videoInfoViewModel)
            return VideoInfoActive[videoInfoViewModel.VideoInfoType]!;
        
        var viewLocator = Locator.Current.GetService<StrongViewLocator>()!;
        var viewType = viewLocator.Locate(viewModel).ViewType;
        return Locator.Current.GetService(viewType)!;
    }
    
    private static ContentPopup MakePopup(object content, PopupParams popupParams)
    {
        var popup = new ContentPopup
        {
            Content = content,
            PopupSize = popupParams.PopupSize,
            UnderlayOpacity = popupParams.UnderlayOpacity,
            CloseOnClickAway = popupParams.CloseOnClickAway,
            AnimateOpacity = popupParams.AnimateOpacity,
        };

        if (popupParams.AnimationDuration != null)
            popup.AnimationDuration = (TimeSpan)popupParams.AnimationDuration;

        if (popupParams.Brush != null)
            popup.Background = popupParams.Brush;

        if (popupParams.Padding != null)
            popup.Padding = (Thickness)popupParams.Padding;

        return popup;
    }
}