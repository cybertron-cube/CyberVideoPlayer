﻿using System.Runtime.CompilerServices;
using System.Text;
using LibMpv.Client;
using static LibMpv.Client.libmpv;

namespace LibMpv.Context;

public unsafe partial class MpvContext : IDisposable
{
    private readonly mpv_handle* _ctx;
    private readonly MpvEventLoop _eventLoop;
    private bool _disposed;
    
    public MpvContext()
    {
        _ctx = mpv_create();
        
        if (_ctx == null)
            throw new MpvException("Unable to create mpv_context. Currently, this can happen in the following situations - out of memory or LC_NUMERIC is not set to \"C\"");
        
        var code = mpv_initialize(_ctx);
        CheckCode(code);

        InitEventHandlers();

        _eventLoop = new MpvEventLoop(_ctx, HandleEvent);

        _eventLoop.Start();
    }

    ~MpvContext()
    {
        ReleaseUnmanagedResources();
    }

    public void RequestLogMessages(string level)
    {
        CheckDisposed();
        mpv_request_log_messages(_ctx, level);
    }

    #region Properties
    public string GetPropertyString(string name)
    {
        CheckDisposed();
        var value = mpv_get_property_string(_ctx, name);
        return UTF8Marshaler.FromNative(Encoding.UTF8, value);
    }

    public void SetPropertyString(string name, string newValue)
    {
        CheckDisposed();
        var code = mpv_set_property_string(_ctx, name, newValue);
        CheckCode(code);
    }

    public bool GetPropertyFlag(string name)
    {
        CheckDisposed();
        int code;
        var value = new[] { 0 };
        fixed(int* valuePtr = value)
        {
            code = mpv_get_property(_ctx, name, mpv_format.MPV_FORMAT_FLAG, valuePtr);
        }
        CheckCode(code);
        return value[0] == 1;
    }

    public void SetPropertyFlag(string name, bool newValue)
    {
        CheckDisposed();
        int code;
        var value = new[] { newValue ? 1 : 0 };
        fixed (int* valuePtr = value)
        {
            code = mpv_set_property(_ctx, name, mpv_format.MPV_FORMAT_FLAG, valuePtr);
        }
        CheckCode(code);
    }

    public long GetPropertyLong(string name)
    {
        CheckDisposed();
        int code;
        var value = new long[] { 0 };
        fixed (long* valuePtr = value)
        {
            code = mpv_get_property(_ctx, name, mpv_format.MPV_FORMAT_INT64, valuePtr);
        }
        CheckCode(code);
        return value[0];
    }

    public void SetPropertyLong(string name, long newValue)
    {
        CheckDisposed();
        int code;
        var value = new[] { newValue };
        fixed (long* valuePtr = value)
        {
            code = mpv_set_property(_ctx, name, mpv_format.MPV_FORMAT_INT64, valuePtr);
        }
        CheckCode(code);
    }

    public double GetPropertyDouble(string name)
    {
        CheckDisposed();
        int code;
        var value = new double[] { 0 };
        fixed (double* valuePtr = value)
        {
            code = mpv_get_property(_ctx, name, mpv_format.MPV_FORMAT_DOUBLE, valuePtr);
        }
        CheckCode(code);
        return value[0];
    }

    public void SetPropertyDouble(string name, double newValue)
    {
        CheckDisposed();
        int code;
        var value = new double[] { 0 };
        fixed (double* valuePtr = value)
        {
            code = mpv_set_property(_ctx, name, mpv_format.MPV_FORMAT_DOUBLE, valuePtr);
        }
        CheckCode(code);
    }

    public void ObserveProperty(ulong userData, string name, mpv_format format)
    {
        CheckDisposed();
        var code = mpv_observe_property(_ctx, userData, name, format);
        CheckCode(code);
    }

    public void UnobserveProperty(ulong userData)
    {
        CheckDisposed();
        var code = mpv_unobserve_property(_ctx, userData);
        CheckCode(code);
    }

    #endregion

    public void Command(params string[] args)
    {
        if (args.Length == 0)
            throw new ArgumentException("Missing arguments.", nameof(args));
        
        CheckDisposed();

        using var helper = new MarshalHelper();
        var code = mpv_command(_ctx, (byte**)helper.CStringArrayForManagedUTF8StringArray(args));
        
        CheckCode(code);
    }


    public void CommandAsync(ulong userData, params string[] args)
    {
        if (args.Length == 0)
            throw new ArgumentException("Missing arguments.", nameof(args));

        CheckDisposed();

        using var helper = new MarshalHelper();
        var code = mpv_command_async(_ctx, userData, (byte**)helper.CStringArrayForManagedUTF8StringArray(args));

        CheckCode(code);
    }

    public void SetOptionString(string name, string data)
    {
        CheckDisposed();
        var code = mpv_set_option_string(_ctx, name, data);
        CheckCode(code);
    }

    public void RequestEvent(mpv_event_id @event, bool enabled)
    {
        CheckDisposed();
        var code = mpv_request_event(_ctx, @event, enabled ? 1 : 0);
        CheckCode(code);
    }

    public string EventName(mpv_event_id @event)
    {
        return mpv_event_name(@event);
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    public void ReleaseUnmanagedResources()
    {
        if (!_disposed)
        {
            StopRendering();
            _eventLoop.Stop();
            _eventLoop.Dispose();
            mpv_terminate_destroy(_ctx);
            _disposed = true;
        }
    }
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CheckCode(int code)
    {
        if (code >= 0) return;
        throw MpvException.FromCode(code);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(MpvContext));
    }
}
