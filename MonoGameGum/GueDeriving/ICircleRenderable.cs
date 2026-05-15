#if XNALIKE
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace Gum.GueDeriving;

/// <summary>
/// Role contract for the renderable that backs <see cref="CircleRuntime"/>. Defined in core
/// MonoGameGum so the runtime can be constructed and bound without core referencing any
/// optional package. Implementations: <c>DefaultCircleRenderable</c> in core (outline-only),
/// and Apos.Shapes' <c>Circle</c> in the optional MonoGameGumShapes package (fill + stroke).
/// </summary>
/// <remarks>
/// <para>
/// This is deliberately a <i>role</i> interface rather than the smaller capability interfaces
/// recommended in the runtime-unification design doc — see issue #2761 for the reasoning.
/// CircleRuntime binds its renderable once at construction (no per-property swap), so it needs
/// a single complete surface up front. A composable capability shape (e.g. one interface for
/// IsFilled, another for stroke width, another for radius) would force the runtime to probe
/// the renderable for each capability at every property assignment — exactly the swap-thrash
/// the construct-time-binding model is designed to avoid.
/// </para>
/// <para>
/// Members are the minimum surface CircleRuntime needs today. Adding capabilities here (drop
/// shadow, gradient, dashed stroke, antialiasing toggle…) commits every implementor to support
/// them — wait for a second real implementor before widening this interface.
/// </para>
/// </remarks>
public interface ICircleRenderable : IRenderable
{
    /// <summary>
    /// Color the circle renders with. On implementors that distinguish fill from stroke, this
    /// is the fill color when <see cref="IsFilled"/> is true and the stroke color when false.
    /// </summary>
    Color Color { get; set; }

    /// <summary>
    /// Whether the circle renders as a solid fill (<c>true</c>) or an outline (<c>false</c>).
    /// Implementors without true fill support (the core default <c>DefaultCircleRenderable</c>)
    /// store the flag but always render an outline — a documented graceful degradation when the
    /// optional MonoGameGumShapes package is not installed.
    /// </summary>
    bool IsFilled { get; set; }

    /// <summary>
    /// Radius of the circle. Setting this is expected to keep the renderable's Width/Height
    /// in sync where applicable — the runtime additionally updates its own layout dimensions.
    /// </summary>
    float Radius { get; set; }

    /// <summary>
    /// Width of the outline when <see cref="IsFilled"/> is <c>false</c>. The runtime pushes
    /// this from its own ScreenPixel-aware <c>StrokeWidth</c> in <c>PreRender</c> each frame;
    /// renderables should not divide by camera zoom themselves.
    /// </summary>
    float StrokeWidth { get; set; }
}
#endif
