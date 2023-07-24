using System;
using CyberPlayer.Player.Views;
using DynamicData.Binding;
using ReactiveUI;

namespace CyberPlayer.Player.ViewModels;

public class ProgressViewModel : ViewModelBase, IDialogContent
{
    public IObservable<bool> CloseDialog { get; }

    private bool _close;
    
    public bool Close
    {
        get => _close;
        set => this.RaiseAndSetIfChanged(ref _close, value);
    }

    private string _labelText = "LABEL TEXT";

    public string LabelText
    {
        get => _labelText;
        set => this.RaiseAndSetIfChanged(ref _labelText, value);
    }

    private double _progressValue;

    public double ProgressValue
    {
        get => _progressValue;
        set => this.RaiseAndSetIfChanged(ref _progressValue, value);
    }

    public ProgressViewModel()
    {
        CloseDialog = this.WhenValueChanged(x => x.Close);
    }
}