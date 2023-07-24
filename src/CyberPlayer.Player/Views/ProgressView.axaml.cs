using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CyberPlayer.Player.Views;

public partial class ProgressView : UserControl
{
    public ProgressView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}