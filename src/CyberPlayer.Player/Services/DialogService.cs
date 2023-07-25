using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using CyberPlayer.Player.Controls;
using CyberPlayer.Player.ViewModels;
using CyberPlayer.Player.Views;
using HanumanInstitute.MvvmDialogs.Avalonia;
using Splat;

namespace CyberPlayer.Player.Services;

public static class DialogService
{
    public static async Task<MessagePopupResult> ShowMessagePopup(this ViewModelBase viewModel, MessagePopupButtons buttons, string title, string message, PopupParams popupParams)
    {
        var content = Locator.Current.GetService<MessagePopupView>()!;
        var dataContext = Locator.Current.GetService<MessagePopupViewModel>()!;
        dataContext.Message = message;
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
        var viewLocator = Locator.Current.GetService<StrongViewLocator>()!;
        var viewType = viewLocator.Locate(viewModel).ViewType;
        var view = (IParentPanelView)Locator.Current.GetService(viewType)!;
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