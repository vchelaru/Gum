using System;

namespace Gum.Services;

/// <summary>
/// Marshals work onto the UI thread. Abstracts <c>System.Windows.Threading.Dispatcher</c> so
/// ViewModels can schedule deferred/coalesced work without a WPF dependency (ADR-0005).
/// </summary>
public interface IDispatcher
{
    /// <summary>Runs <paramref name="action"/> on the UI thread, blocking until it completes.</summary>
    void Invoke(Action action);

    /// <summary>Schedules <paramref name="action"/> to run on the UI thread without blocking the caller.</summary>
    void Post(Action action);
}
