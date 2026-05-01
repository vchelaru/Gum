namespace RenderingLibrary.Graphics;

/// <summary>
/// Owns the BatchKey transition state machine extracted from <see cref="Renderer"/>.
/// As the renderer walks renderables, each one is announced to <see cref="OnRenderable"/>;
/// the orchestrator decides when to flush the previous custom batch and start a new
/// one via the renderable's <see cref="IRenderable.StartBatch"/> / <see cref="IRenderable.EndBatch"/> hooks.
/// <para>
/// Pulled out of <c>Renderer</c> so the transition rules can be unit-tested with mock
/// <see cref="IRenderable"/> instances — no MonoGame device required. See <c>BatchOrchestratorTests</c>
/// for the full set of invariants this enforces.
/// </para>
/// </summary>
public sealed class BatchOrchestrator
{
    string _currentBatchKey = string.Empty;
    IRenderable? _lastBatchOwner;

    /// <summary>The batch key that the most recently announced renderable established.</summary>
    public string CurrentBatchKey => _currentBatchKey;

    /// <summary>The renderable whose <c>StartBatch</c> was most recently called and not yet ended.</summary>
    public IRenderable? LastBatchOwner => _lastBatchOwner;

    /// <summary>
    /// Announce that <paramref name="renderable"/> is about to be rendered. If its
    /// <see cref="IRenderable.BatchKey"/> is non-empty AND differs from the current
    /// batch key, the previous batch's <c>EndBatch</c> is invoked and the new
    /// renderable's <c>StartBatch</c> is invoked. Empty BatchKey (e.g. plain
    /// <c>ContainerRuntime</c> wrappers) is a no-op — the renderable participates
    /// in whatever batch the previous renderable established.
    /// </summary>
    public void OnRenderable(IRenderable renderable, ISystemManagers managers)
    {
        var newKey = renderable.BatchKey;
        if (string.IsNullOrEmpty(newKey) || newKey == _currentBatchKey)
        {
            return;
        }

        _lastBatchOwner?.EndBatch(managers);
        _currentBatchKey = newKey;
        _lastBatchOwner = renderable;
        renderable.StartBatch(managers);
    }

    /// <summary>
    /// Flushes any pending custom batch and clears state. Call at the end of a
    /// render walk (<c>Renderer.End</c> and end of <c>RenderLayer</c>) and when
    /// restoring clip rectangles mid-walk. Safe to call repeatedly — when there's
    /// no pending owner this is a no-op.
    /// </summary>
    public void FlushAndReset(ISystemManagers managers)
    {
        _lastBatchOwner?.EndBatch(managers);
        _lastBatchOwner = null;
        _currentBatchKey = string.Empty;
    }
}
