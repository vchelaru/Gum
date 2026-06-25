using System;

namespace Gum.Undo;

/// <summary>
/// Suppresses undo recording for as long as it is held. <c>UndoManager.RequestLock</c> hands one
/// out and tracks it in its lock collection; disposing the lock invokes the supplied callback,
/// which removes it from that collection (recording resumes once the last lock is released).
/// </summary>
/// <remarks>
/// The removal is wired as an injected <see cref="Action"/> rather than a direct reference back to
/// UndoManager so this type can live in the headless Gum.Presentation assembly while the concrete
/// UndoManager stays in the WPF shell (ADR-0005 Phase 3 back-edge break).
/// </remarks>
public class UndoLock : IDisposable
{
    private readonly Action _onDispose;

    public UndoLock(Action onDispose)
    {
        _onDispose = onDispose;
    }

    public void Dispose()
    {
        _onDispose?.Invoke();
    }
}
