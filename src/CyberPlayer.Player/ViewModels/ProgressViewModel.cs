using System;
using DynamicData.Binding;
using ReactiveUI.SourceGenerators;

namespace CyberPlayer.Player.ViewModels;

public partial class ProgressViewModel : ViewModelBase, IDialogContent
{
    public IObservable<bool> CloseDialog { get; }

    [Reactive]
    private bool _close;

    [Reactive]
    private string? _labelText;

    [Reactive]
    private double _progressValue;

    public ProgressViewModel()
    {
        CloseDialog = this.WhenValueChanged(x => x.Close);
    }
}