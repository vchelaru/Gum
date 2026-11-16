#if BRUTAL
#nullable enable

using SkiaSharp;
using System;

namespace Gum;

/// <summary>
/// Takes the host's Vulkan queue lock around the queue work Gum does outside SkiaGameRendering: the device wait
/// before a resize, and freeing the targets and the backend. SkiaGameRendering takes the same lock itself around
/// Skia's submits. Gum never takes the lock while it already holds it (a host's lock may not be re-entrant), and
/// <see cref="Wrap"/> adds nothing at all when the host passed no lock. Used by Gum.Brutal.Vulkan; OpenGL has no queue.
/// </summary>
internal sealed class QueueLockedBackend : IBrutalGpuBackend
{
    private readonly Func<IDisposable> _acquireQueueLock;

    private QueueLockedBackend(IBrutalGpuBackend inner, Func<IDisposable> acquireQueueLock)
    {
        Inner = inner;
        _acquireQueueLock = acquireQueueLock;
    }

    /// <summary>The backend whose queue work this locks.</summary>
    internal IBrutalGpuBackend Inner { get; }

    /// <summary>Returns <paramref name="backend"/> itself when <paramref name="acquireQueueLock"/> is null, so an unsynchronized host pays nothing.</summary>
    internal static IBrutalGpuBackend Wrap(IBrutalGpuBackend backend, Func<IDisposable>? acquireQueueLock) =>
        acquireQueueLock == null ? backend : new QueueLockedBackend(backend, acquireQueueLock);

    /// <summary>The backend behind a wrapper, or <paramref name="backend"/> itself.</summary>
    internal static IBrutalGpuBackend? Unwrap(IBrutalGpuBackend? backend) =>
        backend is QueueLockedBackend locked ? locked.Inner : backend;

    /// <summary>The target behind a wrapper, or <paramref name="target"/> itself.</summary>
    internal static IBrutalTarget? Unwrap(IBrutalTarget? target) =>
        target is LockedTarget locked ? locked.Inner : target;

    /// <inheritdoc/>
    public IBrutalTarget CreateTarget(int width, int height) =>
        new LockedTarget(Inner.CreateTarget(width, height), _acquireQueueLock);

    /// <inheritdoc/>
    public void BeginFrame() => Inner.BeginFrame();

    /// <inheritdoc/>
    public void WaitForGpu()
    {
        using (_acquireQueueLock())
        {
            Inner.WaitForGpu();
        }
    }

    /// <inheritdoc/>
    public void Present(IBrutalTarget target, int width, int height) =>
        Inner.Present(Unwrap(target)!, width, height);

    /// <inheritdoc/>
    public void Dispose()
    {
        using (_acquireQueueLock())
        {
            Inner.Dispose();
        }
    }

    private sealed class LockedTarget : IBrutalTarget
    {
        private readonly Func<IDisposable> _acquireQueueLock;

        public LockedTarget(IBrutalTarget inner, Func<IDisposable> acquireQueueLock)
        {
            Inner = inner;
            _acquireQueueLock = acquireQueueLock;
        }

        public IBrutalTarget Inner { get; }

        public SKCanvas Canvas => Inner.Canvas;

        // Begin and End run SkiaGameRendering's own locked submits.
        public void Begin() => Inner.Begin();

        public void End() => Inner.End();

        public void EndWithoutDrawing() => Inner.EndWithoutDrawing();

        public void Dispose()
        {
            using (_acquireQueueLock())
            {
                Inner.Dispose();
            }
        }
    }
}
#endif
