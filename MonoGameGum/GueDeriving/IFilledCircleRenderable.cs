#if XNALIKE
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace Gum.GueDeriving;

/// <summary>
/// Fill-slot role contract for <see cref="CircleRuntime"/>'s two-slot renderable model
/// (issue #2768). Defined in core MonoGameGum so the runtime can resolve a fill renderable
/// at construction without core referencing any optional package. Implementations: none in
/// core (a fill-capable circle requires the optional MonoGameGumShapes / Apos.Shapes
/// package). When no factory is registered, <see cref="RenderableRegistry.Create{T}(Gum.Wireframe.GraphicalUiElement)"/>
/// returns null and <see cref="CircleRuntime.FillColor"/> setters become no-ops — the
/// documented graceful degradation. Pair: <see cref="IStrokedCircleRenderable"/>.
/// </summary>
public interface IFilledCircleRenderable : IRenderable
{
    /// <summary>
    /// Color the filled circle renders with. Pushed by <see cref="CircleRuntime.FillColor"/>.
    /// </summary>
    Color Color { get; set; }

    /// <summary>
    /// Radius of the circle. The runtime keeps this in sync with the stroke slot and updates
    /// its own Width/Height to <c>2 * value</c>.
    /// </summary>
    float Radius { get; set; }

    /// <summary>
    /// Pixels to subtract from the rendered radius (issue #2834). Pushed by
    /// <see cref="CircleRuntime.PreRender"/> when the stroke slot is visible alongside the
    /// fill — pulling the fill's outer AA halo inside the stroke's opaque band prevents
    /// the two AA boundaries from compositing and producing a color bleed. Inset is
    /// applied at render time only; Width/Height stay layout-owned.
    /// </summary>
    float FillRadiusInset { get; set; }
}
#endif
