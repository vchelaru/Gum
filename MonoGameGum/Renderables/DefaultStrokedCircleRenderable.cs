#if XNALIKE
using Gum.GueDeriving;
using RenderingLibrary.Math.Geometry;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.Renderables;

/// <summary>
/// Core-default <see cref="IStrokedCircleRenderable"/> implementation. Draws as an outline
/// circle using <see cref="LineCircle"/>'s line-polygon machinery — no extra dependencies
/// beyond core MonoGameGum, batches as a normal sprite draw.
/// </summary>
/// <remarks>
/// Registered at module load by <see cref="RegisterRuntimeTypes"/> as the default factory for
/// <see cref="IStrokedCircleRenderable"/>. The optional MonoGameGumShapes package overrides
/// this with an Apos.Shapes-backed <c>Circle</c>. Stores <see cref="StrokeWidth"/> but
/// <c>LineCircle</c> always draws a 1 px line — the documented graceful degradation when
/// MonoGameGumShapes is not installed. There is no core default for
/// <see cref="IFilledCircleRenderable"/> — fill is a no-op without the optional package
/// (issue #2768).
/// </remarks>
public class DefaultStrokedCircleRenderable : LineCircle, IStrokedCircleRenderable
{
    /// <summary>
    /// Registers <see cref="DefaultStrokedCircleRenderable"/> as the default factory for
    /// <see cref="IStrokedCircleRenderable"/>. Called automatically by the runtime via
    /// <see cref="System.Runtime.CompilerServices.ModuleInitializerAttribute"/>;
    /// applications do not need to call this directly. <c>GumService.Initialize</c>'s
    /// reflection scan also re-invokes it after Uninitialize → Initialize so the
    /// registration survives the <see cref="RenderableRegistry.Reset"/> that Uninitialize
    /// triggers.
    /// </summary>
#pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    [System.Runtime.CompilerServices.ModuleInitializer]
#pragma warning restore CA2255
    public static void RegisterRuntimeTypes()
    {
        RenderableRegistry.RegisterDefaultFactory<IStrokedCircleRenderable>(
            () => new DefaultStrokedCircleRenderable());
    }

    public DefaultStrokedCircleRenderable()
    {
        CircleOrigin = CircleOrigin.TopLeft;
    }

    XnaColor IStrokedCircleRenderable.Color
    {
        // LineCircle.Color is System.Drawing.Color; the interface speaks XNA Color. Convert
        // both directions via the same extensions that CircleRuntime's legacy Color setter
        // historically used.
        get => RenderingLibrary.Graphics.XNAExtensions.ToXNA(this.Color);
        set => this.Color = RenderingLibrary.Graphics.XNAExtensions.ToSystemDrawing(value);
    }

    // Stored but unused: LineCircle has no stroke-width concept. Setting this is intentionally
    // a no-op visually — see class-level remarks for the degradation contract.
    public float StrokeWidth { get; set; } = 1f;
}
#endif
