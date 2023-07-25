using System;
using Avalonia.Controls;
using DynamicData.Binding;
using ReactiveUI;

namespace CyberPlayer.Player.ViewModels;

public class MessagePopupViewModel : ViewModelBase, IDialogContent
{
    public IObservable<bool> CloseDialog { get; }
    
    private bool _close;
    
    public bool Close
    {
        get => _close;
        set => this.RaiseAndSetIfChanged(ref _close, value);
    }

    private string _message = string.Empty;

    public string Message
    {
        get => _message;
        set => this.RaiseAndSetIfChanged(ref _message, value);
    }

    private string _title = string.Empty;

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public MessagePopupViewModel()
    {
        CloseDialog = this.WhenValueChanged(x => x.Close);

        if (Design.IsDesignMode)
        {
            _title = "Yeet SKEET!";
            _message = """
            # Release 1.0.5
            ## Features
            + Add support for OSX
            + More log information
            + Additional setting to detach ffmpeg process
               + When `DetachFFmpegProcess` is set to `true` ffmpeg will run and show up in its own terminal like normal
               + Note that this will mean progress does not update in the UI while ffmpeg is running
            ## BugFixes
            + Fixed choosing no on overwrite prompt
            + Fixed ffmpeg speed degrading to basically a stand still on certain video codecs

            ## What's Changed
            * Release 1.0.5 by @Blitznir in https://github.com/Blitznir/FFmpegAvalonia/pull/2


            **Full Changelog**: https://github.com/Blitznir/FFmpegAvalonia/compare/1.0.4...1.0.5
            """;
        }
    }
}