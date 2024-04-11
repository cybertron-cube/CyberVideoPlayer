using System.Runtime.InteropServices;
using LibMpv.Client;
using static LibMpv.Client.libmpv;

namespace LibMpv.Context;

public delegate nint GetProcAddress(string name);
public delegate void UpdateCallback();

public sealed unsafe partial class MpvContext
{
    public bool IsCustomRendering() => _renderContext != null;

    private mpv_render_context* _renderContext;
    private mpv_opengl_init_params_get_proc_address? _getProcAddress;
    private mpv_render_context_set_update_callback_callback? _updateCallback;
    
    public void StartOpenGlRendering(GetProcAddress getProcAddress, UpdateCallback updateCallback)
    {
        if (_disposed) return;
        StopRendering();

        _getProcAddress = (_, name) => (void*)getProcAddress(name);
        _updateCallback = _ => updateCallback();

        using var marshalHelper = new MarshalHelper();

        var parameters = new mpv_render_param[]
        {
            new()
            {
                type = mpv_render_param_type.MPV_RENDER_PARAM_API_TYPE,
                data = (void*)marshalHelper.StringToHGlobalAnsi(MPV_RENDER_API_TYPE_OPENGL)
            },
            new()
            {
                type = mpv_render_param_type.MPV_RENDER_PARAM_OPENGL_INIT_PARAMS,
                data = (void*)marshalHelper.AllocHGlobal(new mpv_opengl_init_params
                {
                    get_proc_address = _getProcAddress,
                    get_proc_address_ctx = null
                })
            },
            new()
            {
                type = mpv_render_param_type.MPV_RENDER_PARAM_ADVANCED_CONTROL,
                data = (void*)marshalHelper.AllocHGlobalValue(0)
            },
            new()
            {
                type = mpv_render_param_type.MPV_RENDER_PARAM_INVALID,
                data = null
            }
        };

        int errorCode;

        mpv_render_context* contextPtr = null;
        fixed (mpv_render_param* parametersPtr = parameters)
        {
            errorCode = mpv_render_context_create(&contextPtr, _ctx, parametersPtr);
        }

        if (errorCode >= 0)
        {
            _renderContext = contextPtr;
            mpv_render_context_set_update_callback(_renderContext, _updateCallback, null);
        }

        CheckCode(errorCode);
    }

    public void OpenGlRender(int width, int height, int fb = 0, int flipY = 0)
    {
        if (_disposed) return;
        if (_renderContext == null) return;

        using var marshalHelper = new MarshalHelper();

        var fbo = new mpv_opengl_fbo
        {
            w = width,
            h = height,
            fbo = fb
        };
        
        var handle = GCHandle.Alloc(fbo, GCHandleType.Pinned);

        var parameters = new mpv_render_param[]
        {
            new()
            {
                type = mpv_render_param_type.MPV_RENDER_PARAM_OPENGL_FBO,
                data = &fbo
            },
            new()
            {
                type = mpv_render_param_type.MPV_RENDER_PARAM_FLIP_Y,
                data = (void *) marshalHelper.AllocHGlobalValue(flipY)
            },
            new() 
            { 
                type = mpv_render_param_type.MPV_RENDER_PARAM_INVALID
            },
        };

        int errorCode;
        fixed (mpv_render_param* parametersPtr = parameters)
        {
            errorCode = mpv_render_context_render(_renderContext, parametersPtr);
        }
        handle.Free();

        CheckCode(errorCode);
       
    }

    public void StartSoftwareRendering(UpdateCallback updateCallback)
    {
        if (_disposed) return;
        StopRendering();

        _updateCallback = _ => updateCallback();

        using var marshalHelper = new MarshalHelper();

        var parameters = new mpv_render_param[]
        {
            new()
            {
                type = mpv_render_param_type.MPV_RENDER_PARAM_API_TYPE,
                data = (void*)marshalHelper.StringToHGlobalAnsi(MPV_RENDER_API_TYPE_SW)
            },
            new ()
            {
                type = mpv_render_param_type.MPV_RENDER_PARAM_ADVANCED_CONTROL,
                data = (void *) marshalHelper.AllocHGlobalValue(0)
            },
            new ()
            {
                type = mpv_render_param_type.MPV_RENDER_PARAM_INVALID,
                data =null
            }
        };

        int errorCode;

        mpv_render_context* contextPtr = null;
        fixed (mpv_render_param* parametersPtr = parameters)
        {
            errorCode = mpv_render_context_create(&contextPtr, _ctx, parametersPtr);
        }

        if (errorCode >= 0)
        {
            _renderContext = contextPtr;
            mpv_render_context_set_update_callback(_renderContext, _updateCallback, null);
        }

        CheckCode(errorCode);
    }

    public void SoftwareRender(int width, int height, nint surfaceAddress, string format)
    {
        if (_disposed) return;
        if (_renderContext == null) return;

        using var marshalHelper = new MarshalHelper();

        var size = new[] { width, height };
        var stride = new[] { (uint)width * 4 };

        fixed(int* sizePtr = size)
        {
            fixed(uint * stridePtr = stride) 
            {
                var parameters = new mpv_render_param[]
                {
                    new()
                    {
                        type = mpv_render_param_type.MPV_RENDER_PARAM_SW_SIZE,
                        data = sizePtr
                    },
                    new ()
                    {
                        type = mpv_render_param_type.MPV_RENDER_PARAM_SW_FORMAT,
                        data = (void*)marshalHelper.CStringFromManagedUTF8String(format)
                    },
                    new ()
                    {
                        type = mpv_render_param_type.MPV_RENDER_PARAM_SW_STRIDE,
                        data = stridePtr
                    },
                    new ()
                    {
                        type = mpv_render_param_type.MPV_RENDER_PARAM_SW_POINTER,
                        data = (void*)surfaceAddress
                    },
                    new ()
                    {
                        type = mpv_render_param_type.MPV_RENDER_PARAM_INVALID,
                        data = null
                    }
                };
                int errorCode;
                fixed (mpv_render_param* parametersPtr = parameters)
                {
                    errorCode = mpv_render_context_render(_renderContext, parametersPtr);
                }
                CheckCode(errorCode);
            }
        }
    }

    public void StartNativeRendering(long hw)
    {
        if (_disposed) return;
        SetPropertyLong("wid", hw);
    }

    public void StopRendering()
    {
        Command("stop");
        if (_renderContext != null)
        {
            mpv_render_context_free(_renderContext);
            _renderContext = null;
        }
        else
        {
            SetPropertyLong("wid", 0);
        }
    }
}
