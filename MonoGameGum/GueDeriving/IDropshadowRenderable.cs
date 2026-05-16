#if XNALIKE
using Microsoft.Xna.Framework;

namespace Gum.GueDeriving;

/// <summary>
/// Opt-in dropshadow surface for fill/stroke renderables in the two-slot shape runtime
/// model (issue #2797). Implemented by the optional MonoGameGumShapes <c>Circle</c> /
/// <c>RoundedRectangle</c> (the property bag is inherited from <c>RenderableShapeBase</c>)
/// so <see cref="CircleRuntime"/> and friends can push dropshadow state through to a
/// shadow-capable slot. Core defaults like
/// <see cref="MonoGameGum.Renderables.DefaultStrokedCircleRenderable"/> deliberately do NOT
/// implement this interface — <c>LineCircle</c> has no shadow concept. Setters round-trip
/// on the runtime but render as a no-op without the optional package (graceful degradation,
/// matching the <see cref="CircleRuntime.FillColor"/>, <see cref="IGradientedRenderable"/>,
/// and <see cref="IAntialiasedRenderable"/> patterns).
/// </summary>
/// <remarks>
/// Property names mirror <c>SkiaShapeRuntime</c> so the runtime APIs match across backends.
/// Unlike <see cref="IGradientedRenderable"/> (which the runtime pushes to BOTH fill and
/// stroke so a single gradient covers the filled disk and dashed-stroke ring at once), a
/// dropshadow is drawn once per renderable — pushing the shadow params to both fill and
/// stroke would render the shadow twice and visibly double up. The runtime prefers the fill
/// slot and falls back to stroke when fill is null.
/// </remarks>
public interface IDropshadowRenderable
{
    bool HasDropshadow { get; set; }

    Color DropshadowColor { get; set; }
    int DropshadowAlpha { get; set; }
    int DropshadowRed { get; set; }
    int DropshadowGreen { get; set; }
    int DropshadowBlue { get; set; }

    float DropshadowOffsetX { get; set; }
    float DropshadowOffsetY { get; set; }

    float DropshadowBlurX { get; set; }
    float DropshadowBlurY { get; set; }
}
#endif
