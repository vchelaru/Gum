using Gum.Wireframe;
using System;

#if RAYLIB
using Gum.DataTypes;
using Gum.Renderables;
using RenderingLibrary;
using Color = Raylib_cs.Color;
using ColorExtensions = RaylibGum.Helpers.ColorExtensions;
#elif SKIA
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

#if RAYLIB
/// <summary>
/// Raylib branch of <c>ArcRuntime</c>. Wraps the <see cref="LineArc"/> renderable added in
/// issue #2866 and exposes the cross-backend arc surface (StartAngle, SweepAngle, Thickness,
/// IsEndRounded, dashed strokes, dropshadow) so cross-platform sample code reads the same
/// against Skia, Apos.Shapes, and raylib. Single-slot composition — arcs have no fill mode on
/// any backend (sealed in PR #2891).
/// </summary>
/// <remarks>
/// Gradients on arcs are intentionally not exposed in this pass (deferred follow-up to #2866)
/// — see <see cref="LineArc"/> for the renderable-side rationale. <see cref="StrokeColor"/> is
/// the single user-facing color slot, mirroring the post-PR-#2891 Skia <c>ArcRuntime</c>'s API.
/// </remarks>
public class ArcRuntime : GraphicalUiElement
{
    LineArc containedLineArc = null!;
    LineArc ContainedLineArc
    {
        get
        {
            if (containedLineArc == null)
            {
                containedLineArc = (LineArc)this.RenderableComponent!;
            }
            return containedLineArc;
        }
    }

    /// <inheritdoc cref="LineArc.StartAngle"/>
    public float StartAngle
    {
        get => ContainedLineArc.StartAngle;
        set
        {
            ContainedLineArc.StartAngle = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="LineArc.SweepAngle"/>
    public float SweepAngle
    {
        get => ContainedLineArc.SweepAngle;
        set
        {
            ContainedLineArc.SweepAngle = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="LineArc.IsEndRounded"/>
    /// <remarks>raylib has no native stroke-cap analog; <see cref="LineArc"/> synthesizes
    /// rounded caps with <c>DrawCircleSector</c> half-disks at each band endpoint (issue #2895)
    /// so this property is now visually live on raylib, on parity with Skia/Apos.</remarks>
    public bool IsEndRounded
    {
        get => ContainedLineArc.IsEndRounded;
        set
        {
            ContainedLineArc.IsEndRounded = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// Legacy single-color slot. Routes to the renderable's <see cref="LineArc.Color"/>; used as
    /// the stroke color when <see cref="StrokeColor"/> is <c>null</c>. Matches the cross-backend
    /// "Color is the legacy slot, StrokeColor is the explicit modern slot" pattern that
    /// <see cref="CircleRuntime"/> and <see cref="RectangleRuntime"/> use on raylib.
    /// </summary>
    public Color Color
    {
        get => ContainedLineArc.Color;
        set
        {
            ContainedLineArc.Color = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="LineArc.StrokeColor"/>
    public Color? StrokeColor
    {
        get => ContainedLineArc.StrokeColor;
        set
        {
            ContainedLineArc.StrokeColor = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>Alpha channel of the legacy <see cref="Color"/> slot.</summary>
    public int Alpha
    {
        get => ContainedLineArc.Color.A;
        set
        {
            ContainedLineArc.Color = ColorExtensions.WithAlpha(ContainedLineArc.Color, (byte)value);
            NotifyPropertyChanged();
        }
    }

    /// <summary>Red channel of the legacy <see cref="Color"/> slot.</summary>
    public int Red
    {
        get => ContainedLineArc.Color.R;
        set
        {
            ContainedLineArc.Color = ColorExtensions.WithRed(ContainedLineArc.Color, (byte)value);
            NotifyPropertyChanged();
        }
    }

    /// <summary>Green channel of the legacy <see cref="Color"/> slot.</summary>
    public int Green
    {
        get => ContainedLineArc.Color.G;
        set
        {
            ContainedLineArc.Color = ColorExtensions.WithGreen(ContainedLineArc.Color, (byte)value);
            NotifyPropertyChanged();
        }
    }

    /// <summary>Blue channel of the legacy <see cref="Color"/> slot.</summary>
    public int Blue
    {
        get => ContainedLineArc.Color.B;
        set
        {
            ContainedLineArc.Color = ColorExtensions.WithBlue(ContainedLineArc.Color, (byte)value);
            NotifyPropertyChanged();
        }
    }

    float _thickness;

    /// <summary>
    /// Width of the stroked band along the arc curve, in <see cref="StrokeWidthUnits"/>.
    /// <c>Thickness</c> is the canonical user-facing name on arcs and the variable the Gum
    /// tool persists in <c>.gumx</c>; see the Skia/Apos branch of this class for the full
    /// visual-progression docs (thin → fat → wedge at <c>Width/2</c>).
    /// </summary>
    public float Thickness
    {
        get => _thickness;
        set
        {
            _thickness = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// Obsolete alias for <see cref="Thickness"/> on raylib too, matching the Skia/Apos branch.
    /// Same backing field; the obsolete marker is purely a steering hint toward the canonical
    /// user-facing name.
    /// </summary>
    [Obsolete("Use Thickness instead — it's the canonical user-facing name for arc stroke weight and the variable name the Gum tool persists. StrokeWidth still works (same backing field) but new code should prefer Thickness.")]
    public float StrokeWidth
    {
        get => _thickness;
        set
        {
            _thickness = value;
            NotifyPropertyChanged();
        }
    }

    DimensionUnitType _strokeWidthUnits;

    /// <inheritdoc cref="CircleRuntime.StrokeWidthUnits"/>
    public DimensionUnitType StrokeWidthUnits
    {
        get => _strokeWidthUnits;
        set
        {
            _strokeWidthUnits = value;
            NotifyPropertyChanged();
        }
    }

    float _strokeDashLength;

    /// <inheritdoc cref="LineArc.StrokeDashLength"/>
    public float StrokeDashLength
    {
        get => _strokeDashLength;
        set
        {
            _strokeDashLength = value;
            ContainedLineArc.StrokeDashLength = value;
            NotifyPropertyChanged();
        }
    }

    float _strokeGapLength;

    /// <inheritdoc cref="LineArc.StrokeGapLength"/>
    public float StrokeGapLength
    {
        get => _strokeGapLength;
        set
        {
            _strokeGapLength = value;
            ContainedLineArc.StrokeGapLength = value;
            NotifyPropertyChanged();
        }
    }

    bool _hasDropshadow;

    /// <inheritdoc cref="LineArc.HasDropshadow"/>
    public bool HasDropshadow
    {
        get => _hasDropshadow;
        set
        {
            _hasDropshadow = value;
            ContainedLineArc.HasDropshadow = value;
            NotifyPropertyChanged();
        }
    }

    Color _dropshadowColor = new Color((byte)0, (byte)0, (byte)0, (byte)255);

    /// <inheritdoc cref="LineArc.DropshadowColor"/>
    public Color DropshadowColor
    {
        get => _dropshadowColor;
        set
        {
            _dropshadowColor = value;
            ContainedLineArc.DropshadowColor = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>Alpha channel of <see cref="DropshadowColor"/>.</summary>
    public int DropshadowAlpha
    {
        get => _dropshadowColor.A;
        set => DropshadowColor = new Color(_dropshadowColor.R, _dropshadowColor.G, _dropshadowColor.B, (byte)value);
    }

    /// <summary>Red channel of <see cref="DropshadowColor"/>.</summary>
    public int DropshadowRed
    {
        get => _dropshadowColor.R;
        set => DropshadowColor = new Color((byte)value, _dropshadowColor.G, _dropshadowColor.B, _dropshadowColor.A);
    }

    /// <summary>Green channel of <see cref="DropshadowColor"/>.</summary>
    public int DropshadowGreen
    {
        get => _dropshadowColor.G;
        set => DropshadowColor = new Color(_dropshadowColor.R, (byte)value, _dropshadowColor.B, _dropshadowColor.A);
    }

    /// <summary>Blue channel of <see cref="DropshadowColor"/>.</summary>
    public int DropshadowBlue
    {
        get => _dropshadowColor.B;
        set => DropshadowColor = new Color(_dropshadowColor.R, _dropshadowColor.G, (byte)value, _dropshadowColor.A);
    }

    float _dropshadowOffsetX;
    /// <inheritdoc cref="LineArc.DropshadowOffsetX"/>
    public float DropshadowOffsetX
    {
        get => _dropshadowOffsetX;
        set
        {
            _dropshadowOffsetX = value;
            ContainedLineArc.DropshadowOffsetX = value;
            NotifyPropertyChanged();
        }
    }

    float _dropshadowOffsetY;
    /// <inheritdoc cref="LineArc.DropshadowOffsetY"/>
    public float DropshadowOffsetY
    {
        get => _dropshadowOffsetY;
        set
        {
            _dropshadowOffsetY = value;
            ContainedLineArc.DropshadowOffsetY = value;
            NotifyPropertyChanged();
        }
    }

    float _dropshadowBlurX;
    /// <inheritdoc cref="LineArc.DropshadowBlurX"/>
    public float DropshadowBlurX
    {
        get => _dropshadowBlurX;
        set
        {
            _dropshadowBlurX = value;
            ContainedLineArc.DropshadowBlurX = value;
            NotifyPropertyChanged();
        }
    }

    float _dropshadowBlurY;
    /// <inheritdoc cref="LineArc.DropshadowBlurY"/>
    public float DropshadowBlurY
    {
        get => _dropshadowBlurY;
        set
        {
            _dropshadowBlurY = value;
            ContainedLineArc.DropshadowBlurY = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// Isotropic blur radius in pixels for the dropshadow. Convenience wrapper that pushes a
    /// single value to both <see cref="DropshadowBlurX"/> and <see cref="DropshadowBlurY"/>
    /// (#2949), mirroring industry convention (CSS <c>box-shadow</c>, Figma, Photoshop) where
    /// dropshadow blur is a single scalar. Reading returns the X axis.
    /// </summary>
    public float DropshadowBlur
    {
        get => DropshadowBlurX;
        set
        {
            DropshadowBlurX = value;
            DropshadowBlurY = value;
        }
    }

    /// <summary>
    /// Initializes a new raylib <c>ArcRuntime</c>. Constructor defaults mirror the Skia/Apos
    /// branch so cross-backend sample code starts from the same baseline (Width = Height = 100,
    /// SweepAngle = 90, Thickness = 10, Color = White, dropshadow off but pre-seeded with a
    /// 3 px Y offset/blur). Pass <c>fullInstantiation = false</c> only when constructing through
    /// deserialization.
    /// </summary>
    public ArcRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
            LineArc arc = new();
            SetContainedObject(arc);
            containedLineArc = arc;

            Width = 100;
            Height = 100;
            Thickness = 10;
            StartAngle = 0;
            SweepAngle = 90;

            // Pre-seed dropshadow offset/blur so toggling HasDropshadow = true at runtime
            // produces a visible shadow without further setup — same default the Skia/Apos
            // branch lands via the SkiaShapeRuntime/AposShapeRuntime constructor.
            DropshadowOffsetX = 0;
            DropshadowOffsetY = 3;
            DropshadowBlurX = 0;
            DropshadowBlurY = 3;
        }
    }

    /// <summary>
    /// Pushes the stroke thickness to the contained <see cref="LineArc"/> each frame, resolving
    /// <see cref="StrokeWidthUnits"/> against the current camera zoom so a ScreenPixel-unit
    /// thickness holds its on-screen pixel width regardless of zoom. Mirrors the raylib
    /// <see cref="CircleRuntime.PreRender"/> pattern.
    /// </summary>
    public override void PreRender()
    {
        float thickness = _thickness;
        if (_strokeWidthUnits == DimensionUnitType.ScreenPixel)
        {
            var camera = this.EffectiveManagers?.Renderer?.Camera;
            if (camera != null)
            {
                thickness /= camera.Zoom;
            }
        }
        ContainedLineArc.Thickness = thickness;
    }

    /// <inheritdoc/>
    public override GraphicalUiElement Clone()
    {
        ArcRuntime toReturn = (ArcRuntime)base.Clone();
        // Drop the cached renderable reference so the clone re-resolves it against its own
        // RenderableComponent on next access. Same pattern as the Skia/Apos branch's Clone.
        toReturn.containedLineArc = null!;
        return toReturn;
    }
}
#else
/// <summary>
/// Runtime that draws a circular arc inscribed in its Width x Height bounds. Arcs are always
/// stroked — there is no fill mode. Use <see cref="Thickness"/> to control how thick the band
/// is; thickness ranges from "thin line tracing the curve" all the way up to "pie wedge filling
/// the bounds" (see the <see cref="Thickness"/> docs for the wedge configuration).
/// </summary>
/// <remarks>
/// Source-shared between SkiaGum, MonoGameGumShapes / KniGumShapes, and the raylib runtime via a
/// Compile/Link in the consuming csprojs - see <c>RoundedRectangleRuntime</c> for the same
/// pattern. Platform differences are gated behind <c>#if SKIA</c> / <c>#if RAYLIB</c>; the raylib
/// branch is a separate class definition above this one because it inherits from a different
/// base (<c>GraphicalUiElement</c> directly, not <c>SkiaShapeRuntime</c>/<c>AposShapeRuntime</c>),
/// so the property surface differs. Dashed strokes are only rendered on Skia and raylib;
/// Apos.Shapes' Arc primitive lacks dashing, so on that backend <c>StrokeDashLength</c> is
/// exposed via the base class for API parity but no-ops.
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
    /// Width of the stroked band that traces the arc curve, in pixels. <c>Thickness</c> is the
    /// canonical user-facing name for this value on arcs — it's the variable name the Gum tool
    /// persists in <c>.gumx</c> projects and the one shown in the Variables grid. <see cref="StrokeWidth"/>
    /// is an alias for cross-shape code consumers and is marked obsolete on <c>ArcRuntime</c> to
    /// steer callers toward <c>Thickness</c>.
    /// </summary>
    /// <remarks>
    /// <para><b>Façade</b></para>
    /// <para>
    /// Routes to the same backing field as <see cref="StrokeWidth"/> on the base shape runtime.
    /// There is no second storage location; setting either name is byte-for-byte identical. The
    /// runtime holds the value (not the renderable) so <c>PreRender</c> can apply
    /// <c>StrokeWidthUnits</c> (e.g. ScreenPixel zoom scaling) each frame before pushing the
    /// resolved value to the renderable.
    /// </para>
    /// <para><b>Visual progression as Thickness grows</b></para>
    /// <para>
    /// Arcs render as a stroked band centered on a curve inscribed in the bounding box. The
    /// stroke is inset by half its width (<see cref="RenderableShapeBase.IsOffsetAppliedForStroke"/>),
    /// so the curve's radius shrinks as <c>Thickness</c> grows. For a square arc with
    /// <c>Width = Height = W</c>:
    /// </para>
    /// <list type="bullet">
    /// <item><b>Thin (e.g. Thickness = 2)</b>: a fine line tracing the curve. Use for outlined rings.</item>
    /// <item><b>Medium (e.g. Thickness = W / 8)</b>: the chunky band most progress-ring / dial UIs want.</item>
    /// <item><b>Thickness = W / 2</b>: the band's inner edge collapses to a point at center and
    /// the butt-end caps become radial edges — the arc renders as a true pie wedge (a circular
    /// sector) filling the bounding box. This is the supported path to a "filled wedge" arc.
    /// Set <see cref="IsEndRounded"/> = false (the default) for a clean wedge; rounded caps
    /// distort the wedge corners.</item>
    /// <item><b>Thickness &gt; W / 2</b>: undefined. The internal curve would have negative
    /// radius; Skia/Apos behavior in that regime is not specified. Clamp at <c>W / 2</c>.</item>
    /// </list>
    /// <para><b>Gradient interaction</b></para>
    /// <para>
    /// Setting <see cref="SkiaShapeRuntime.UseGradient"/> = true (after seeding gradient colors
    /// and endpoints) applies the gradient to the stroke band at any Thickness — including at
    /// <c>Thickness = W / 2</c>, which produces a gradient-filled pie wedge.
    /// </para>
    /// </remarks>
    public float Thickness
    {
        get => base.StrokeWidth;
        set => base.StrokeWidth = value;
    }

    /// <summary>
    /// Obsolete on <c>ArcRuntime</c> — use <see cref="Thickness"/>. Both properties route to the
    /// same backing field, so existing code using <c>StrokeWidth</c> still works; the obsolete
    /// marker exists to point new callers at the canonical user-facing name. The Gum tool
    /// persists this value as "Thickness" in <c>.gumx</c> projects and surfaces only that name
    /// in the Variables grid; <c>StrokeWidth</c> is a cross-shape technical alias inherited
    /// from the base shape runtime.
    /// </summary>
    [Obsolete("Use Thickness instead — it's the canonical user-facing name for arc stroke weight and the variable name the Gum tool persists. StrokeWidth still works (same backing field) but new code should prefer Thickness.")]
    public new float StrokeWidth
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
    /// Isotropic blur radius in pixels for the dropshadow. Convenience wrapper that pushes a
    /// single value to both the inherited <see cref="SkiaShapeRuntime.DropshadowBlurX"/> and
    /// <see cref="SkiaShapeRuntime.DropshadowBlurY"/> (#2949). Skia natively supports per-axis
    /// blur, but the user-facing arc surface mirrors industry convention (CSS <c>box-shadow</c>,
    /// Figma, Photoshop) where blur is a single scalar. Reading returns the X axis.
    /// </summary>
    public float DropshadowBlur
    {
        get => DropshadowBlurX;
        set
        {
            DropshadowBlurX = value;
            DropshadowBlurY = value;
        }
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

            // Seed the runtime auto-property so PreRender pushes the legacy Arc default (10) to
            // the renderable instead of overwriting it with 0. Thickness === StrokeWidth (same
            // backing field); using Thickness here matches the canonical user-facing name.
            Thickness = 10;

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
#endif
