using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Platform;
using LibMpv.Context;

namespace CyberPlayer.Player.RendererVideoViews;

public class NativeVideoView : NativeControlHost
{
    private IPlatformHandle? _platformHandle;

    private MpvContext? _mpvContext;

    public static readonly DirectProperty<NativeVideoView, MpvContext?> MpvContextProperty =
           AvaloniaProperty.RegisterDirect<NativeVideoView, MpvContext?>(
               nameof(MpvContext),
               o => o.MpvContext,
               (o, v) => o.MpvContext = v,
               defaultBindingMode: BindingMode.TwoWay);


    public MpvContext? MpvContext
    {
        get => _mpvContext;
        set
        {
            if (ReferenceEquals(value, _mpvContext)) return;
            _mpvContext?.StopRendering();
            _mpvContext = value;
            if (_platformHandle != null)
                _mpvContext?.StartNativeRendering(_platformHandle.Handle);
        }
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        _platformHandle = base.CreateNativeControlCore(parent);
        _mpvContext?.StartNativeRendering(_platformHandle.Handle);
        return _platformHandle;
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        _mpvContext?.StopRendering();
        base.DestroyNativeControlCore(control);
        _platformHandle = null;
    }
}
