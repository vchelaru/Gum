#if XNALIKE
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace Gum.GueDeriving;

/// <summary>
/// Fill-slot role contract for <see cref="RectangleRuntime"/>'s two-slot renderable model
/// (issue #2768). Implementations: <see cref="MonoGameGum.Renderables.DefaultFilledRectangleRenderable"/>
/// in core (wraps <c>SolidRectangle</c>; ignores
/// <see cref="CornerRadius"/>) and Apos.Shapes' <c>RoundedRectangle</c> with
/// <c>IsFilled = true</c> in MonoGameGumShapes. Pair: <see cref="IStrokedRectangleRenderable"/>.
/// </summary>
public interface IFilledRectangleRenderable : IRenderable
{
    /// <summary>
    /// Color of the fill. Pushed by <see cref="RectangleRuntime.FillColor"/>.
    /// </summary>
    Color Color { get; set; }

    /// <summary>
    /// Rounded-corner radius in pixels. Stored on the core default but not rendered
    /// (<c>SolidRectangle</c> has no rounded-corner support);
    /// (<c>SolidRectangle</c>) honored fully by Apos.Shapes. This is intentional graceful degradation — install
    /// MonoGameGumShapes for rounded corners. Kept in lockstep with the stroke slot.
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
