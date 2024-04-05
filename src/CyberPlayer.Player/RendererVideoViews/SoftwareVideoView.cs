using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using LibMpv.Context;

namespace CyberPlayer.Player.RendererVideoViews;

public class SoftwareVideoView : Control
{
    private WriteableBitmap? _renderTarget;

    private MpvContext? _mpvContext;

    public static readonly DirectProperty<SoftwareVideoView, MpvContext?> MpvContextProperty =
           AvaloniaProperty.RegisterDirect<SoftwareVideoView, MpvContext?>(
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
            _mpvContext?.StartSoftwareRendering(UpdateVideoView);
        }
    }

    public SoftwareVideoView()
    {
        ClipToBounds = true;
    }

    public override void Render(DrawingContext context)
    {
        if (VisualRoot == null || _mpvContext == null)
            return;

        var bitmapSize = GetPixelSize();

        if (_renderTarget == null || _renderTarget.PixelSize.Width != bitmapSize.Width || _renderTarget.PixelSize.Height != bitmapSize.Height)
            _renderTarget = new WriteableBitmap(bitmapSize, new Vector(96.0, 96.0), PixelFormat.Bgra8888, AlphaFormat.Premul);

        using (var lockedBitmap = _renderTarget.Lock())
        {
            _mpvContext.SoftwareRender(lockedBitmap.Size.Width, lockedBitmap.Size.Height, lockedBitmap.Address, "bgra");
        }
        context.DrawImage(_renderTarget, new Rect(0, 0, _renderTarget.PixelSize.Width, _renderTarget.PixelSize.Height));
    }

    private PixelSize GetPixelSize()
    {
        return new PixelSize((int)Bounds.Width, (int)Bounds.Height);
    }

    private void UpdateVideoView()
    {
        Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
    }
}
