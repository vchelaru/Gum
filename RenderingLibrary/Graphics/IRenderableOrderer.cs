using System;
using System.Collections.Generic;

namespace RenderingLibrary.Graphics;

/// <summary>
/// Produces the flat <see cref="DrawCommand"/> sequence for a render pass. Pluggable so that
/// alternative orderings (e.g. batch-grouped) can be swapped in without touching the renderer's
/// submit phase. The default implementation is <see cref="HierarchicalOrderer"/>, which
/// preserves the legacy depth-first walk.
/// </summary>
public interface IRenderableOrderer
{
    /// <summary>
    /// Builds the ordered command list for <paramref name="layer"/> into the caller-owned
    /// <paramref name="destination"/>. Implementations MUST clear <paramref name="destination"/>
    /// before appending so the renderer can pool a single buffer across layers and frames.
    /// </summary>
    /// <param name="layer">The layer whose renderables are flattened into the ordered draw commands.</param>
    /// <param name="destination">Caller-owned buffer; cleared, then filled with the ordered commands.</param>
    /// <param name="camera">
    /// The render camera, used for the off-screen cull (#2998): renderables falling entirely
    /// outside an active clip rectangle are skipped. Optional — when null (e.g. order-only unit
    /// tests) no culling is performed and the full walk is emitted.
    /// </param>
    void BuildDrawList(Layer layer, List<DrawCommand> destination, Camera? camera = null);

    /// <summary>
    /// Builds the ordered command list for an arbitrary subtree of renderables — e.g. a
    /// render-target container's children being baked into an offscreen texture — rather than a
    /// <see cref="Layer"/>'s top-level renderables. Lets a caller outside the main render pass
    /// reuse the same visibility / off-screen-cull / <see cref="IRenderableIpso.ClipsChildren"/> /
    /// hierarchy-traversal semantics as <see cref="BuildDrawList(Layer, List{DrawCommand}, Camera?)"/>
    /// without requiring a <see cref="Layer"/> or a screen-space <see cref="Camera"/> — the caller
    /// supplies the renderable-to-rectangle mappings directly, in whatever coordinate space it is
    /// drawing into (screen space for the main pass, render-target-local space for a bake).
    /// </summary>
    /// <param name="roots">The top-level renderables of the subtree to flatten.</param>
    /// <param name="destination">Caller-owned buffer; cleared, then filled with the ordered commands.</param>
    /// <param name="getScissorRectangle">
    /// Maps a renderable to the clip/scissor rectangle it establishes when it has
    /// <see cref="IRenderableIpso.ClipsChildren"/> set — used to narrow the active clip for its
    /// descendants. Optional — when null, no off-screen culling or clip narrowing is performed
    /// (e.g. order-only unit tests), matching the <c>camera == null</c> behavior of the
    /// <see cref="Layer"/> overload.
    /// </param>
    /// <param name="getCullTestBounds">
    /// Maps a renderable to the bounds used for the off-screen cull (#2998) — separate from
    /// <paramref name="getScissorRectangle"/> so a caller can widen the cull test for content whose
    /// actual drawn extent exceeds its declared bounds (e.g. wrapped text, #4144) without changing
    /// what is actually clipped. Optional — when null but <paramref name="getScissorRectangle"/>
    /// is supplied, falls back to <paramref name="getScissorRectangle"/> for the cull test.
    /// </param>
    void BuildDrawList(
        IList<IRenderableIpso> roots,
        List<DrawCommand> destination,
        Func<IRenderableIpso, System.Drawing.Rectangle>? getScissorRectangle = null,
        Func<IRenderableIpso, System.Drawing.Rectangle>? getCullTestBounds = null);
}
