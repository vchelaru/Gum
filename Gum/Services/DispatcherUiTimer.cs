using System;
using System.Windows.Threading;

namespace Gum.Services;

/// <summary>
/// <see cref="IUiTimer"/> implementation backed by a WPF <see cref="DispatcherTimer"/>. Lives in the
/// WPF-capable Gum tool project so ViewModels in the headless Gum.Presentation assembly (ADR-0005)
/// can be driven by a periodic tick without referencing WPF themselves. Shared across plugins that
/// reference Gum.csproj (StateAnimationPlugin, PerformanceMeasurementPlugin) rather than duplicated
/// per plugin -- each caller constructs its own instance, so timers are never shared across
/// unrelated view models (issue #3754).
/// </summary>
public class DispatcherUiTimer : IUiTimer
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
