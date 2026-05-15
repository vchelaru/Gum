#if XNALIKE
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.Renderables;

/// <summary>
/// Core-default <see cref="IStrokedRectangleRenderable"/> implementation. Draws as an outline
/// rectangle using <see cref="LineRectangle"/>'s line-polygon machinery.
/// </summary>
/// <remarks>
/// Registered at module load by <see cref="RegisterRuntimeTypes"/>. Stores
/// <c>StrokeWidth</c> via <see cref="LineRectangle.LinePixelWidth"/>; stores
/// <see cref="CornerRadius"/> but does not render it — LineRectangle is hard-cornered.
/// MonoGameGumShapes overrides this with Apos.Shapes' rounded outline.
/// </remarks>
public class DefaultStrokedRectangleRenderable : LineRectangle, IStrokedRectangleRenderable
{
    /// <inheritdoc cref="DefaultStrokedCircleRenderable.RegisterRuntimeTypes"/>
#pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    [System.Runtime.CompilerServices.ModuleInitializer]
#pragma warning restore CA2255
    public static void RegisterRuntimeTypes()
    {
        RenderableRegistry.RegisterDefaultFactory<IStrokedRectangleRenderable>(
            () => new DefaultStrokedRectangleRenderable());
    }

    XnaColor IStrokedRectangleRenderable.Color
    {
        get => XNAExtensions.ToXNA(this.Color);
        set => this.Color = XNAExtensions.ToSystemDrawing(value);
    }

    float IStrokedRectangleRenderable.StrokeWidth
    {
        get => this.LinePixelWidth;
        set => this.LinePixelWidth = value;
    }

    // Stored but not rendered: LineRectangle is hard-cornered.
    public float CornerRadius { get; set; }
}
#endif
