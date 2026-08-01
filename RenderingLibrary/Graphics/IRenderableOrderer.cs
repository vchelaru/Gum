using System;
using System.Collections.Generic;

namespace RenderingLibrary.Graphics;

/// <summary>
/// Resolves the clip rectangle a <see cref="IRenderableIpso.ClipsChildren"/> renderable establishes,
/// and the bounds the off-screen cull (#2998) tests against, for one draw-list build.
/// <para>
/// A struct rather than an interface, deliberately: the render path rebuilds the draw list every
/// frame, so this must not allocate — an interface would mean either a per-call implementation
/// object or boxing. Declared here rather than in its own file so the type travels with
/// <see cref="IRenderableOrderer"/> through the per-consumer <c>Compile Include</c> links that
/// already carry it (RaylibGum, FRB), matching why <c>CameraScissorExtensions</c> lives in
/// <c>Camera.cs</c>.
/// </para>
/// <para>
/// Two forms. A <see cref="Camera"/> plus optional <see cref="Layer"/> covers the main pass and the
/// xnalike render-target bake, whose camera is already rebased into target-local space. A pair of
/// caller-supplied delegates covers a caller that maps rectangles itself — raylib's bake, which
/// rebases into render-target-local pixels from its own bake origin. The default value supplies no
/// mapping at all: no clip narrowing and no culling, which is what order-only unit tests want.
/// </para>
/// </summary>
public readonly struct ClipBoundsSource
{
    private readonly Camera? _camera;
    private readonly Layer? _layer;
    private readonly Func<IRenderableIpso, System.Drawing.Rectangle>? _getScissorRectangle;
    private readonly Func<IRenderableIpso, System.Drawing.Rectangle>? _getCullTestBounds;

    /// <summary>
    /// Maps through <paramref name="camera"/>, in the coordinate space that camera currently
    /// describes. <paramref name="layer"/> is honored for its <c>LayerCameraSettings</c> when
    /// supplied.
    /// </summary>
    public ClipBoundsSource(Camera camera, Layer? layer = null)
    {
        _camera = camera;
        _layer = layer;
        _getScissorRectangle = null;
        _getCullTestBounds = null;
    }

    /// <summary>
    /// Maps through caller-supplied delegates, for a caller drawing in a space no
    /// <see cref="Camera"/> describes. <paramref name="getCullTestBounds"/> is optional and falls
    /// back to <paramref name="getScissorRectangle"/>; supply it separately only when the drawn
    /// extent can exceed the declared bounds (wrapped text, #4144).
    /// <para>
    /// Callers on the render path should cache the delegates rather than passing lambdas inline,
    /// which allocates a closure per call.
    /// </para>
    /// </summary>
    public ClipBoundsSource(
        Func<IRenderableIpso, System.Drawing.Rectangle>? getScissorRectangle,
        Func<IRenderableIpso, System.Drawing.Rectangle>? getCullTestBounds = null)
    {
        _camera = null;
        _layer = null;
        _getScissorRectangle = getScissorRectangle;
        _getCullTestBounds = getCullTestBounds ?? getScissorRectangle;
    }

    /// <summary>
    /// Whether a clip rectangle can be resolved. When false the walk cannot narrow the active clip
    /// for descendants, so it leaves it unchanged.
    /// </summary>
    public bool CanResolveScissorRectangle => _camera != null || _getScissorRectangle != null;

    /// <summary>
    /// Whether cull-test bounds can be resolved. When false the off-screen cull is skipped entirely
    /// and the full walk is emitted.
    /// </summary>
    public bool CanResolveCullTestBounds => _camera != null || _getCullTestBounds != null;

    /// <summary>
    /// The clip rectangle <paramref name="renderable"/> establishes. Only call when
    /// <see cref="CanResolveScissorRectangle"/> is true.
    /// </summary>
    public System.Drawing.Rectangle GetScissorRectangle(IRenderableIpso renderable) =>
        _camera != null
            ? _camera.GetScissorRectangleFor(_layer, renderable)
            : _getScissorRectangle!(renderable);

    /// <summary>
    /// The bounds the off-screen cull tests against for <paramref name="renderable"/>. Only call
    /// when <see cref="CanResolveCullTestBounds"/> is true.
    /// </summary>
    public System.Drawing.Rectangle GetCullTestBounds(IRenderableIpso renderable) =>
        _camera != null
            ? _camera.GetCullTestBoundsFor(_layer, renderable)
            : _getCullTestBounds!(renderable);
}

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
    /// without requiring a <see cref="Layer"/>, in whatever coordinate space it is drawing into.
    /// </summary>
    /// <param name="roots">The top-level renderables of the subtree to flatten.</param>
    /// <param name="destination">Caller-owned buffer; cleared, then filled with the ordered commands.</param>
    /// <param name="clipBounds">
    /// How to resolve clip and cull-test rectangles for this subtree. Defaults to no mapping, which
    /// performs no clip narrowing and no culling — matching the <c>camera == null</c> behavior of
    /// the <see cref="Layer"/> overload.
    /// </param>
    void BuildDrawList(
        IList<IRenderableIpso> roots,
        List<DrawCommand> destination,
        ClipBoundsSource clipBounds = default);
}
