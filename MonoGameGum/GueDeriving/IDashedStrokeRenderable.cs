#if XNALIKE
namespace Gum.GueDeriving;

/// <summary>
/// Opt-in dashed-stroke surface for stroke renderables in the two-slot shape runtime model
/// (issue #2796). Implemented by the optional MonoGameGumShapes <c>Circle</c> /
/// <c>RoundedRectangle</c> (the property bag is inherited from <c>RenderableShapeBase</c>)
/// so <see cref="CircleRuntime"/> and friends can push dashed-stroke state through to a
/// dash-capable slot. Core defaults like
/// <see cref="MonoGameGum.Renderables.DefaultStrokedCircleRenderable"/> deliberately do NOT
/// implement this interface — <c>LineCircle</c> has no dash concept. Setters round-trip on
/// the runtime but render as a no-op without the optional package (graceful degradation,
/// matching the <see cref="CircleRuntime.FillColor"/>, <see cref="IGradientedRenderable"/>,
/// <see cref="IAntialiasedRenderable"/>, and <see cref="IDropshadowRenderable"/> patterns).
/// </summary>
/// <remarks>
/// Property names mirror <c>SkiaShapeRuntime</c> so the runtime APIs match across backends.
/// Dashed strokes are a stroke-only concept (Apos.Shapes' <c>Circle.RenderDashed</c> path is
/// guarded by <c>!IsFilled</c>), so the runtime pushes these values to the stroke slot only;
/// pushing to a fill slot would be ignored anyway. The runtime pushes each frame in
/// <c>PreRender</c> so ScreenPixel-scaled dash/gap stays in sync with the camera zoom — same
/// pattern as <see cref="CircleRuntime.StrokeWidth"/>.
/// </remarks>
public interface IDashedStrokeRenderable
{
    /// <summary>
    /// Length of each dash segment in pixels. A value of 0 (the default) produces a solid
    /// stroke; both dash and gap must be &gt; 0 for dashed rendering to engage.
    /// </summary>
    float StrokeDashLength { get; set; }

    /// <summary>
    /// Length of each gap between dashes in pixels. Ignored when
    /// <see cref="StrokeDashLength"/> is 0.
    /// </summary>
    float StrokeGapLength { get; set; }
}
#endif
