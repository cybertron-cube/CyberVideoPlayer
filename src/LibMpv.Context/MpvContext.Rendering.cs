using System.Runtime.InteropServices;
using LibMpv.Client;
using static LibMpv.Client.libmpv;

namespace LibMpv.Context;

public delegate nint GetProcAddress(string name);
public delegate void UpdateCallback();

public sealed unsafe partial class MpvContext
{
    public void StartOpenGlRendering(GetProcAddress getProcAddress, UpdateCallback updateCallback)
    {
        if (_disposed) return;
        StopRendering();

        this.getProcAddress  = (_, name)=>(void*)getProcAddress(name);
        this.updateCallback  = _ =>updateCallback();

        using MarshalHelper marshalHelper = new MarshalHelper();

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
                data = (void*)marshalHelper.AllocHGlobal(new mpv_opengl_init_params()
                {
                    get_proc_address = this.getProcAddress,
                    get_proc_address_ctx = null

                })
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
            renderContext = contextPtr;
            mpv_render_context_set_update_callback(renderContext, this.updateCallback, null);
        }

        CheckCode(errorCode);
    }

    public void OpenGlRender(int width, int height, int fb = 0, int flipY = 0)
    {
        if (_disposed) return;
        if (renderContext == null) return;

        using var marshalHelper = new MarshalHelper();

        var fbo = new mpv_opengl_fbo()
        {
            w = width,
            h = height,
            fbo = fb
        };
        
        var handle = GCHandle.Alloc(fbo,GCHandleType.Pinned);

        var parameters = new mpv_render_param[]
        {
            new()
            {
                type= mpv_render_param_type.MPV_RENDER_PARAM_OPENGL_FBO,
                data=(void*)&fbo
            },
            new()
            {
                type= mpv_render_param_type.MPV_RENDER_PARAM_FLIP_Y,
                data = (void *) marshalHelper.AllocHGlobalValue(flipY)
            },
            new() 
            { 
                type= mpv_render_param_type.MPV_RENDER_PARAM_INVALID
            },
        };

        int errorCode;
        fixed (mpv_render_param* parametersPtr = parameters)
        {
            errorCode = mpv_render_context_render(renderContext, parametersPtr);
        }
        handle.Free();

        CheckCode(errorCode);
       
    }

    public void StartSoftwareRendering(UpdateCallback updateCallback)
    {
        if (_disposed) return;
        StopRendering();

        this.updateCallback = _ => updateCallback();

        using MarshalHelper marshalHelper = new MarshalHelper();

        var parameters = new mpv_render_param[]
        {
            new()
            {
                type = mpv_render_param_type.MPV_RENDER_PARAM_API_TYPE,
                data = (void*)marshalHelper.StringToHGlobalAnsi(libmpv.MPV_RENDER_API_TYPE_SW)
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

        int errorCode = 0;

        mpv_render_context* contextPtr = null;
        fixed (mpv_render_param* parametersPtr = parameters)
        {
            errorCode = mpv_render_context_create((mpv_render_context**)&contextPtr, _ctx, parametersPtr);
        }

        if (errorCode >= 0)
        {
            this.renderContext = (mpv_render_context*)contextPtr;
            mpv_render_context_set_update_callback(this.renderContext, this.updateCallback, null);
        }

        CheckCode(errorCode);
    }

    public void SoftwareRender(int width, int height, nint surfaceAddress, string format)
    {
        if (_disposed) return;
        if (renderContext == null) return;

        using MarshalHelper marshalHelper = new MarshalHelper();

        var size   = new int[2] { width, height };
        var stride = new uint[1] { (uint)width * 4 };

        fixed(int* sizePtr  = size)
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
                        data = (void *) marshalHelper.CStringFromManagedUTF8String(format)
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
                        data =null
                    }
                };
                int errorCode = 0;
                fixed (mpv_render_param* parametersPtr = parameters)
                {
                    errorCode = mpv_render_context_render(renderContext, parametersPtr);
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
        if (renderContext != null)
        {
            mpv_render_context_free(renderContext);
            renderContext = null;
        }
        else
        {
            SetPropertyLong("wid", 0);
        }
    }

    public bool IsCustomRendering() => renderContext != null;

    private  mpv_render_context* renderContext;
    private  mpv_opengl_init_params_get_proc_address getProcAddress;
    private  mpv_render_context_set_update_callback_callback updateCallback;
}
