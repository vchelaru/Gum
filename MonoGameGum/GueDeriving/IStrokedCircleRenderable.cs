#if XNALIKE
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace Gum.GueDeriving;

/// <summary>
/// Stroke-slot role contract for <see cref="CircleRuntime"/>'s two-slot renderable model
/// (issue #2768). Implementations: <see cref="MonoGameGum.Renderables.DefaultStrokedCircleRenderable"/>
/// in core (wraps <c>LineCircle</c>), and Apos.Shapes'
/// <c>Circle</c> with <c>IsFilled = false</c> in the optional MonoGameGumShapes package. Pair:
/// <see cref="IFilledCircleRenderable"/>.
/// </summary>
public interface IStrokedCircleRenderable : IRenderable
{
    /// <summary>
    /// Color of the outline. Pushed by <see cref="CircleRuntime.StrokeColor"/>.
    /// </summary>
    Color Color { get; set; }

    /// <summary>
    /// Radius of the circle. Kept in sync with the fill slot by the runtime.
    /// </summary>
    float Radius { get; set; }

    /// <summary>
    /// Width of the outline. The runtime pushes its own ScreenPixel-aware
    /// <c>StrokeWidth</c> here each frame in <c>PreRender</c>; implementors should not
    /// divide by camera zoom themselves.
    /// </summary>
    float StrokeWidth { get; set; }
}
#endif
