namespace Gum.Services;

/// <summary>
/// Exposes the active renderer's per-frame render-state-change counts and its sibling-ordering/
/// culling tuning switches, without exposing the XNALIKE-only RenderingLibrary.Graphics.Renderer/
/// SystemManagers types to the headless Gum.Presentation assembly (ADR-0005, issue #3754). The
/// counts read 0 when no canvas/renderer is active; the switches are backed by process-wide state
/// on the concrete renderer and are always readable/settable.
/// </summary>
public interface IRenderDiagnosticsService
{
    /// <summary>SpriteBatch begins in the last frame — one per render-state change.</summary>
    int SpriteBatchBeginCount { get; }

    /// <summary>Apos.Shapes ShapeBatch begins in the last frame.</summary>
    int ShapeBatchBeginCount { get; }

    /// <summary>
    /// True when the renderer regroups same-BatchKey draws into contiguous runs instead of
    /// walking siblings depth-first, reducing batch flushes at the cost of a reorder pass.
    /// </summary>
    bool SortByBatchKey { get; set; }

    /// <summary>
    /// When on, renderables that fall entirely outside an active clip region — and their
    /// children — are skipped.
    /// </summary>
    bool CullOffscreenWhenClipped { get; set; }
}
