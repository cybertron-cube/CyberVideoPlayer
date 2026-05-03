using System;
using DynamicData.Binding;
using ReactiveUI.SourceGenerators;

namespace CyberPlayer.Player.ViewModels;

public partial class ProgressViewModel : ViewModelBase, IDialogContent
{
    public IObservable<bool> CloseDialog { get; }

    [Reactive]
    public partial bool Close { get; set; }

    [Reactive]
    public partial string? LabelText { get; set; }

    [Reactive]
    public partial double ProgressValue { get; set; }

    public ProgressViewModel()
    {
        CloseDialog = this.WhenValueChanged(x => x.Close);
    }
}