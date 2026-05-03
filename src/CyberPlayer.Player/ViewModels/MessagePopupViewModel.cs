using System;
using Avalonia.Controls;
using DynamicData.Binding;
using ReactiveUI.SourceGenerators;

namespace CyberPlayer.Player.ViewModels;

public partial class MessagePopupViewModel : ViewModelBase, IDialogContent
{
    public IObservable<bool> CloseDialog { get; }
    
    [Reactive]
    public partial bool Close { get; set; }

    [Reactive]
    public partial string? Message { get; set; }

    [Reactive]
    public partial string? Title { get; set; }

    public MessagePopupViewModel()
    {
        CloseDialog = this.WhenValueChanged(x => x.Close);

        if (Design.IsDesignMode)
        {
            Title = "Yeet SKEET!";
            Message = """
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