using System;

namespace Gum.Diagnostics;

/// <summary>
/// Pure registry backing <see cref="UiFreezeWatchdogHook"/>: holds the suspend/resume callbacks a
/// watchdog registers, and hands out scope objects that invoke them. Split out from the static
/// facade so this logic is unit-testable in isolation (same shape as <see cref="StartupTimingLog"/>
/// backing <see cref="StartupTiming"/>).
/// </summary>
public sealed class UiFreezeWatchdogHookRegistry
{
    private Action? _suspend;
    private Action? _resume;

    /// <summary>Wires this registry to the real watchdog implementation.</summary>
    public void Register(Action suspend, Action resume)
    {
        _suspend = suspend;
        _resume = resume;
    }

    /// <summary>
    /// Invokes the registered suspend callback (a no-op if nothing has registered) and returns a
    /// scope whose disposal invokes the registered resume callback.
    /// </summary>
    public IDisposable SuspendScope()
    {
        _suspend?.Invoke();
        return new ResumeScope(this);
    }

    private sealed class ResumeScope : IDisposable
    {
        private readonly UiFreezeWatchdogHookRegistry _owner;
        private bool _disposed;

        public ResumeScope(UiFreezeWatchdogHookRegistry owner)
        {
            _owner = owner;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _owner._resume?.Invoke();
        }
    }
}
