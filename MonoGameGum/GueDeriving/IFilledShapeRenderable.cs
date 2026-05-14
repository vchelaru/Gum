#if XNALIKE
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace Gum.GueDeriving;

/// <summary>
/// Capability contract for a renderable that can draw a filled shape. Defined in core
/// MonoGameGum so that runtimes like <see cref="CircleRuntime"/> can drive a fill-capable
/// renderable without core referencing the optional Apos.Shapes package.
/// </summary>
/// <remarks>
/// The optional MonoGameGumShapes package implements this interface on its Apos.Shapes-backed
/// renderables and registers a factory via <see cref="ShapeRenderableRegistry"/>. When that
/// package is not referenced, no factory is registered and runtimes degrade to outline-only.
///
/// Spike (#2758): throwaway scope — only the members CircleRuntime's FillColor path needs.
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
}
#endif
