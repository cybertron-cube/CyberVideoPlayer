using System;
using DynamicData.Binding;
using ReactiveUI.Fody.Helpers;

namespace CyberPlayer.Player.ViewModels;

public class ProgressViewModel : ViewModelBase, IDialogContent
{
    public IObservable<bool> CloseDialog { get; }

    [Reactive]
    public bool Close { get; set; }

    [Reactive]
    public string? LabelText { get; set; }

    [Reactive]
    public double ProgressValue { get; set; }

    public ProgressViewModel()
    {
        CloseDialog = this.WhenValueChanged(x => x.Close);
    }
}