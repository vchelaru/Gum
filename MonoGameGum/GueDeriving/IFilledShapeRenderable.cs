#if XNALIKE
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace Gum.GueDeriving;

/// <summary>
/// Capability contract for a renderable that can draw a filled-or-outlined shape with a
/// configurable stroke width. Defined in core MonoGameGum so that runtimes like
/// <see cref="CircleRuntime"/> can drive a fill-capable renderable without core referencing
/// the optional Apos.Shapes (MonoGameGumShapes) package.
/// </summary>
/// <remarks>
/// Members are deliberately the minimum surface CircleRuntime's collapse needs (#2761). The
/// optional MonoGameGumShapes package implements this on its Apos.Shapes-backed renderables
/// and registers a context-bearing factory via <see cref="RenderableRegistry"/>. When that
/// package is not referenced no factory is registered and the runtime degrades to outline-only.
/// </remarks>
public interface IFilledShapeRenderable : IRenderable
{
    /// <summary>
    /// Whether the shape renders as a solid fill (<c>true</c>) or an outline (<c>false</c>).
    /// </summary>
    bool IsFilled { get; set; }

    /// <summary>
    /// The color used to render the shape.
    /// </summary>
    Color Color { get; set; }

    /// <summary>
    /// The width of the outline when <see cref="IsFilled"/> is <c>false</c>. The runtime
    /// pushes this from its own ScreenPixel-aware <c>StrokeWidth</c> in <c>PreRender</c>
    /// each frame; renderables should not divide by camera zoom themselves.
    /// </summary>
    float StrokeWidth { get; set; }
}
#endif
