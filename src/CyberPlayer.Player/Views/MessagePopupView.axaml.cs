using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CyberPlayer.Player.Views;

public partial class MessagePopupView : UserControl
{
    public MessagePopupView()
    {
        InitializeComponent();
        ButtonPanel = this.FindControl<StackPanel>("ButtonPanel");
        MarkdownBorder = this.FindControl<Border>("MarkdownBorder");
        Label = this.FindControl<Label>("Label");
        MainGrid = this.FindControl<Grid>("MainGrid");
        if (ButtonPanel == null) throw new NullReferenceException();
        if (Design.IsDesignMode)
        {
            ButtonPanel.Children.Add(new Button
            {
                Content = "Yes"
            });
            ButtonPanel.Children.Add(new Button
            {
                Content = "No"
            });
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}