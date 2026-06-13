#if XNALIKE
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using SysDrawColor = System.Drawing.Color;
using XnaColor = Microsoft.Xna.Framework.Color;

#if FRB
namespace MonoGameGum.Renderables;
#else
namespace Gum.Renderables;
#endif

/// <summary>
/// Core-default <see cref="IFilledRectangleRenderable"/> implementation. Draws as a solid
/// filled rectangle using <see cref="SolidRectangle"/> — no extra dependencies beyond core
/// MonoGameGum.
/// </summary>
/// <remarks>
/// Registered at module load by <see cref="RegisterRuntimeTypes"/> as the default factory for
/// <see cref="IFilledRectangleRenderable"/>. The optional MonoGameGumShapes package overrides
/// this with an Apos.Shapes-backed <c>RoundedRectangle</c>. <see cref="CornerRadius"/> is
/// stored but <c>SolidRectangle</c> draws hard-cornered axis-aligned rectangles — the
/// documented graceful degradation when MonoGameGumShapes is not installed.
/// </remarks>
public class DefaultFilledRectangleRenderable : SolidRectangle, IFilledRectangleRenderable
{
    /// <inheritdoc cref="DefaultStrokedCircleRenderable.RegisterRuntimeTypes"/>
#pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    [System.Runtime.CompilerServices.ModuleInitializer]
#pragma warning restore CA2255
    public static void RegisterRuntimeTypes()
    {
        RenderableRegistry.RegisterDefaultFactory<IFilledRectangleRenderable>(
            () => new DefaultFilledRectangleRenderable());
    }

    XnaColor IFilledRectangleRenderable.Color
    {
        // SolidRectangle.Color is System.Drawing.Color; the interface speaks XNA Color.
        get => XNAExtensions.ToXNA(this.Color);
        set => this.Color = XNAExtensions.ToSystemDrawing(value);
    }

    // Stored but not rendered: SolidRectangle is hard-cornered. Set fully for round-tripping.
    public float CornerRadius { get; set; }

    public float? CustomRadiusTopLeft { get; set; }
    public float? CustomRadiusTopRight { get; set; }
    public float? CustomRadiusBottomLeft { get; set; }
    public float? CustomRadiusBottomRight { get; set; }

    // Stored but not rendered: SolidRectangle always draws at its full Width/Height. The
    // fill-inset visual (hiding the fill under a transparent stroke band) is an Apos.Shapes
    // feature; the core path keeps its historical full-edge fill. Set for round-tripping.
    public float FillInset { get; set; }
}
#endif
