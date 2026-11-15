using System;
using System.ComponentModel;
using System.Threading;

namespace RenderingLibrary.Graphics;

/// <summary>
/// Plumbing for renderers that want to skip their render-target tree scans when nothing in the
/// process can be a render target. A renderable holds one handle while it is a render-target
/// container, or while it points at one through <c>RenderTargetTextureSource</c>.
/// <see cref="AnyLive"/> is false only when no handle is held, so a false answer proves no scan
/// can find anything. An abandoned handle is released by its finalizer, so the answer can stay
/// true for a while after the last user is gone (the renderer then just scans, as it always did).
/// This is a cached fact about tree structure; it caches no rendering.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class RenderTargetUsageHandle
{
    private static int _liveCount;
    private int _released;

    /// <summary>True when any renderable may currently be a render target or reference one.</summary>
    public static bool AnyLive => Volatile.Read(ref _liveCount) > 0;

    private RenderTargetUsageHandle()
    {
    }

    /// <summary>Registers one render-target user. Call <see cref="Release"/> when it stops.</summary>
    public static RenderTargetUsageHandle Acquire()
    {
        Interlocked.Increment(ref _liveCount);
        return new RenderTargetUsageHandle();
    }

    /// <summary>Unregisters the user. Safe to call more than once.</summary>
    public void Release()
    {
        if (Interlocked.Exchange(ref _released, 1) == 0)
        {
            Interlocked.Decrement(ref _liveCount);
            GC.SuppressFinalize(this);
        }
    }

    ~RenderTargetUsageHandle() => Release();
}
