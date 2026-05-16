#if XNALIKE
namespace Gum.GueDeriving;

/// <summary>
/// Opt-in anti-aliasing surface for fill/stroke renderables in the two-slot shape runtime
/// model (issue #2798). Implemented by the optional MonoGameGumShapes <c>Circle</c> /
/// <c>RoundedRectangle</c> (the property bag is inherited from <c>RenderableShapeBase</c>),
/// so <see cref="CircleRuntime"/> can push <c>IsAntialiased</c> through to whichever slots
/// support it. Core defaults like
/// <see cref="MonoGameGum.Renderables.DefaultStrokedCircleRenderable"/> deliberately do NOT
/// implement this interface — <c>LineCircle</c> has no AA concept, so the value is purely
/// advisory there. Setters round-trip on the runtime but render as a no-op without the
/// optional package (graceful degradation, matching the <see cref="CircleRuntime.FillColor"/>
/// and <see cref="IGradientedRenderable"/> patterns).
/// </summary>
public interface IAntialiasedRenderable
{
    /// <summary>
    /// When <c>true</c> (the default on Apos.Shapes) edges are rendered with 1 px of
    /// anti-aliasing. When <c>false</c> the AA radius is dropped to 0 for crisp rasterization —
    /// useful for retro / pixel-art / hairline patterns where AA bloom would widen a nominal
    /// 1 px stroke.
    /// </summary>
    bool IsAntialiased { get; set; }
}
#endif
