using System;

namespace Gum.Diagnostics;

/// <summary>
/// Process-wide facade that lets shared code (e.g. <c>ProjectManager.LoadProjectAsync</c>) suspend the
/// Avalonia head's UI freeze watchdog for the duration of a known-long, synchronous operation,
/// without <c>Gum.ProjectServices</c>/<c>Gum.Presentation</c> taking a project reference to
/// <c>Tool.Gum.Avalonia</c> (which would invert the head/shared layering - those heads depend on
/// this project, not the other way around; the frozen WPF head has no watchdog of its own).
///
/// <c>UiFreezeWatchdog</c> wires itself in via <see cref="Register"/> once its poll thread starts.
/// Until then - or on the WPF head, which never registers - <see cref="SuspendScope"/> is a no-op,
/// mirroring the watchdog's own "no heartbeat yet = can't be a stall" invariant. Do not remove this
/// indirection thinking it is dead code: it exists specifically to keep the layering direction
/// correct while still letting project load suppress false-positive freeze reports.
/// </summary>
public static class UiFreezeWatchdogHook
{
    private static readonly UiFreezeWatchdogHookRegistry _registry = new UiFreezeWatchdogHookRegistry();

    /// <inheritdoc cref="UiFreezeWatchdogHookRegistry.Register"/>
    public static void Register(Action suspend, Action resume) => _registry.Register(suspend, resume);

    /// <inheritdoc cref="UiFreezeWatchdogHookRegistry.SuspendScope"/>
    public static IDisposable SuspendScope() => _registry.SuspendScope();
}
