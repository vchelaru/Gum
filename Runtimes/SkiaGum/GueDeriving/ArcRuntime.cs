using Gum.Wireframe;

#if SKIA
using SkiaGum.Renderables;
using SkiaSharp;
using RenderingLibrary.Graphics;
#else
using MonoGameAndGum.Renderables;
#endif

#if FRB
#if SKIA
namespace SkiaGum.GueDeriving;
#else
namespace MonoGameGum.GueDeriving;
#endif
#else
namespace Gum.GueDeriving;
#endif

/// <summary>
/// Runtime that draws a circular arc inscribed in its Width x Height bounds.
/// The arc is always stroked (it is never filled); use <see cref="Thickness"/> to control its weight.
/// </summary>
/// <remarks>
/// Source-shared between SkiaGum and MonoGameGumShapes (and KniGumShapes) via a Compile/Link in
/// the Apos-side csprojs - see <c>RoundedRectangleRuntime</c> for the same pattern. Platform
/// differences are gated behind <c>#if SKIA</c>; the cross-platform shape (constructor defaults,
/// StartAngle/SweepAngle/IsEndRounded properties, base wiring) is shared. Dashed strokes are
/// only rendered on Skia (Apos.Shapes' Arc primitive lacks dashing); the <c>StrokeDashLength</c>
/// property is exposed on both backends via the base class for API parity but no-ops on Apos.
/// </remarks>
public class ArcRuntime
#if SKIA
    : SkiaShapeRuntime
#else
    : AposShapeRuntime
#endif
{
    #region Contained Renderable
    protected override RenderableShapeBase ContainedRenderable => ContainedArc;

    Arc? _containedArc;
    Arc ContainedArc
    {
        get
        {
            if (_containedArc == null)
            {
                _containedArc = (Arc)this.RenderableComponent;
            }
            return _containedArc;
        }
    }
    #endregion

    /// <summary>
    /// Gets or sets the thickness of the arc, in pixels. Façade for <c>StrokeWidth</c> on the
    /// base shape runtime - the value is held on the runtime so <c>PreRender</c> can apply
    /// <c>StrokeWidthUnits</c> (e.g. ScreenPixel zoom scaling) before pushing it to the renderable
    /// each frame.
    /// </summary>
    public float Thickness
    {
        get => base.StrokeWidth;
        set => base.StrokeWidth = value;
    }

    /// <summary>
    /// Gets or sets the angle, in degrees, at which the arc begins. A value of 0 points to the right,
    /// and increasing values sweep counter-clockwise.
    /// </summary>
    public float StartAngle
    {
        get => ContainedArc.StartAngle;
        set => ContainedArc.StartAngle = value;
    }

    /// <summary>
    /// Gets or sets how far the arc sweeps from <see cref="StartAngle"/>, in degrees. A value of
    /// 360 produces a full ring.
    /// </summary>
    public float SweepAngle
    {
        get => ContainedArc.SweepAngle;
        set => ContainedArc.SweepAngle = value;
    }

    /// <summary>
    /// Gets or sets whether the ends of the arc are rounded. If true, the arc has rounded caps;
    /// if false, the ends are flat. Defaults to false on both backends as of issue #2728 - see
    /// <c>docs/gum-tool/upgrading/migrating-to-2026-may.md</c>. Existing Apos consumers who relied
    /// on the prior rounded default must set this to true explicitly.
    /// </summary>
    public bool IsEndRounded
    {
        get => ContainedArc.IsEndRounded;
        set => ContainedArc.IsEndRounded = value;
    }

    /// <summary>
    /// Initializes a new ArcRuntime. When <paramref name="fullInstantiation"/> is true (the
    /// default), an underlying <c>Arc</c> renderable is created and default values are applied
    /// (Width = Height = 100, StartAngle = 0, SweepAngle = 90, IsEndRounded = false,
    /// Thickness = 10, Color = White). Pass false only when the runtime is being constructed by
    /// deserialization, which sets up the renderable separately.
    /// </summary>
    public ArcRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
#if SKIA
            SetContainedObject(new Arc());
#pragma warning disable CS0618 // Color is obsolete; migration to FillColor/StrokeColor tracked in #2790 (depends on two-slot composition).
            this.Color = SKColors.White;
#pragma warning restore CS0618
#else
            SetContainedShape(new Arc());
            this.Color = Microsoft.Xna.Framework.Color.White;
#endif

            Width = 100;
            Height = 100;

            // Seed the runtime auto-property StrokeWidth so PreRender pushes the legacy Arc
            // default (10) to the renderable instead of overwriting it with 0.
            StrokeWidth = 10;

            // IsEndRounded intentionally left at the renderable's default of false. Locked in
            // unification (issue #2728) - this is a visible behavior change for Apos consumers
            // who relied on the previous rounded-caps default. See migration doc for details.

            StartAngle = 0;
            SweepAngle = 90;

            Red1 = 255;
            Green1 = 255;
            Blue1 = 255;

            Red2 = 255;
            Green2 = 255;
            Blue2 = 0;

            GradientX2 = 100;
            GradientY2 = 100;

            DropshadowAlpha = 255;
            DropshadowRed = 0;
            DropshadowGreen = 0;
            DropshadowBlue = 0;

            DropshadowOffsetX = 0;
            DropshadowOffsetY = 3;
            DropshadowBlurX = 0;
            DropshadowBlurY = 3;
        }
    }

    public override GraphicalUiElement Clone()
    {
        var toReturn = (ArcRuntime)base.Clone();

        // Reset the cached renderable reference so the clone re-resolves it from its own
        // RenderableComponent rather than holding a reference to the source instance's renderable.
        // Skia previously did this; Apos previously did not (latent bug, fixed by unification).
        toReturn._containedArc = null;

        return toReturn;
    }
}
