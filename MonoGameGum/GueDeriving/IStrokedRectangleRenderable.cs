#if XNALIKE
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace Gum.GueDeriving;

/// <summary>
/// Stroke-slot role contract for <see cref="RectangleRuntime"/>'s two-slot renderable model
/// (issue #2768). Implementations: <see cref="MonoGameGum.Renderables.DefaultStrokedRectangleRenderable"/>
/// in core (wraps <c>LineRectangle</c>; ignores
/// <see cref="CornerRadius"/>) and Apos.Shapes' <c>RoundedRectangle</c> with
/// <c>IsFilled = false</c> in MonoGameGumShapes. Pair: <see cref="IFilledRectangleRenderable"/>.
/// </summary>
public interface IStrokedRectangleRenderable : IRenderable
{
    /// <summary>
    /// Color of the outline. Pushed by <see cref="RectangleRuntime.StrokeColor"/>.
    /// </summary>
    Color Color { get; set; }

    /// <summary>
    /// Width of the outline. The runtime pushes its own ScreenPixel-aware
    /// <c>StrokeWidth</c> here each frame in <c>PreRender</c>.
    /// </summary>
    float StrokeWidth { get; set; }

    /// <summary>
    /// Rounded-corner radius in pixels. Stored on the core default but not rendered
    /// (LineRectangle is axis-aligned, no rounded corners); honored on Apos.Shapes. Same
    /// graceful-degradation contract as <see cref="IFilledRectangleRenderable.CornerRadius"/>.
    /// </summary>
    float CornerRadius { get; set; }

    /// <summary>
    /// Per-corner radius override. Null falls back to <see cref="CornerRadius"/>. Issue #2818:
    /// honored by Apos.Shapes' RoundedRectangle; stored but not rendered on the core default.
    /// </summary>
    float? CustomRadiusTopLeft { get; set; }

    /// <inheritdoc cref="CustomRadiusTopLeft"/>
    float? CustomRadiusTopRight { get; set; }

    /// <inheritdoc cref="CustomRadiusTopLeft"/>
    float? CustomRadiusBottomLeft { get; set; }

    /// <inheritdoc cref="CustomRadiusTopLeft"/>
    float? CustomRadiusBottomRight { get; set; }
}
#endif
