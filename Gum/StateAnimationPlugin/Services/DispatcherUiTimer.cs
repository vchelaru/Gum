using Gum.Services;
using System;
using System.Windows.Threading;

namespace StateAnimationPlugin.Services;

/// <summary>
/// <see cref="IUiTimer"/> implementation backed by a WPF <see cref="DispatcherTimer"/>. Keeps the
/// System.Windows.Threading dependency out of ElementAnimationsViewModel (ADR-0005, issue #3754)
/// by living in the WPF-capable StateAnimationPlugin assembly instead.
/// </summary>
internal class DispatcherUiTimer : IUiTimer
{
    private readonly DispatcherTimer _timer;

    public event Action? Tick;

    public DispatcherUiTimer()
    {
        _timer = new DispatcherTimer();
        _timer.Tick += (_, _) => Tick?.Invoke();
    }

    public void Start(TimeSpan interval)
    {
        _timer.Interval = interval;
        _timer.Start();
    }

    public void Stop() => _timer.Stop();
}
