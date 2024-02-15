using System;
using Avalonia.ReactiveUI;
using CyberPlayer.Player.Models;
using CyberPlayer.Player.ViewModels;
using DynamicData.Binding;

namespace CyberPlayer.Player.Views;

public partial class ExportWindow : ReactiveWindow<ExportWindowViewModel>
{
    public ExportWindow()
    {
        InitializeComponent();
        
        Opened += ExportWindow_Opened;
        
        AudioTrackListBox.SelectionChanged += (_, args) =>
        {
            foreach (var item in args.AddedItems)
            {
                ((TrackInfo)item).IncludeInExport = true;
            }

            foreach (var item in args.RemovedItems)
            {
                ((TrackInfo)item).IncludeInExport = false;
            }
        };

        CancelButton.Click += (_, _) => Close();
    }

    private void ExportWindow_Opened(object? sender, EventArgs e)
    {
        AudioTrackListBox.SelectAll();
        ViewModel!.WhenPropertyChanged(x => x.AudioTrackInfos).Subscribe(_ => AudioTrackListBox.SelectAll());
    }
}