using System;
using Avalonia.Threading;
using Gum.Services;

namespace Gum.Avalonia.Services;

/// <summary>
/// <see cref="IUiTimer"/> on an Avalonia <see cref="DispatcherTimer"/>, so its ticks run on the UI
/// thread. Twin of the WPF head's <c>DispatcherUiTimer</c>; each caller constructs its own instance.
/// </summary>
public sealed class AvaloniaUiTimer : IUiTimer
{
    private readonly DispatcherTimer _timer;

    /// <summary>Creates a stopped timer.</summary>
    public AvaloniaUiTimer()
    {
        _timer = new DispatcherTimer();
        _timer.Tick += (_, _) => Tick?.Invoke();
    }

    /// <inheritdoc/>
    public event Action? Tick;

    /// <inheritdoc/>
    public void Start(TimeSpan interval)
    {
        _timer.Interval = interval;
        _timer.Start();
    }

    /// <inheritdoc/>
    public void Stop() => _timer.Stop();
}
