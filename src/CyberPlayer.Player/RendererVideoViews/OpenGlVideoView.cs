﻿using System;
using Avalonia;
using Avalonia.Data;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using LibMpv.Context;

namespace CyberPlayer.Player.RendererVideoViews;

public class OpenGlVideoView : OpenGlControlBase
{
    private delegate IntPtr GetProcAddress(string proc);

    private GetProcAddress? _getProcAddress;
    private MpvContext? _mpvContext;

    public static readonly DirectProperty<OpenGlVideoView, MpvContext?> MpvContextProperty =
           AvaloniaProperty.RegisterDirect<OpenGlVideoView, MpvContext?>(
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
            if (_getProcAddress != null)
                _mpvContext?.StartOpenGlRendering(name => _getProcAddress(name), UpdateVideoView);
        }
    }
    protected override void OnOpenGlRender(GlInterface gl, int fbo)
    {
        if (_mpvContext != null && _mpvContext.IsCustomRendering())
        {
            var size = GetPixelSize();
            _mpvContext.OpenGlRender(size.Width, size.Height, fbo, 1);
        }
    }

    protected override void OnOpenGlInit(GlInterface gl)
    {
        if (_getProcAddress != null) return;
        _getProcAddress = gl.GetProcAddress;
        _mpvContext?.StopRendering();
        _mpvContext?.StartOpenGlRendering(name => _getProcAddress(name), UpdateVideoView);
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        _mpvContext?.StopRendering();
        _getProcAddress = null;
    }

    private PixelSize GetPixelSize()
    {
        var scaling = VisualRoot!.RenderScaling;
        return new PixelSize(Math.Max(1, (int)(Bounds.Width * scaling)), Math.Max(1, (int)(Bounds.Height * scaling)));
    }

    private void UpdateVideoView()
    {
        Dispatcher.UIThread.Post(RequestNextFrameRendering, DispatcherPriority.Render);
    }
}
