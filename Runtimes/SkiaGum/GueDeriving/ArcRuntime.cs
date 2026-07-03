using Gum.Wireframe;
using System;

#if RAYLIB
using Gum.DataTypes;
using Gum.Renderables;
using RenderingLibrary;
using RenderingLibrary.Graphics;
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
/// Issue #3454 — gradients are now exposed on raylib (<see cref="UseGradient"/>), matching the
/// Skia/Apos branch. Per the #3009 model the gradient START is the arc's primary
/// <see cref="Color"/> (synced into the renderable's <c>Color1</c> in <see cref="PreRender"/>);
/// <see cref="Color2"/> is the standalone second stop. <see cref="StrokeColor"/> remains the
/// explicit stroke-color slot for the solid (non-gradient) path.
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
    /// <summary>
    /// Deprecated on <c>ArcRuntime</c> — use <see cref="DropshadowBlur"/> (scalar). Kept
    /// functional for back-compat; the arc surface is moving to a single isotropic blur.
    /// </summary>
    [Obsolete("Use DropshadowBlur (scalar). Per-axis blur is deprecated on ArcRuntime; the arc dropshadow blur is a single isotropic value.")]
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
    /// <inheritdoc cref="DropshadowBlurX"/>
    [Obsolete("Use DropshadowBlur (scalar). Per-axis blur is deprecated on ArcRuntime; the arc dropshadow blur is a single isotropic value.")]
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
    /// Isotropic blur radius in pixels for the dropshadow. The user-facing arc surface mirrors
    /// industry convention (CSS <c>box-shadow</c>, Figma, Photoshop) where dropshadow blur is a
    /// single scalar (#2949). Pushes the value to both per-axis fields (deprecated on this
    /// surface) and the contained <see cref="LineArc"/>. Reading returns the X axis.
    /// </summary>
    public float DropshadowBlur
    {
        get => _dropshadowBlurX;
        set
        {
            _dropshadowBlurX = value;
            _dropshadowBlurY = value;
            ContainedLineArc.DropshadowBlurX = value;
            ContainedLineArc.DropshadowBlurY = value;
            NotifyPropertyChanged();
        }
    }

    // Issue #3454 — gradient surface, forwarded to the contained LineArc. The gradient START color
    // is not a standalone slot: per the #3009 model it mirrors the arc's primary Color, synced into
    // the renderable's Color1 in PreRender. Color2 below is the standalone second stop. The obsolete
    // Color1 / Red1 / Green1 / Blue1 / Alpha1 shims that the Skia/Apos branch carries are
    // intentionally NOT widened to raylib — gradient support never shipped on raylib arcs, so there
    // is no legacy data to preserve, and the cross-platform guidance is that the narrower footprint
    // wins for deprecated members (see the gum-cross-platform-unification skill).

    /// <inheritdoc cref="LineArc.UseGradient"/>
    public bool UseGradient
    {
        get => ContainedLineArc.UseGradient;
        set
        {
            ContainedLineArc.UseGradient = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="LineArc.GradientType"/>
    public GradientType GradientType
    {
        get => ContainedLineArc.GradientType;
        set
        {
            ContainedLineArc.GradientType = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="LineArc.Color2"/>
    public Color Color2
    {
        get => ContainedLineArc.Color2;
        set
        {
            ContainedLineArc.Color2 = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>Red channel of the gradient's second stop, <see cref="Color2"/>.</summary>
    public int Red2
    {
        get => ContainedLineArc.Color2.R;
        set
        {
            ContainedLineArc.Color2 = ColorExtensions.WithRed(ContainedLineArc.Color2, (byte)value);
            NotifyPropertyChanged();
        }
    }

    /// <summary>Green channel of the gradient's second stop, <see cref="Color2"/>.</summary>
    public int Green2
    {
        get => ContainedLineArc.Color2.G;
        set
        {
            ContainedLineArc.Color2 = ColorExtensions.WithGreen(ContainedLineArc.Color2, (byte)value);
            NotifyPropertyChanged();
        }
    }

    /// <summary>Blue channel of the gradient's second stop, <see cref="Color2"/>.</summary>
    public int Blue2
    {
        get => ContainedLineArc.Color2.B;
        set
        {
            ContainedLineArc.Color2 = ColorExtensions.WithBlue(ContainedLineArc.Color2, (byte)value);
            NotifyPropertyChanged();
        }
    }

    /// <summary>Alpha channel of the gradient's second stop, <see cref="Color2"/>.</summary>
    public int Alpha2
    {
        get => ContainedLineArc.Color2.A;
        set
        {
            ContainedLineArc.Color2 = ColorExtensions.WithAlpha(ContainedLineArc.Color2, (byte)value);
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="LineArc.GradientX1"/>
    public float GradientX1
    {
        get => ContainedLineArc.GradientX1;
        set
        {
            ContainedLineArc.GradientX1 = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="LineArc.GradientY1"/>
    public float GradientY1
    {
        get => ContainedLineArc.GradientY1;
        set
        {
            ContainedLineArc.GradientY1 = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="LineArc.GradientX2"/>
    public float GradientX2
    {
        get => ContainedLineArc.GradientX2;
        set
        {
            ContainedLineArc.GradientX2 = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="LineArc.GradientY2"/>
    public float GradientY2
    {
        get => ContainedLineArc.GradientY2;
        set
        {
            ContainedLineArc.GradientY2 = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="LineArc.GradientInnerRadius"/>
    public float GradientInnerRadius
    {
        get => ContainedLineArc.GradientInnerRadius;
        set
        {
            ContainedLineArc.GradientInnerRadius = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="LineArc.GradientOuterRadius"/>
    public float GradientOuterRadius
    {
        get => ContainedLineArc.GradientOuterRadius;
        set
        {
            ContainedLineArc.GradientOuterRadius = value;
            NotifyPropertyChanged();
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

            // Issue #3454 — seed the gradient second stop + axis to match the Skia/Apos branch's
            // ctor so cross-backend sample code starts from the same baseline (yellow second stop,
            // axis running to the bottom-right corner of the default 100x100 bounds). The gradient
            // start follows the primary Color (White) via PreRender; UseGradient defaults off.
            Red2 = 255;
            Green2 = 255;
            Blue2 = 0;
            GradientX2 = 100;
            GradientY2 = 100;

            // Pre-seed dropshadow offset/blur so toggling HasDropshadow = true at runtime
            // produces a visible shadow without further setup — same default the Skia/Apos
            // branch lands via the SkiaShapeRuntime/AposShapeRuntime constructor. Anisotropic
            // seed (X = 0, Y = 3) is intentional; per-axis is deprecated but DropshadowBlur
            // (scalar) can't express the asymmetry.
            DropshadowOffsetX = 0;
            DropshadowOffsetY = 3;
#pragma warning disable CS0618
            DropshadowBlurX = 0;
            DropshadowBlurY = 3;
#pragma warning restore CS0618
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
        // Issue #3183 — raylib shape strokes get NO AA compensation: the geometric width IS the
        // visible width (MSAA adds none). The arc band is a DrawRing filled annulus already at the
        // nominal Thickness, so the band width is pushed straight through. See
        // RectangleRuntime.PreRender for the full rationale.
        ContainedLineArc.Thickness = thickness;

        // Issue #3454 / #3009 — the gradient START is the arc's primary body Color, not a standalone
        // slot. Mirror it into the renderable's Color1 each frame so the start stop follows the body
        // regardless of how Color was set (state change, animation, .gumx load). Matches the Skia/Apos
        // branch's PreRender, which syncs ContainedArc.Red1/Green1/Blue1/Alpha1 from the body color.
        ContainedLineArc.Color1 = ContainedLineArc.Color;
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
    /// Isotropic blur radius in pixels for the dropshadow. The user-facing arc surface mirrors
    /// industry convention (CSS <c>box-shadow</c>, Figma, Photoshop) where blur is a single
    /// scalar (#2949); this pushes the value to both inherited per-axis fields. Skia natively
    /// supports per-axis blur, so the per-axis members remain functional but are deprecated on
    /// the arc surface (see below). Reading returns the X axis.
    /// </summary>
    public float DropshadowBlur
    {
        get => base.DropshadowBlurX;
        set
        {
            base.DropshadowBlurX = value;
            base.DropshadowBlurY = value;
        }
    }

    /// <summary>
    /// Deprecated on <c>ArcRuntime</c> — use <see cref="DropshadowBlur"/> (scalar). Kept
    /// functional (forwards to the inherited per-axis blur) for back-compat; the arc surface is
    /// moving to a single isotropic blur. Per-axis blur stays as the real API on the legacy Skia
    /// shapes (<c>RoundedRectangleRuntime</c> / <c>ColoredCircleRuntime</c>).
    /// </summary>
    [Obsolete("Use DropshadowBlur (scalar). Per-axis blur is deprecated on ArcRuntime; the arc dropshadow blur is a single isotropic value.")]
    public new float DropshadowBlurX
    {
        get => base.DropshadowBlurX;
        set => base.DropshadowBlurX = value;
    }

    /// <inheritdoc cref="DropshadowBlurX"/>
    [Obsolete("Use DropshadowBlur (scalar). Per-axis blur is deprecated on ArcRuntime; the arc dropshadow blur is a single isotropic value.")]
    public new float DropshadowBlurY
    {
        get => base.DropshadowBlurY;
        set => base.DropshadowBlurY = value;
    }

    // Issue #3009 — Arc's gradient START stop is now its primary Color (the unified "gradient start
    // = body color" model that Circle/Rectangle adopt via SyncGradientStartToBody). The legacy
    // standalone gradient-start surface (Color1 / Red1 / Green1 / Blue1 / Alpha1) is kept only as
    // [Obsolete] back-compat shims that map onto the primary Color, so old code compiles and old
    // .gumx data — which set these channels independently — keeps loading (see
    // CustomSetPropertyOnRenderable and the migration loss-case analysis on issue #3009). The
    // obsoletes are slated for removal ~Nov 2026, after which Color1 leaves Arc entirely. The
    // renderable's gradient-start channels are kept in lockstep with the primary color in PreRender.

    /// <summary>Obsolete: Arc's gradient start is its primary <c>Color</c>; this maps onto Color for back-compat. See issue #3009.</summary>
    [Obsolete("Arc's gradient start is now its primary Color — set Color (or Red/Green/Blue/Alpha) instead. Color1 maps onto Color for backward compatibility and will be removed ~Nov 2026. See issue #3009.")]
#if SKIA
    public new SKColor Color1
    {
        get => new SKColor((byte)Red, (byte)Green, (byte)Blue, (byte)Alpha);
        set { Red = value.Red; Green = value.Green; Blue = value.Blue; Alpha = value.Alpha; }
    }
#else
    public new Microsoft.Xna.Framework.Color Color1
    {
        get => new Microsoft.Xna.Framework.Color(Red, Green, Blue, Alpha);
        set { Red = value.R; Green = value.G; Blue = value.B; Alpha = value.A; }
    }
#endif

    /// <inheritdoc cref="Color1"/>
    [Obsolete("Arc's gradient start is now its primary Color — set Color (or Red) instead. Red1 maps onto Color for backward compatibility and will be removed ~Nov 2026. See issue #3009.")]
    public new int Red1 { get => Red; set => Red = value; }

    /// <inheritdoc cref="Color1"/>
    [Obsolete("Arc's gradient start is now its primary Color — set Color (or Green) instead. Green1 maps onto Color for backward compatibility and will be removed ~Nov 2026. See issue #3009.")]
    public new int Green1 { get => Green; set => Green = value; }

    /// <inheritdoc cref="Color1"/>
    [Obsolete("Arc's gradient start is now its primary Color — set Color (or Blue) instead. Blue1 maps onto Color for backward compatibility and will be removed ~Nov 2026. See issue #3009.")]
    public new int Blue1 { get => Blue; set => Blue = value; }

    /// <inheritdoc cref="Color1"/>
    [Obsolete("Arc's gradient start is now its primary Color — set Color (or Alpha) instead. Alpha1 maps onto Color for backward compatibility and will be removed ~Nov 2026. See issue #3009.")]
    public new int Alpha1 { get => Alpha; set => Alpha = value; }

    /// <inheritdoc/>
    public override void PreRender()
    {
        base.PreRender();
        // Issue #3009 — mirror the primary Color into the renderable's gradient-start channels each
        // frame so the gradient start follows the body color regardless of how it was set (legacy
        // Color1/Red1 shims, state changes, animation, or .gumx load via CustomSetPropertyOnRenderable).
        ContainedArc.Red1 = ContainedArc.Red;
        ContainedArc.Green1 = ContainedArc.Green;
        ContainedArc.Blue1 = ContainedArc.Blue;
        ContainedArc.Alpha1 = ContainedArc.Alpha;
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

            // Issue #3009 — the gradient start is the primary Color (White, set above) and is
            // synced into the renderable's start channels in PreRender, so the old explicit
            // Red1/Green1/Blue1 = 255 seed is no longer needed. Color2 (the second stop) remains
            // an independent standalone gradient color.
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
            // Anisotropic seed (X = 0, Y = 3) is intentional — the arc's default shadow blurs
            // only vertically. Per-axis is deprecated on the public surface but kept here for
            // that default; DropshadowBlur (scalar) can't express the asymmetry.
#pragma warning disable CS0618
            DropshadowBlurX = 0;
            DropshadowBlurY = 3;
#pragma warning restore CS0618
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
