using LibMpv.Client;
using static LibMpv.Client.libmpv;

namespace LibMpv.Context;

public unsafe class MpvEventLoop : IDisposable
{
    private readonly mpv_handle* _context;
    private readonly Action<mpv_event> _handleEvent;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _eventLoopTask;
    
    public MpvEventLoop(mpv_handle* context, Action<mpv_event> eventHandler)
    {
        _context = context;
        _handleEvent = eventHandler;
    }

    public void Stop()
    {
        if (_eventLoopTask?.Status == TaskStatus.Running)
        {
            _cancellationTokenSource!.Cancel();
            mpv_wakeup(_context);
            _eventLoopTask.Wait();
        }
    }

    public void Start()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _eventLoopTask = Task.Factory.StartNew(EventLoop, _cancellationTokenSource.Token, 
            TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach);
    }

    private void EventLoop(object? cancellationToken)
    {
        if (cancellationToken is not CancellationToken ct)
        {
            return;
        }
        while (!ct.IsCancellationRequested)
        {
            var eventPtr = mpv_wait_event(_context, -1);
            if (eventPtr != null)
            {
                var mpvEvent = MarshalHelper.PtrToStructure<mpv_event>((nint)eventPtr);
                if (mpvEvent.event_id != mpv_event_id.MPV_EVENT_NONE)
                {
                    _handleEvent(mpvEvent);
                }
            }
        }
    }

    private void ReleaseUnmanagedResources()
    {
        Stop();
        _eventLoopTask?.Dispose();
    }

    ~MpvEventLoop()
    {
        ReleaseUnmanagedResources();
    }
    
    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }
}
