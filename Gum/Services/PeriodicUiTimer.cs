using System;
using System.Threading;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;

namespace Gum.Services;

public class PeriodicUiTimer : IDisposable
{
    private IDispatcher Dispatcher { get; }
    private ILogger Logger { get; }
    
    private readonly Timer _timer;
    private volatile bool _enabled;
    private int _inTick; // 0 = idle, 1 = running

    public event Action? Tick;

    public PeriodicUiTimer(IDispatcher dispatcher, ILogger<PeriodicUiTimer> logger)
    {
        Dispatcher = dispatcher;
        Logger = logger;
        _timer = new Timer(OnTimerCallback, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    private TimeSpan Interval { get; set; } = Timeout.InfiniteTimeSpan;

    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            _timer.Change(value ? Interval : Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
    }

    public void Start(TimeSpan interval)
    { 
        Interval = interval;
        Enabled = true;
    }
    
    public void Stop() => Enabled = false;

    private void OnTimerCallback(object? _)
    {
        if (Interlocked.Exchange(ref _inTick, 1) == 1)
        {
            if (_enabled) _timer.Change(Interval, Timeout.InfiniteTimeSpan);
            return;
        }

        Dispatcher.Post(TryTick);
    }
    
    private void TryTick()
    {
        try
        {
            Tick?.Invoke();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error in UiTimer tick");
        }
        finally
        {
            Interlocked.Exchange(ref _inTick, 0);

            if (_enabled)
            {
                try { _timer.Change(Interval, Timeout.InfiniteTimeSpan); }
                catch (ObjectDisposedException) { /* ignore */ }
            }
        }
    }

    public void Dispose() => _timer.Dispose();
}