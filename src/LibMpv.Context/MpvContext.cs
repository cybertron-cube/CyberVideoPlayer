using System.Collections.Frozen;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using LibMpv.Client;
using static LibMpv.Client.libmpv;

namespace LibMpv.Context;

public unsafe partial class MpvContext : IDisposable
{
    private readonly mpv_handle* _ctx;
    private readonly MpvEventLoop _eventLoop;
    private readonly FrozenDictionary<Type, mpv_format> _mpvFormatTypeMapping = new Dictionary<Type, mpv_format>
    {
        { typeof(object), mpv_format.MPV_FORMAT_NONE },
        { typeof(string), mpv_format.MPV_FORMAT_STRING },
        { typeof(bool), mpv_format.MPV_FORMAT_FLAG },
        { typeof(long), mpv_format.MPV_FORMAT_INT64 },
        { typeof(double), mpv_format.MPV_FORMAT_DOUBLE }
    }.ToFrozenDictionary();

    // TODO Reuse userdata values that have been unobserved
    // Cap out at 18,446,744,073,709,551,615 from 0
    // **Shouldn't** need to reuse userdata values with the limit so high
    private ulong _propertyUserData;
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

    // TODO Make a list of mpv property names and create matching properties in a class that inherits this
    /// <summary>
    /// Required to use the correct type for the property unless using object type
    /// </summary>
    /// <param name="propertyName">Use object for properties you don't want a return value</param>
    /// <typeparam name="T">The C# type equivalent to the mpv property's return value</typeparam>
    /// <returns></returns>
    public IObservable<T> ObserveProperty<T>(string propertyName)
    {
        var mpvFormat = _mpvFormatTypeMapping[typeof(T)];
        if (PropertyChangedObservables.TryGetValue((propertyName, mpvFormat),
                out var mpvPropertyObservable))
        {
            return mpvPropertyObservable.Cast<T>();
        }
        else
        {
            var userData = _propertyUserData++;
            ObserveProperty(userData, propertyName, mpvFormat);
            var newObservable = new MpvPropertyObservable<object>(this, userData);
            PropertyChangedObservables.Add((propertyName, mpvFormat), newObservable);
            return newObservable.Cast<T>();
        }
    }

    public void ObserveProperty(ulong userData, string name, mpv_format format)
    {
        CheckDisposed();
        var code = mpv_observe_property(_ctx, userData, name, format);
        CheckCode(code);
    }

    public int UnobserveProperty(ulong userData)
    {
        CheckDisposed();
        var code = mpv_unobserve_property(_ctx, userData);
        CheckCode(code);
        return code;
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
