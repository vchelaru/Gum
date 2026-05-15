#if XNALIKE
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.Renderables;

/// <summary>
/// Core-default <see cref="ICircleRenderable"/> implementation. Draws as an outline circle
/// using <see cref="LineCircle"/>'s line-polygon machinery, so it batches as a normal sprite
/// draw and has no extra dependencies beyond core MonoGameGum.
/// </summary>
/// <remarks>
/// <para>
/// Registered at module load by <see cref="RegisterRuntimeTypes"/> as the default factory for
/// <see cref="ICircleRenderable"/>. The optional MonoGameGumShapes package overrides this by
/// registering its Apos.Shapes <c>Circle</c> in the <i>active</i> layer; once that package is
/// loaded, every <see cref="CircleRuntime"/> from then on is Apos-backed instead.
/// </para>
/// <para>
/// This implementation has no true fill mode — setting <see cref="ICircleRenderable.IsFilled"/>
/// stores the flag but always renders as an outline. Likewise <see cref="ICircleRenderable.StrokeWidth"/>
/// is stored but the underlying <see cref="LineCircle"/> draws a fixed 1 px line. This is the
/// deliberate "graceful degradation" path for projects that haven't installed MonoGameGumShapes
/// (see issue #2761) — the visual is wrong but the layout, color, and radius round-trip cleanly.
/// </para>
/// </remarks>
public class DefaultCircleRenderable : LineCircle, ICircleRenderable
{
    /// <summary>
    /// Registers <see cref="DefaultCircleRenderable"/> as the default factory for
    /// <see cref="ICircleRenderable"/>. Called automatically by the runtime via
    /// <see cref="System.Runtime.CompilerServices.ModuleInitializerAttribute"/>; applications
    /// do not need to call this directly. GumService.Initialize's reflection scan also re-
    /// invokes it after Uninitialize → Initialize so the registration survives the
    /// <see cref="RenderableRegistry.Reset"/> that Uninitialize triggers.
    /// </summary>
#pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    [System.Runtime.CompilerServices.ModuleInitializer]
#pragma warning restore CA2255
    public static void RegisterRuntimeTypes()
    {
        RenderableRegistry.RegisterDefaultFactory<ICircleRenderable>(() => new DefaultCircleRenderable());
    }

    public DefaultCircleRenderable()
    {
        CircleOrigin = CircleOrigin.TopLeft;
    }

    XnaColor ICircleRenderable.Color
    {
        // LineCircle.Color is System.Drawing.Color; the interface speaks XNA Color. Convert
        // both directions via the same extensions that CircleRuntime's legacy Color setter
        // historically used.
        get => global::RenderingLibrary.Graphics.XNAExtensions.ToXNA(this.Color);
        set => this.Color = global::RenderingLibrary.Graphics.XNAExtensions.ToSystemDrawing(value);
    }

    // Stored but unused: LineCircle has no fill mode or stroke-width concept. Setting these
    // is intentionally a no-op visually — see class-level remarks for the degradation contract.
    public bool IsFilled { get; set; }

    public float StrokeWidth { get; set; } = 1f;
}
#endif
