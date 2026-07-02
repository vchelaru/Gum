using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;

#if XNALIKE
using Gum.Converters;
#endif

#if RAYLIB
using Gum.Converters;
using Gum.DataTypes;
using Gum.Renderables;
using Color = Raylib_cs.Color;
using ColorExtensions = RaylibGum.Helpers.ColorExtensions;
using ContainedLineRectangle = Gum.Renderables.LineRectangle;
#elif SOKOL
using Gum.DataTypes;
using Gum.Renderables;
using Color = SokolGum.Color;
using ContainedLineRectangle = Gum.Renderables.LineRectangle;
#elif SKIA
using Gum.DataTypes;
using SkiaGum.Renderables;
using SkiaSharp;
using Color = SkiaSharp.SKColor;
// Issue #2818 — Skia uses RoundedRectangle as the contained type so CornerRadius / per-corner
// radii reach the renderer. With CornerRadius left at 0 the visual matches a hard-cornered
// LineRectangle.
using ContainedLineRectangle = SkiaGum.Renderables.RoundedRectangle;
#else
using Color = Microsoft.Xna.Framework.Color;
using ColorExtensions = ToolsUtilitiesStandard.Helpers.ColorExtensions;
using ContainedLineRectangle = global::RenderingLibrary.Math.Geometry.LineRectangle;
using global::RenderingLibrary.Math.Geometry;
using Gum.DataTypes;
using Gum.Renderables;
#endif

#if FRB
namespace MonoGameGum.GueDeriving;
#else
namespace Gum.GueDeriving;
#endif

/// <summary>
/// Runtime wrapping the fill + stroke renderable pair that draws a rectangle. Mirrors
/// <see cref="CircleRuntime"/>'s two-slot model (issue #2768): under XNA-likes both slots are
/// resolved once at construction via <see cref="RenderableRegistry"/> and kept for life.
/// </summary>
/// <remarks>
/// Core MonoGameGum ships defaults for both slots — <see cref="DefaultFilledRectangleRenderable"/>
/// (wraps <c>SolidRectangle</c>) and <see cref="DefaultStrokedRectangleRenderable"/> (wraps
/// <c>LineRectangle</c>) — so fill and stroke both work
/// without the optional MonoGameGumShapes package. <see cref="CornerRadius"/> is stored on
/// the defaults but not rendered; install MonoGameGumShapes for rounded corners. Backends
/// other than XNA-like are still on the single <c>LineRectangle</c> model.
/// </remarks>
#if SKIA
public class RectangleRuntime : SkiaShapeRuntime
#else
public class RectangleRuntime : GraphicalUiElement
#endif
{
#if XNALIKE
    IFilledRectangleRenderable? _fill;
    IStrokedRectangleRenderable _stroke = null!;
#else
    ContainedLineRectangle containedLineRectangle = null!;
    ContainedLineRectangle ContainedLineRectangle
    {
        get
        {
            if (containedLineRectangle == null)
            {
                containedLineRectangle = (ContainedLineRectangle)this.RenderableComponent!;
            }
            return containedLineRectangle;
        }
    }
#endif

#if !SKIA
    /// <summary>
    /// Per-platform routing for the obsolete <see cref="LineWidth"/> surface: XNALIKE writes
    /// through the stroke renderable's slot, everything else (Raylib/Sokol) writes through
    /// <see cref="ContainedLineRectangle"/>.
    /// </summary>
    float ObsoleteStrokeWidth
    {
#if XNALIKE
        get => _stroke.StrokeWidth;
        set => _stroke.StrokeWidth = value;
#else
        get => ContainedLineRectangle.LinePixelWidth;
        set => ContainedLineRectangle.LinePixelWidth = value;
#endif
    }

    /// <summary>
    /// Obsolete: use <see cref="StrokeWidth"/>. Legacy pre-collapse setter that writes the
    /// stroke renderable's stroke width directly, bypassing <see cref="StrokeWidthUnits"/>.
    /// </summary>
    // SKIA: SkiaShapeRuntime.StrokeWidth supersedes; #2814.
    [Obsolete("Renamed to StrokeWidth in #2757/#2768 for cross-backend naming parity. Functional behavior is unchanged; switch to StrokeWidth to also pick up StrokeWidthUnits scaling.")]
    public float LineWidth
    {
        get => ObsoleteStrokeWidth;
        set
        {
            ObsoleteStrokeWidth = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// Per-platform routing for the obsolete <see cref="IsDotted"/> surface. XNALIKE keeps the
    /// <c>_stroke is LineRectangle</c> check because the Apos.Shapes stroke slot doesn't always
    /// resolve to the core <see cref="LineRectangle"/> type; everything else routes through
    /// <see cref="ContainedLineRectangle"/>.
    /// </summary>
    bool ObsoleteIsDotted
    {
#if XNALIKE
        get => _stroke is LineRectangle lr && lr.IsDotted;
        set
        {
            if (_stroke is LineRectangle lr)
            {
                lr.IsDotted = value;
            }
        }
#else
        get => ContainedLineRectangle.IsDotted;
        set => ContainedLineRectangle.IsDotted = value;
#endif
    }

    /// <summary>
    /// Obsolete: superseded by the <see cref="StrokeDashLength"/> / <see cref="StrokeGapLength"/>
    /// pair for cross-backend naming parity. On MG/Raylib the underlying <c>LineRectangle</c>
    /// only has a binary dotted texture; on XNALIKE Apos.Shapes adds true per-segment dashes
    /// through the stroke slot, on Skia <c>SKPathEffect.CreateDash</c> consumes the lengths
    /// verbatim.
    /// </summary>
    [Obsolete("Renamed to StrokeDashLength + StrokeGapLength in #2757/#2768 for cross-backend parity. With the optional MonoGameGumShapes package the lengths drive true per-segment dashes; without it the core LineRectangle stroke shows the binary dotted texture.")]
    public bool IsDotted
    {
        get => ObsoleteIsDotted;
        set
        {
            ObsoleteIsDotted = value;
            NotifyPropertyChanged();
        }
    }
#endif

#if RAYLIB || SOKOL
    float _strokeWidth = 1;

    /// <inheritdoc cref="CircleRuntime.StrokeWidth"/>
    public float StrokeWidth
    {
        get => _strokeWidth;
        set
        {
            _strokeWidth = value;
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

    /// <inheritdoc cref="PolygonRuntime.StrokeDashLength"/>
    public float StrokeDashLength
    {
        get => _strokeDashLength;
        set
        {
            _strokeDashLength = value;
#if RAYLIB
            // #2757: previously the raylib branch only flipped the binary IsDotted flag on the
            // renderable. The LineRectangle now exposes real StrokeDashLength / StrokeGapLength
            // for a perimeter-walk dashed path, so push the actual length through. SOKOL
            // renderable has no equivalent yet — values round-trip on the runtime as forward
            // compat (same pattern as CircleRuntime's RAYLIB || SOKOL block).
            ContainedLineRectangle.StrokeDashLength = value;
#endif
            NotifyPropertyChanged();
        }
    }

    float _strokeGapLength;

    /// <inheritdoc cref="PolygonRuntime.StrokeGapLength"/>
    public float StrokeGapLength
    {
        get => _strokeGapLength;
        set
        {
            _strokeGapLength = value;
#if RAYLIB
            ContainedLineRectangle.StrokeGapLength = value;
#endif
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// Pushes the stroke width to the contained <c>LineRectangle</c> each frame, resolving
    /// <see cref="StrokeWidthUnits"/> against the current camera zoom so a ScreenPixel stroke
    /// holds its on-screen pixel width regardless of zoom. Mirrors the XNALIKE
    /// <see cref="PreRender"/> below and the Polygon/Circle equivalents (#2757).
    /// </summary>
    public override void PreRender()
    {
        var camera = this.EffectiveManagers?.Renderer?.Camera;
        float cameraZoom = camera?.Zoom ?? 1f;

        float strokeWidth = _strokeWidth;
        if (_strokeWidthUnits == DimensionUnitType.ScreenPixel && camera != null)
        {
            strokeWidth /= cameraZoom;
        }

        // Issue #3183 — raylib gets NO AA compensation (unlike the Apos/XNALIKE PreRender below).
        // Apos.Shapes subtracts a ~1px geometric contribution but adds it back as a ~1px
        // antialiasing band, so its VISIBLE stroke width is preserved. raylib's MSAA only smooths
        // the edges of the geometry it is handed — it adds no width — so the geometric width IS the
        // visible width. Pushing StrokeWidth straight through matches the Apos reference pixel-for-
        // pixel across stroke widths (verified by the shape-parity screenshot harness). A prior
        // attempt (#3179) mirrored Apos's subtraction here without Apos's add-back, which collapsed
        // a nominal 1px outline to ~0.01px — invisible. StrokeWidth <= 0 still pushes literal 0 so
        // LineRectangle's positive-width gate suppresses the stroke (the fill-only-shape fix above).
        ContainedLineRectangle.LinePixelWidth = strokeWidth <= 0 ? 0f : strokeWidth;
    }
#endif

#if RAYLIB
    // Issue #2757 — raylib rectangle parity: surface the same property names the XNALIKE/SKIA
    // branches expose so the cross-backend RectanglesScreen sample compiles against raylib. The
    // setters push through to the contained LineRectangle, whose Render() handles fill/stroke/
    // gradient/dropshadow composition. SOKOL is not extended here yet — its renderable doesn't
    // implement any of this; when it gains support, lift the relevant blocks into RAYLIB ||
    // SOKOL (mirror the CircleRuntime pattern).

    // FillColor defaults to opaque white while IsFilled defaults to false, so a freshly-
    // constructed RectangleRuntime renders as a stroke-only outline (the white fill is gated
    // off). Flipping IsFilled = true fills it white without needing to assign FillColor.
    Color _fillColor = new Color((byte)255, (byte)255, (byte)255, (byte)255);

    /// <inheritdoc cref="Gum.Renderables.LineRectangle.FillColor"/>
    /// <remarks>
    /// Issue #2938 — non-nullable since the visibility gate moved to <see cref="IsFilled"/>.
    /// Defaults to opaque white; IsFilled is false by default so a fresh runtime is a
    /// stroke-only outline and flipping IsFilled on alone yields a visible white fill.
    /// </remarks>
    public Color FillColor
    {
        get => _fillColor;
        set
        {
            _fillColor = value;
            PushFillColorToRenderable();
            SyncGradientStart();
            NotifyPropertyChanged();
        }
    }

    // Issue #3009 follow-up — gate the renderable's fill color by IsFilled so a stroke-only
    // rectangle (IsFilled = false) leaves the renderable's FillColor null and its fill pass (which
    // runs when FillColor.HasValue || IsFilled) stays off — a fresh rectangle is outline-only.
    // Mirrors how the Apos/Skia two-slot model pushes a transparent fill when the shape isn't
    // filled; previously the default opaque-white FillColor was pushed unconditionally, filling
    // every default rectangle.
    void PushFillColorToRenderable()
    {
        ContainedLineRectangle.FillColor = ContainedLineRectangle.IsFilled ? (Color?)_fillColor : null;
    }

    /// <summary>Red channel of <see cref="FillColor"/>.</summary>
    public int FillRed
    {
        get => _fillColor.R;
        set => FillColor = new Color((byte)value, _fillColor.G, _fillColor.B, _fillColor.A);
    }

    /// <summary>Green channel of <see cref="FillColor"/>.</summary>
    public int FillGreen
    {
        get => _fillColor.G;
        set => FillColor = new Color(_fillColor.R, (byte)value, _fillColor.B, _fillColor.A);
    }

    /// <summary>Blue channel of <see cref="FillColor"/>.</summary>
    public int FillBlue
    {
        get => _fillColor.B;
        set => FillColor = new Color(_fillColor.R, _fillColor.G, (byte)value, _fillColor.A);
    }

    /// <summary>Alpha channel of <see cref="FillColor"/>.</summary>
    public int FillAlpha
    {
        get => _fillColor.A;
        set => FillColor = new Color(_fillColor.R, _fillColor.G, _fillColor.B, (byte)value);
    }

    Color _strokeColor = new Color((byte)255, (byte)255, (byte)255, (byte)255);

    /// <inheritdoc cref="Gum.Renderables.LineRectangle.StrokeColor"/>
    public Color StrokeColor
    {
        get => _strokeColor;
        set
        {
            _strokeColor = value;
            ContainedLineRectangle.StrokeColor = value;
            SyncGradientStart();
            NotifyPropertyChanged();
        }
    }

    /// <summary>Red channel of <see cref="StrokeColor"/>.</summary>
    public int StrokeRed
    {
        get => _strokeColor.R;
        set => StrokeColor = new Color((byte)value, _strokeColor.G, _strokeColor.B, _strokeColor.A);
    }

    /// <summary>Green channel of <see cref="StrokeColor"/>.</summary>
    public int StrokeGreen
    {
        get => _strokeColor.G;
        set => StrokeColor = new Color(_strokeColor.R, (byte)value, _strokeColor.B, _strokeColor.A);
    }

    /// <summary>Blue channel of <see cref="StrokeColor"/>.</summary>
    public int StrokeBlue
    {
        get => _strokeColor.B;
        set => StrokeColor = new Color(_strokeColor.R, _strokeColor.G, (byte)value, _strokeColor.A);
    }

    /// <summary>Alpha channel of <see cref="StrokeColor"/>.</summary>
    public int StrokeAlpha
    {
        get => _strokeColor.A;
        set => StrokeColor = new Color(_strokeColor.R, _strokeColor.G, _strokeColor.B, (byte)value);
    }

    /// <inheritdoc cref="Gum.Renderables.LineRectangle.IsFilled"/>
    public bool IsFilled
    {
        get => ContainedLineRectangle.IsFilled;
        set
        {
            ContainedLineRectangle.IsFilled = value;
            // Issue #3009 — re-gate the renderable fill color and re-route the gradient start to
            // the now-active body color.
            PushFillColorToRenderable();
            SyncGradientStart();
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="Gum.Renderables.LineRectangle.CornerRadius"/>
    public float CornerRadius
    {
        get => ContainedLineRectangle.CornerRadius;
        set
        {
            ContainedLineRectangle.CornerRadius = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="Gum.Renderables.LineRectangle.UseGradient"/>
    public bool UseGradient
    {
        get => ContainedLineRectangle.UseGradient;
        set
        {
            ContainedLineRectangle.UseGradient = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="Gum.Renderables.LineRectangle.GradientType"/>
    public GradientType GradientType
    {
        get => ContainedLineRectangle.GradientType;
        set
        {
            ContainedLineRectangle.GradientType = value;
            NotifyPropertyChanged();
        }
    }

    // Issue #3009 — Circle/Rectangle no longer expose a standalone gradient Color1. The gradient
    // start mirrors the active body color (FillColor when filled, StrokeColor otherwise), synced
    // from the FillColor / StrokeColor / IsFilled setters into the renderable's Color1.
    void SyncGradientStart()
    {
        ContainedLineRectangle.Color1 = ContainedLineRectangle.IsFilled ? _fillColor : _strokeColor;
    }

    /// <inheritdoc cref="Gum.Renderables.LineRectangle.Color2"/>
    public Color Color2
    {
        get => ContainedLineRectangle.Color2;
        set
        {
            ContainedLineRectangle.Color2 = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="Gum.Renderables.LineRectangle.GradientX1"/>
    public float GradientX1
    {
        get => ContainedLineRectangle.GradientX1;
        set
        {
            ContainedLineRectangle.GradientX1 = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="Gum.Renderables.LineRectangle.GradientY1"/>
    public float GradientY1
    {
        get => ContainedLineRectangle.GradientY1;
        set
        {
            ContainedLineRectangle.GradientY1 = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="Gum.Renderables.LineRectangle.GradientX2"/>
    public float GradientX2
    {
        get => ContainedLineRectangle.GradientX2;
        set
        {
            ContainedLineRectangle.GradientX2 = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="Gum.Renderables.LineRectangle.GradientY2"/>
    public float GradientY2
    {
        get => ContainedLineRectangle.GradientY2;
        set
        {
            ContainedLineRectangle.GradientY2 = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="Gum.Renderables.LineRectangle.GradientInnerRadius"/>
    public float GradientInnerRadius
    {
        get => ContainedLineRectangle.GradientInnerRadius;
        set
        {
            ContainedLineRectangle.GradientInnerRadius = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="Gum.Renderables.LineRectangle.GradientOuterRadius"/>
    public float GradientOuterRadius
    {
        get => ContainedLineRectangle.GradientOuterRadius;
        set
        {
            ContainedLineRectangle.GradientOuterRadius = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="Gum.Renderables.LineRectangle.HasDropshadow"/>
    public bool HasDropshadow
    {
        get => ContainedLineRectangle.HasDropshadow;
        set
        {
            ContainedLineRectangle.HasDropshadow = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="Gum.Renderables.LineRectangle.DropshadowColor"/>
    public Color DropshadowColor
    {
        get => ContainedLineRectangle.DropshadowColor;
        set
        {
            ContainedLineRectangle.DropshadowColor = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>Alpha channel of <see cref="DropshadowColor"/>.</summary>
    public int DropshadowAlpha
    {
        get => ContainedLineRectangle.DropshadowColor.A;
        set => DropshadowColor = ColorExtensions.WithAlpha(ContainedLineRectangle.DropshadowColor, (byte)value);
    }

    /// <summary>Red channel of <see cref="DropshadowColor"/>.</summary>
    public int DropshadowRed
    {
        get => ContainedLineRectangle.DropshadowColor.R;
        set => DropshadowColor = ColorExtensions.WithRed(ContainedLineRectangle.DropshadowColor, (byte)value);
    }

    /// <summary>Green channel of <see cref="DropshadowColor"/>.</summary>
    public int DropshadowGreen
    {
        get => ContainedLineRectangle.DropshadowColor.G;
        set => DropshadowColor = ColorExtensions.WithGreen(ContainedLineRectangle.DropshadowColor, (byte)value);
    }

    /// <summary>Blue channel of <see cref="DropshadowColor"/>.</summary>
    public int DropshadowBlue
    {
        get => ContainedLineRectangle.DropshadowColor.B;
        set => DropshadowColor = ColorExtensions.WithBlue(ContainedLineRectangle.DropshadowColor, (byte)value);
    }

    /// <inheritdoc cref="Gum.Renderables.LineRectangle.DropshadowOffsetX"/>
    public float DropshadowOffsetX
    {
        get => ContainedLineRectangle.DropshadowOffsetX;
        set
        {
            ContainedLineRectangle.DropshadowOffsetX = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="Gum.Renderables.LineRectangle.DropshadowOffsetY"/>
    public float DropshadowOffsetY
    {
        get => ContainedLineRectangle.DropshadowOffsetY;
        set
        {
            ContainedLineRectangle.DropshadowOffsetY = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// Isotropic blur radius in pixels for the dropshadow. The raylib renderable
    /// approximates blur via concentric semi-transparent rings; pushing a single value
    /// to both X and Y of the contained <see cref="Gum.Renderables.LineRectangle"/>.
    /// </summary>
    public float DropshadowBlur
    {
        get => ContainedLineRectangle.DropshadowBlurX;
        set
        {
            ContainedLineRectangle.DropshadowBlurX = value;
            ContainedLineRectangle.DropshadowBlurY = value;
            NotifyPropertyChanged();
        }
    }

    // Issue #3175 — additional raylib parity surface so theme source written against the richer
    // Apos.Shapes feature set (per-corner radii, gradient-endpoint units, antialias toggle)
    // compiles unchanged on raylib. The raylib LineRectangle renderable does not consume these
    // yet, so the values round-trip on the runtime as forward compat (same pattern as the SOKOL
    // StrokeDashLength note above). When the renderable gains support, push them through the way
    // the GradientX1 / CornerRadius setters above already do.

    bool _isAntialiased = true;
    /// <inheritdoc cref="CircleRuntime.IsAntialiased"/>
    public bool IsAntialiased
    {
        get => _isAntialiased;
        set { _isAntialiased = value; NotifyPropertyChanged(); }
    }

    float? _customRadiusTopLeft;
    /// <summary>Top-left corner radius override; <c>null</c> falls back to <see cref="CornerRadius"/>. Parity surface, not yet rendered on raylib.</summary>
    public float? CustomRadiusTopLeft
    {
        get => _customRadiusTopLeft;
        set { _customRadiusTopLeft = value; NotifyPropertyChanged(); }
    }

    float? _customRadiusTopRight;
    /// <inheritdoc cref="CustomRadiusTopLeft"/>
    public float? CustomRadiusTopRight
    {
        get => _customRadiusTopRight;
        set { _customRadiusTopRight = value; NotifyPropertyChanged(); }
    }

    float? _customRadiusBottomLeft;
    /// <inheritdoc cref="CustomRadiusTopLeft"/>
    public float? CustomRadiusBottomLeft
    {
        get => _customRadiusBottomLeft;
        set { _customRadiusBottomLeft = value; NotifyPropertyChanged(); }
    }

    float? _customRadiusBottomRight;
    /// <inheritdoc cref="CustomRadiusTopLeft"/>
    public float? CustomRadiusBottomRight
    {
        get => _customRadiusBottomRight;
        set { _customRadiusBottomRight = value; NotifyPropertyChanged(); }
    }

    GeneralUnitType _gradientX1Units;
    /// <summary>Unit for <see cref="GradientX1"/>. Parity surface, not yet rendered on raylib.</summary>
    public GeneralUnitType GradientX1Units
    {
        get => _gradientX1Units;
        set { _gradientX1Units = value; NotifyPropertyChanged(); }
    }

    GeneralUnitType _gradientY1Units;
    /// <inheritdoc cref="GradientX1Units"/>
    public GeneralUnitType GradientY1Units
    {
        get => _gradientY1Units;
        set { _gradientY1Units = value; NotifyPropertyChanged(); }
    }

    GeneralUnitType _gradientX2Units;
    /// <inheritdoc cref="GradientX1Units"/>
    public GeneralUnitType GradientX2Units
    {
        get => _gradientX2Units;
        set { _gradientX2Units = value; NotifyPropertyChanged(); }
    }

    GeneralUnitType _gradientY2Units;
    /// <inheritdoc cref="GradientX1Units"/>
    public GeneralUnitType GradientY2Units
    {
        get => _gradientY2Units;
        set { _gradientY2Units = value; NotifyPropertyChanged(); }
    }

    DimensionUnitType _gradientInnerRadiusUnits;
    /// <summary>Unit for <see cref="GradientInnerRadius"/>. Parity surface, not yet rendered on raylib.</summary>
    public DimensionUnitType GradientInnerRadiusUnits
    {
        get => _gradientInnerRadiusUnits;
        set { _gradientInnerRadiusUnits = value; NotifyPropertyChanged(); }
    }

    DimensionUnitType _gradientOuterRadiusUnits;
    /// <inheritdoc cref="GradientInnerRadiusUnits"/>
    public DimensionUnitType GradientOuterRadiusUnits
    {
        get => _gradientOuterRadiusUnits;
        set { _gradientOuterRadiusUnits = value; NotifyPropertyChanged(); }
    }
#endif

#if !SKIA
    /// <summary>
    /// Per-platform routing for the obsolete single-color surface (<see cref="Color"/>,
    /// <see cref="Alpha"/>, <see cref="Red"/>, <see cref="Green"/>, <see cref="Blue"/>).
    /// XNALIKE and Sokol write the color directly onto their respective renderable slots;
    /// Raylib routes through <see cref="StrokeColor"/> since raylib's renderer draws
    /// <c>StrokeColor ?? Color</c> and the ctor seeds StrokeColor opaque-white — writing the
    /// renderable's de-prioritized Color slot directly (as this did before #2757) was silently
    /// shadowed, so legacy `Color = x` outlines (every Editor-theme outline) rendered white.
    /// </summary>
    Color ObsoleteStrokeColor
    {
#if XNALIKE
        get => _stroke.Color;
        set
        {
            _stroke.Color = value;
            NotifyPropertyChanged();
        }
#elif RAYLIB
        get => _strokeColor;
        // StrokeColor's own setter already calls NotifyPropertyChanged; don't double-call it.
        set => StrokeColor = value;
#else
        get => ContainedLineRectangle.Color;
        set
        {
            ContainedLineRectangle.Color = value;
            NotifyPropertyChanged();
        }
#endif
    }

    // Local (not the per-backend ColorExtensions.WithX aliases): under XNALIKE, ObsoleteStrokeColor
    // is Microsoft.Xna.Framework.Color, which the ToolsUtilitiesStandard.Helpers.ColorExtensions
    // alias (System.Drawing.Color-based) can't accept. XNA Color and Raylib_cs.Color both support
    // the (r, g, b, a) constructor already used by the pre-collapse manual arithmetic below, so a
    // single local helper covers both real compile targets for this file (SKIA is excluded above).
    static Color WithAlpha(Color color, byte value) => new Color(color.R, color.G, color.B, value);
    static Color WithRed(Color color, byte value) => new Color(value, color.G, color.B, color.A);
    static Color WithGreen(Color color, byte value) => new Color(color.R, value, color.B, color.A);
    static Color WithBlue(Color color, byte value) => new Color(color.R, color.G, value, color.A);

    /// <summary>
    /// Obsolete: use <see cref="FillColor"/> or <see cref="StrokeColor"/>. Legacy member that
    /// writes the alpha channel of the stroke renderable's color slot.
    /// </summary>
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2768.")]
    public int Alpha
    {
        get => ObsoleteStrokeColor.A;
        set => ObsoleteStrokeColor = WithAlpha(ObsoleteStrokeColor, (byte)value);
    }

    /// <inheritdoc cref="Alpha"/>
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2768.")]
    public int Red
    {
        get => ObsoleteStrokeColor.R;
        set => ObsoleteStrokeColor = WithRed(ObsoleteStrokeColor, (byte)value);
    }

    /// <inheritdoc cref="Alpha"/>
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2768.")]
    public int Green
    {
        get => ObsoleteStrokeColor.G;
        set => ObsoleteStrokeColor = WithGreen(ObsoleteStrokeColor, (byte)value);
    }

    /// <inheritdoc cref="Alpha"/>
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2768.")]
    public int Blue
    {
        get => ObsoleteStrokeColor.B;
        set => ObsoleteStrokeColor = WithBlue(ObsoleteStrokeColor, (byte)value);
    }

    /// <summary>
    /// Obsolete: use <see cref="FillColor"/> or <see cref="StrokeColor"/>. Routes to the
    /// stroke slot for back-compat — <see cref="RectangleRuntime"/> was historically
    /// outline-only.
    /// </summary>
    // SkiaShapeRuntime base supplies an obsolete Color pass-through under SKIA. #2814.
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2768.")]
    public Color Color
    {
        get => ObsoleteStrokeColor;
        set => ObsoleteStrokeColor = value;
    }
#endif

#if XNALIKE
    // FillColor defaults to opaque white while IsFilled defaults to false, so a freshly-
    // constructed RectangleRuntime renders as a stroke-only outline (the white fill is gated
    // off). This is the "pit of success" default: flipping IsFilled = true fills the rectangle
    // white without needing to also assign FillColor.
    Color _fillColor = Color.White;

    /// <summary>
    /// Color of the filled rectangle. Pushed to the fill slot when <see cref="IsFilled"/> is
    /// <c>true</c>; when <c>IsFilled</c> is <c>false</c> the fill slot is pushed a transparent
    /// color so only the stroke draws. Both core (<see cref="DefaultFilledRectangleRenderable"/>)
    /// and MonoGameGumShapes (<c>RoundedRectangle</c> with <c>IsFilled = true</c>) honor this.
    /// Defaults to opaque white so that flipping <see cref="IsFilled"/> on alone produces a
    /// visible (white) fill.
    /// </summary>
    /// <remarks>
    /// Issue #2938 — non-nullable since the visibility gate moved to <see cref="IsFilled"/>.
    /// </remarks>
    public Color FillColor
    {
        get => _fillColor;
        set
        {
            _fillColor = value;
            if (_fill != null)
            {
                _fill.Color = _isFilled ? _fillColor : new Color(0, 0, 0, 0);
            }
            // Issue #3009 — the fill slot's gradient start mirrors FillColor (no standalone Color1).
            SyncGradientStart();
            NotifyPropertyChanged();
        }
    }

    /// <summary>Red channel of <see cref="FillColor"/>.</summary>
    public int FillRed
    {
        get => _fillColor.R;
        set => FillColor = new Color((byte)value, _fillColor.G, _fillColor.B, _fillColor.A);
    }

    /// <summary>Green channel of <see cref="FillColor"/>.</summary>
    public int FillGreen
    {
        get => _fillColor.G;
        set => FillColor = new Color(_fillColor.R, (byte)value, _fillColor.B, _fillColor.A);
    }

    /// <summary>Blue channel of <see cref="FillColor"/>.</summary>
    public int FillBlue
    {
        get => _fillColor.B;
        set => FillColor = new Color(_fillColor.R, _fillColor.G, (byte)value, _fillColor.A);
    }

    /// <summary>Alpha channel of <see cref="FillColor"/>.</summary>
    public int FillAlpha
    {
        get => _fillColor.A;
        set => FillColor = new Color(_fillColor.R, _fillColor.G, _fillColor.B, (byte)value);
    }

    bool _isFilled = false;

    /// <summary>
    /// Gates fill rendering. When <c>true</c> the fill slot is painted with
    /// <see cref="FillColor"/>. When <c>false</c> (the default) the fill slot is pushed a
    /// transparent color so only the stroke draws — a freshly-constructed runtime is therefore
    /// a stroke-only outline. Because <see cref="FillColor"/> defaults to opaque white, setting
    /// this <c>true</c> alone produces a visible white fill. Stroke visibility is gated
    /// separately by <see cref="StrokeWidth"/> (0 hides stroke).
    /// </summary>
    public bool IsFilled
    {
        get => _isFilled;
        set
        {
            _isFilled = value;
            if (_fill != null)
            {
                _fill.Color = _isFilled ? _fillColor : new Color(0, 0, 0, 0);
            }
            // Dropshadow routing depends on IsFilled — see CircleRuntime for the rationale.
            SyncDropshadowToTarget();
            // Gradient routing also depends on IsFilled: the gradient paints the active body
            // (fill when filled, stroke when stroke-only), so re-route on toggle.
            SyncGradientToTarget();
            NotifyPropertyChanged();
        }
    }

    Color _strokeColor = Color.White;

    /// <summary>
    /// Color of the outline. Defaults to white so a freshly-constructed RectangleRuntime
    /// renders the same visible outline as legacy code did. Set <see cref="StrokeWidth"/> to 0
    /// to hide the stroke. The stroke slot is always non-null on XNA-like backends — core ships
    /// <see cref="DefaultStrokedRectangleRenderable"/> as the default.
    /// </summary>
    public Color StrokeColor
    {
        get => _strokeColor;
        set
        {
            _strokeColor = value;
            _stroke.Color = value;
            // Issue #3009 — the stroke slot's gradient start mirrors StrokeColor (no standalone Color1).
            SyncGradientStart();
            NotifyPropertyChanged();
        }
    }

    /// <summary>Red channel of <see cref="StrokeColor"/>.</summary>
    public int StrokeRed
    {
        get => _strokeColor.R;
        set => StrokeColor = new Color((byte)value, _strokeColor.G, _strokeColor.B, _strokeColor.A);
    }

    /// <summary>Green channel of <see cref="StrokeColor"/>.</summary>
    public int StrokeGreen
    {
        get => _strokeColor.G;
        set => StrokeColor = new Color(_strokeColor.R, (byte)value, _strokeColor.B, _strokeColor.A);
    }

    /// <summary>Blue channel of <see cref="StrokeColor"/>.</summary>
    public int StrokeBlue
    {
        get => _strokeColor.B;
        set => StrokeColor = new Color(_strokeColor.R, _strokeColor.G, (byte)value, _strokeColor.A);
    }

    /// <summary>Alpha channel of <see cref="StrokeColor"/>.</summary>
    public int StrokeAlpha
    {
        get => _strokeColor.A;
        set => StrokeColor = new Color(_strokeColor.R, _strokeColor.G, _strokeColor.B, (byte)value);
    }

    float _strokeWidth = 1;

    /// <inheritdoc cref="CircleRuntime.StrokeWidth"/>
    public float StrokeWidth
    {
        get => _strokeWidth;
        set
        {
            _strokeWidth = value;
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

    Gum.RenderingLibrary.Blend _blend = Gum.RenderingLibrary.Blend.Normal;
    /// <inheritdoc cref="CircleRuntime.Blend"/>
    public Gum.RenderingLibrary.Blend Blend
    {
        get => _blend;
        set
        {
            _blend = value;
            if (_fill is IBlendedRenderable fillBlend) fillBlend.Blend = value;
            if (_stroke is IBlendedRenderable strokeBlend) strokeBlend.Blend = value;
            NotifyPropertyChanged();
        }
    }

    bool _isAntialiased = true;

    /// <inheritdoc cref="CircleRuntime.IsAntialiased"/>
    public bool IsAntialiased
    {
        get => _isAntialiased;
        set
        {
            _isAntialiased = value;
            NotifyPropertyChanged();
        }
    }

    float _cornerRadius;

    /// <summary>
    /// Rounded-corner radius in pixels. Pushed to both slots so a paired fill + stroke draws
    /// matching rounded corners on Apos.Shapes. Core defaults store the value but render
    /// hard-cornered rectangles — install MonoGameGumShapes for visual rounding.
    /// </summary>
    public float CornerRadius
    {
        get => _cornerRadius;
        set
        {
            _cornerRadius = value;
            if (_fill != null) _fill.CornerRadius = value;
            _stroke.CornerRadius = value;
            NotifyPropertyChanged();
        }
    }

    DimensionUnitType _cornerRadiusUnits;

    /// <summary>
    /// Unit of measurement for <see cref="CornerRadius"/> and the per-corner overrides.
    /// <c>Absolute</c> renders the raw pixel value; <c>ScreenPixel</c> divides each radius by
    /// the camera zoom each frame in <see cref="PreRender"/> so corners hold a constant
    /// on-screen size as the camera zooms — matching Skia and <see cref="StrokeWidthUnits"/>
    /// (issue #2925 Phase 0 parity).
    /// </summary>
    public DimensionUnitType CornerRadiusUnits
    {
        get => _cornerRadiusUnits;
        set
        {
            _cornerRadiusUnits = value;
            NotifyPropertyChanged();
        }
    }

    float? _customRadiusTopLeft;
    /// <summary>
    /// Top-left corner radius override. <c>null</c> falls back to <see cref="CornerRadius"/>.
    /// Pushed to both slots. Honored by Apos.Shapes' RoundedRectangle; stored but not rendered
    /// on the core <see cref="DefaultFilledRectangleRenderable"/> / <see cref="DefaultStrokedRectangleRenderable"/>.
    /// </summary>
    public float? CustomRadiusTopLeft
    {
        get => _customRadiusTopLeft;
        set
        {
            _customRadiusTopLeft = value;
            if (_fill != null) _fill.CustomRadiusTopLeft = value;
            _stroke.CustomRadiusTopLeft = value;
            NotifyPropertyChanged();
        }
    }

    float? _customRadiusTopRight;
    /// <inheritdoc cref="CustomRadiusTopLeft"/>
    public float? CustomRadiusTopRight
    {
        get => _customRadiusTopRight;
        set
        {
            _customRadiusTopRight = value;
            if (_fill != null) _fill.CustomRadiusTopRight = value;
            _stroke.CustomRadiusTopRight = value;
            NotifyPropertyChanged();
        }
    }

    float? _customRadiusBottomLeft;
    /// <inheritdoc cref="CustomRadiusTopLeft"/>
    public float? CustomRadiusBottomLeft
    {
        get => _customRadiusBottomLeft;
        set
        {
            _customRadiusBottomLeft = value;
            if (_fill != null) _fill.CustomRadiusBottomLeft = value;
            _stroke.CustomRadiusBottomLeft = value;
            NotifyPropertyChanged();
        }
    }

    float? _customRadiusBottomRight;
    /// <inheritdoc cref="CustomRadiusTopLeft"/>
    public float? CustomRadiusBottomRight
    {
        get => _customRadiusBottomRight;
        set
        {
            _customRadiusBottomRight = value;
            if (_fill != null) _fill.CustomRadiusBottomRight = value;
            _stroke.CustomRadiusBottomRight = value;
            NotifyPropertyChanged();
        }
    }

    #region Gradient

    // Issue #2818 (mirror of CircleRuntime gradient region #2791): backing fields round-trip
    // even when neither slot implements IGradientedRenderable (core defaults wrap SolidRectangle
    // / LineRectangle, no gradient concept). Setters push to whichever slot(s) implement it.

    // Routes the gradient gate to the ACTIVE body slot — the fill when IsFilled, the stroke when
    // stroke-only — and forces the inactive slot's gate off. Mirrors SyncDropshadowToTarget: a
    // single gradient is the active body's paint, so the other slot renders solid (its StrokeColor
    // / FillColor) rather than sharing the gradient and compositing invisibly over it. Re-run from
    // both the UseGradient and IsFilled setters so toggling IsFilled re-routes the gate.
    void SyncGradientToTarget()
    {
        var fillGrad = _fill as IGradientedRenderable;
        var strokeGrad = _stroke as IGradientedRenderable;
        var active = _isFilled ? fillGrad : strokeGrad;
        var inactive = _isFilled ? strokeGrad : fillGrad;
        if (active != null) active.UseGradient = _useGradient;
        if (inactive != null) inactive.UseGradient = false;
    }

    // Issue #3009 — the gradient START stop is the slot's own solid body color; Circle/Rectangle
    // no longer carry a standalone Color1. Each slot mirrors its solid color into its
    // Red1/Green1/Blue1/Alpha1 so the gradient start equals the color the shape was already
    // showing (no jump when UseGradient toggles), and the dropshadow alpha — which the Apos
    // renderable scales by the slot's Color.A — converges onto the gradient start alpha. Called
    // from the FillColor / StrokeColor setters and the constructor; the UseGradient gate routing
    // stays in SyncGradientToTarget.
    void SyncGradientStart()
    {
        if (_fill is IGradientedRenderable fillGrad)
        {
            fillGrad.Red1 = _fillColor.R;
            fillGrad.Green1 = _fillColor.G;
            fillGrad.Blue1 = _fillColor.B;
            fillGrad.Alpha1 = _fillColor.A;
        }
        if (_stroke is IGradientedRenderable strokeGrad)
        {
            strokeGrad.Red1 = _strokeColor.R;
            strokeGrad.Green1 = _strokeColor.G;
            strokeGrad.Blue1 = _strokeColor.B;
            strokeGrad.Alpha1 = _strokeColor.A;
        }
    }

    bool _useGradient;
    /// <inheritdoc cref="CircleRuntime.UseGradient"/>
    public bool UseGradient
    {
        get => _useGradient;
        set
        {
            _useGradient = value;
            SyncGradientToTarget();
            NotifyPropertyChanged();
        }
    }

    GradientType _gradientType;
    public GradientType GradientType
    {
        get => _gradientType;
        set
        {
            _gradientType = value;
            if (_fill is IGradientedRenderable fillGrad) fillGrad.GradientType = value;
            if (_stroke is IGradientedRenderable strokeGrad) strokeGrad.GradientType = value;
            NotifyPropertyChanged();
        }
    }

    // Issue #3009 — the gradient start (Red1/Green1/Blue1/Alpha1 / Color1) is no longer stored on
    // the runtime. It is driven from the active body color via SyncGradientStart(); the standalone
    // Color1 surface was dropped for Circle/Rectangle (gradient support is unshipped, so no data to
    // preserve). Color2 below remains the only standalone gradient color.

    int _alpha2 = 255;
    public int Alpha2
    {
        get => _alpha2;
        set
        {
            _alpha2 = value;
            if (_fill is IGradientedRenderable fillGrad) fillGrad.Alpha2 = value;
            if (_stroke is IGradientedRenderable strokeGrad) strokeGrad.Alpha2 = value;
            NotifyPropertyChanged();
        }
    }

    int _red2;
    public int Red2
    {
        get => _red2;
        set
        {
            _red2 = value;
            if (_fill is IGradientedRenderable fillGrad) fillGrad.Red2 = value;
            if (_stroke is IGradientedRenderable strokeGrad) strokeGrad.Red2 = value;
            NotifyPropertyChanged();
        }
    }

    int _green2;
    public int Green2
    {
        get => _green2;
        set
        {
            _green2 = value;
            if (_fill is IGradientedRenderable fillGrad) fillGrad.Green2 = value;
            if (_stroke is IGradientedRenderable strokeGrad) strokeGrad.Green2 = value;
            NotifyPropertyChanged();
        }
    }

    int _blue2;
    public int Blue2
    {
        get => _blue2;
        set
        {
            _blue2 = value;
            if (_fill is IGradientedRenderable fillGrad) fillGrad.Blue2 = value;
            if (_stroke is IGradientedRenderable strokeGrad) strokeGrad.Blue2 = value;
            NotifyPropertyChanged();
        }
    }

    public Color Color2
    {
        get => new Color(_red2, _green2, _blue2, _alpha2);
        set
        {
            Red2 = value.R;
            Green2 = value.G;
            Blue2 = value.B;
            Alpha2 = value.A;
        }
    }

    float _gradientX1;
    public float GradientX1
    {
        get => _gradientX1;
        set
        {
            _gradientX1 = value;
            if (_fill is IGradientedRenderable fillGrad) fillGrad.GradientX1 = value;
            if (_stroke is IGradientedRenderable strokeGrad) strokeGrad.GradientX1 = value;
            NotifyPropertyChanged();
        }
    }

    GeneralUnitType _gradientX1Units;
    public GeneralUnitType GradientX1Units
    {
        get => _gradientX1Units;
        set
        {
            _gradientX1Units = value;
            if (_fill is IGradientedRenderable fillGrad) fillGrad.GradientX1Units = value;
            if (_stroke is IGradientedRenderable strokeGrad) strokeGrad.GradientX1Units = value;
            NotifyPropertyChanged();
        }
    }

    float _gradientY1;
    public float GradientY1
    {
        get => _gradientY1;
        set
        {
            _gradientY1 = value;
            if (_fill is IGradientedRenderable fillGrad) fillGrad.GradientY1 = value;
            if (_stroke is IGradientedRenderable strokeGrad) strokeGrad.GradientY1 = value;
            NotifyPropertyChanged();
        }
    }

    GeneralUnitType _gradientY1Units;
    public GeneralUnitType GradientY1Units
    {
        get => _gradientY1Units;
        set
        {
            _gradientY1Units = value;
            if (_fill is IGradientedRenderable fillGrad) fillGrad.GradientY1Units = value;
            if (_stroke is IGradientedRenderable strokeGrad) strokeGrad.GradientY1Units = value;
            NotifyPropertyChanged();
        }
    }

    float _gradientX2;
    public float GradientX2
    {
        get => _gradientX2;
        set
        {
            _gradientX2 = value;
            if (_fill is IGradientedRenderable fillGrad) fillGrad.GradientX2 = value;
            if (_stroke is IGradientedRenderable strokeGrad) strokeGrad.GradientX2 = value;
            NotifyPropertyChanged();
        }
    }

    GeneralUnitType _gradientX2Units;
    public GeneralUnitType GradientX2Units
    {
        get => _gradientX2Units;
        set
        {
            _gradientX2Units = value;
            if (_fill is IGradientedRenderable fillGrad) fillGrad.GradientX2Units = value;
            if (_stroke is IGradientedRenderable strokeGrad) strokeGrad.GradientX2Units = value;
            NotifyPropertyChanged();
        }
    }

    float _gradientY2;
    public float GradientY2
    {
        get => _gradientY2;
        set
        {
            _gradientY2 = value;
            if (_fill is IGradientedRenderable fillGrad) fillGrad.GradientY2 = value;
            if (_stroke is IGradientedRenderable strokeGrad) strokeGrad.GradientY2 = value;
            NotifyPropertyChanged();
        }
    }

    GeneralUnitType _gradientY2Units;
    public GeneralUnitType GradientY2Units
    {
        get => _gradientY2Units;
        set
        {
            _gradientY2Units = value;
            if (_fill is IGradientedRenderable fillGrad) fillGrad.GradientY2Units = value;
            if (_stroke is IGradientedRenderable strokeGrad) strokeGrad.GradientY2Units = value;
            NotifyPropertyChanged();
        }
    }

    float _gradientInnerRadius;
    public float GradientInnerRadius
    {
        get => _gradientInnerRadius;
        set
        {
            _gradientInnerRadius = value;
            if (_fill is IGradientedRenderable fillGrad) fillGrad.GradientInnerRadius = value;
            if (_stroke is IGradientedRenderable strokeGrad) strokeGrad.GradientInnerRadius = value;
            NotifyPropertyChanged();
        }
    }

    DimensionUnitType _gradientInnerRadiusUnits;
    public DimensionUnitType GradientInnerRadiusUnits
    {
        get => _gradientInnerRadiusUnits;
        set
        {
            _gradientInnerRadiusUnits = value;
            if (_fill is IGradientedRenderable fillGrad) fillGrad.GradientInnerRadiusUnits = value;
            if (_stroke is IGradientedRenderable strokeGrad) strokeGrad.GradientInnerRadiusUnits = value;
            NotifyPropertyChanged();
        }
    }

    float _gradientOuterRadius;
    public float GradientOuterRadius
    {
        get => _gradientOuterRadius;
        set
        {
            _gradientOuterRadius = value;
            if (_fill is IGradientedRenderable fillGrad) fillGrad.GradientOuterRadius = value;
            if (_stroke is IGradientedRenderable strokeGrad) strokeGrad.GradientOuterRadius = value;
            NotifyPropertyChanged();
        }
    }

    DimensionUnitType _gradientOuterRadiusUnits;
    public DimensionUnitType GradientOuterRadiusUnits
    {
        get => _gradientOuterRadiusUnits;
        set
        {
            _gradientOuterRadiusUnits = value;
            if (_fill is IGradientedRenderable fillGrad) fillGrad.GradientOuterRadiusUnits = value;
            if (_stroke is IGradientedRenderable strokeGrad) strokeGrad.GradientOuterRadiusUnits = value;
            NotifyPropertyChanged();
        }
    }

    #endregion

    #region Dropshadow

    // Issue #2818 (mirror of CircleRuntime dropshadow region #2797): single-slot push via
    // DropshadowTarget — pushing to both would double up the visible shadow. Target picks
    // by IsFilled: fill when true (the rectangle casts the shadow), stroke when false
    // (the fill is gated transparent and can't cast a visible shadow). SyncDropshadowToTarget
    // re-routes on IsFilled toggle so the old target releases its flag.

    IDropshadowRenderable? DropshadowTarget
    {
        get
        {
            var fillDs = _fill as IDropshadowRenderable;
            var strokeDs = _stroke as IDropshadowRenderable;
            return _isFilled ? (fillDs ?? strokeDs) : (strokeDs ?? fillDs);
        }
    }

    void SyncDropshadowToTarget()
    {
        var fillDs = _fill as IDropshadowRenderable;
        var strokeDs = _stroke as IDropshadowRenderable;
        var target = DropshadowTarget;
        var other = ReferenceEquals(target, fillDs) ? strokeDs : fillDs;

        if (other != null) other.HasDropshadow = false;
        if (target != null)
        {
            target.HasDropshadow = _hasDropshadow;
            target.DropshadowColor = _dropshadowColor;
            target.DropshadowOffsetX = _dropshadowOffsetX;
            target.DropshadowOffsetY = _dropshadowOffsetY;
            target.DropshadowBlurX = _dropshadowBlur;
            target.DropshadowBlurY = _dropshadowBlur;
        }
    }

    bool _hasDropshadow;
    /// <inheritdoc cref="CircleRuntime.HasDropshadow"/>
    public bool HasDropshadow
    {
        get => _hasDropshadow;
        set
        {
            _hasDropshadow = value;
            if (DropshadowTarget is { } target) target.HasDropshadow = value;
            NotifyPropertyChanged();
        }
    }

    Color _dropshadowColor;
    public Color DropshadowColor
    {
        get => _dropshadowColor;
        set
        {
            _dropshadowColor = value;
            if (DropshadowTarget is { } target) target.DropshadowColor = value;
            NotifyPropertyChanged();
        }
    }

    public int DropshadowAlpha
    {
        get => _dropshadowColor.A;
        set
        {
            DropshadowColor = new Color(_dropshadowColor.R, _dropshadowColor.G, _dropshadowColor.B, (byte)value);
        }
    }

    public int DropshadowRed
    {
        get => _dropshadowColor.R;
        set
        {
            DropshadowColor = new Color((byte)value, _dropshadowColor.G, _dropshadowColor.B, _dropshadowColor.A);
        }
    }

    public int DropshadowGreen
    {
        get => _dropshadowColor.G;
        set
        {
            DropshadowColor = new Color(_dropshadowColor.R, (byte)value, _dropshadowColor.B, _dropshadowColor.A);
        }
    }

    public int DropshadowBlue
    {
        get => _dropshadowColor.B;
        set
        {
            DropshadowColor = new Color(_dropshadowColor.R, _dropshadowColor.G, (byte)value, _dropshadowColor.A);
        }
    }

    float _dropshadowOffsetX;
    public float DropshadowOffsetX
    {
        get => _dropshadowOffsetX;
        set
        {
            _dropshadowOffsetX = value;
            if (DropshadowTarget is { } target) target.DropshadowOffsetX = value;
            NotifyPropertyChanged();
        }
    }

    float _dropshadowOffsetY;
    public float DropshadowOffsetY
    {
        get => _dropshadowOffsetY;
        set
        {
            _dropshadowOffsetY = value;
            if (DropshadowTarget is { } target) target.DropshadowOffsetY = value;
            NotifyPropertyChanged();
        }
    }

    float _dropshadowBlur;
    /// <inheritdoc cref="CircleRuntime.DropshadowBlur"/>
    public float DropshadowBlur
    {
        get => _dropshadowBlur;
        set
        {
            _dropshadowBlur = value;
            if (DropshadowTarget is { } target)
            {
                target.DropshadowBlurX = value;
                target.DropshadowBlurY = value;
            }
            NotifyPropertyChanged();
        }
    }

    #endregion

    #region DashedStroke

    // Issue #2818 (mirror of CircleRuntime dashed region #2796): values round-trip on backing
    // fields; pushed to the stroke slot in PreRender alongside StrokeWidth so ScreenPixel
    // scaling keeps dash/gap in sync with stroke. Stroke-only — dashing is guarded by
    // !IsFilled in the Apos RoundedRectangle renderable.

    float _strokeDashLength;
    /// <inheritdoc cref="CircleRuntime.StrokeDashLength"/>
    public float StrokeDashLength
    {
        get => _strokeDashLength;
        set
        {
            _strokeDashLength = value;
            NotifyPropertyChanged();
        }
    }

    float _strokeGapLength;
    /// <inheritdoc cref="CircleRuntime.StrokeGapLength"/>
    public float StrokeGapLength
    {
        get => _strokeGapLength;
        set
        {
            _strokeGapLength = value;
            NotifyPropertyChanged();
        }
    }

    #endregion

    /// <inheritdoc cref="CircleRuntime.PreRender"/>
    public override void PreRender()
    {
        // Resolve the camera once up front — drives both the ScreenPixel → world conversion
        // for StrokeWidth and the screen → world conversion for aposAaContribution (#2936).
        // Mirrors CircleRuntime.PreRender.
        var camera = this.EffectiveManagers?.Renderer?.Camera;
        float cameraZoom = camera?.Zoom ?? 1f;

        float strokeWidth = _strokeWidth;
        float strokeDashLength = _strokeDashLength;
        float strokeGapLength = _strokeGapLength;
        if (_strokeWidthUnits == DimensionUnitType.ScreenPixel && camera != null)
        {
            // Mirror AposShapeRuntime.PreRender — dash and gap scale alongside stroke width.
            strokeWidth /= cameraZoom;
            strokeDashLength /= cameraZoom;
            strokeGapLength /= cameraZoom;
        }
        // Mirror of CircleRuntime.PreRender — same two-case structure (user-set <= 0 vs
        // positive thin stroke). See CircleRuntime.PreRender for the full rationale on why
        // these two cases must stay separate. tl;dr:
        //   - StrokeWidth <= 0 (user hide-stroke gate, #2950 follow-up) → push literal 0
        //     so the renderable's HasVisibleOutput suppresses the draw.
        //   - StrokeWidth > 0 with AA → subtract the 1 px Apos AA contribution and floor at
        //     a tiny positive epsilon so thin strokes still render (#2818).
        const float aposAaContribution = 1f;
        const float aposMinThicknessEpsilon = 0.01f;
        // #2936 — aposAaContribution is in SCREEN pixels; convert to world units. See
        // CircleRuntime.PreRender for the full rationale.
        float aposAaContributionWorld = aposAaContribution / cameraZoom;
        float renderableStrokeWidth;
        if (strokeWidth <= 0)
        {
            renderableStrokeWidth = 0f;
        }
        else if (_isAntialiased && _stroke is IAntialiasedRenderable)
        {
            renderableStrokeWidth = Math.Max(aposMinThicknessEpsilon, strokeWidth - aposAaContributionWorld);
        }
        else
        {
            renderableStrokeWidth = strokeWidth;
        }
        _stroke.StrokeWidth = renderableStrokeWidth;

        // Issue #2818: push dash/gap to the stroke slot when it supports dashing.
        if (_stroke is IDashedStrokeRenderable strokeDashed)
        {
            strokeDashed.StrokeDashLength = strokeDashLength;
            strokeDashed.StrokeGapLength = strokeGapLength;
        }

        // Issue #2818: push AA to both slots so a single setter flips fill + stroke together.
        if (_fill is IAntialiasedRenderable fillAa) fillAa.IsAntialiased = _isAntialiased;
        if (_stroke is IAntialiasedRenderable strokeAa) strokeAa.IsAntialiased = _isAntialiased;

        // Mirror size to stroke when fill is the contained object — see CircleRuntime.PreRender.
        if (_fill is IPositionedSizedObject fillSized && _stroke is IPositionedSizedObject strokeSized)
        {
            strokeSized.Width = fillSized.Width;
            strokeSized.Height = fillSized.Height;
        }

        // Issue #2925 (Phase 0) — resolve unit-aware corner radii each frame so ScreenPixel
        // holds a constant on-screen size at non-1 camera zoom. Mirrors the Skia branch in
        // RoundedRectangleRuntime.PreRender / RectangleRuntime SKIA PreRender. The setters
        // already pushed the raw values into both slots; this pass overwrites them with the
        // resolved values when the camera is available. Under Absolute (or with no camera)
        // the resolved values equal the raw values so the slot state is unchanged.
        float cornerRadius = _cornerRadius;
        float? topLeft = _customRadiusTopLeft;
        float? topRight = _customRadiusTopRight;
        float? bottomLeft = _customRadiusBottomLeft;
        float? bottomRight = _customRadiusBottomRight;

        if (_cornerRadiusUnits == DimensionUnitType.ScreenPixel)
        {
            if (camera != null)
            {
                cornerRadius /= camera.Zoom;
                topLeft /= camera.Zoom;
                topRight /= camera.Zoom;
                bottomLeft /= camera.Zoom;
                bottomRight /= camera.Zoom;
            }
        }

        if (_fill != null)
        {
            _fill.CornerRadius = cornerRadius;
            _fill.CustomRadiusTopLeft = topLeft;
            _fill.CustomRadiusTopRight = topRight;
            _fill.CustomRadiusBottomLeft = bottomLeft;
            _fill.CustomRadiusBottomRight = bottomRight;
        }
        _stroke.CornerRadius = cornerRadius;
        _stroke.CustomRadiusTopLeft = topLeft;
        _stroke.CustomRadiusTopRight = topRight;
        _stroke.CustomRadiusBottomLeft = bottomLeft;
        _stroke.CustomRadiusBottomRight = bottomRight;

        // Mirror of CircleRuntime.PreRender's FillRadiusInset push (#2834). When both slots are
        // visible, inset the fill so its outer edge sits inside the stroke's opaque band —
        // otherwise a filled rectangle with a semi-transparent stroke shows the FILL through the
        // stroke instead of the background. Pushed via FillInset rather than mutating
        // fill.Width/Height (the fill IS the runtime's contained sizing object; mutating Width
        // would feed back into layout and shrink the rectangle each frame). Inset per side =
        // AA-compensated stroke width, floored at the AA contribution for hairline strokes.
        // Gated on stroke visibility: alpha 0 OR StrokeWidth 0 means no stroke is drawn, and an
        // inset would render a thin background gap where the stroke would have been.
        if (_fill != null)
        {
            float fillInset = 0f;
            if (_stroke.Color.A > 0 && _strokeWidth > 0)
            {
                fillInset = renderableStrokeWidth;
                if (_isAntialiased && _stroke is IAntialiasedRenderable)
                {
                    fillInset = Math.Max(fillInset, aposAaContributionWorld);
                }
            }
            _fill.FillInset = fillInset;
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Issue #2818 — mirror of <see cref="CircleRuntime.Clone"/>: rebuild both slots via
    /// <see cref="RenderableRegistry"/> so the clone is fully independent of the source.
    /// </remarks>
    public override GraphicalUiElement Clone()
    {
        RectangleRuntime toReturn = (RectangleRuntime)base.Clone();

        toReturn._fill = (IFilledRectangleRenderable?)toReturn.mContainedObjectAsIpso;
        toReturn._stroke = RenderableRegistry.Create<IStrokedRectangleRenderable>(toReturn)
            ?? new DefaultStrokedRectangleRenderable();

        if (toReturn._fill is IRenderableIpso fillIpso
            && toReturn._stroke is IRenderableIpso strokeIpso)
        {
            strokeIpso.Parent = fillIpso;
        }
        else if (toReturn._fill == null)
        {
            toReturn.SetContainedObject(toReturn._stroke);
        }

        toReturn.StrokeColor = toReturn.StrokeColor;
        // Issue #2938 — re-fire FillColor / IsFilled so the freshly-built fill slot picks up
        // the user's values; MemberwiseClone copied the backing fields but the new slot was
        // constructed in its default state.
        toReturn.FillColor = toReturn.FillColor;
        toReturn.IsFilled = toReturn.IsFilled;
        // Issue #2937 — re-fire Blend onto the freshly-built slots for the same reason.
        toReturn.Blend = toReturn.Blend;
        return toReturn;
    }
#endif

#if SKIA
    /// <summary>
    /// Routes SkiaShapeRuntime solid/gradient/stroke/dropshadow accessors to the
    /// contained RoundedRectangle. Issue #2814 / #2818.
    /// </summary>
    protected override RenderableShapeBase ContainedRenderable => ContainedLineRectangle;

    // Unified-API cleanup: RectangleRuntime is a new fill+stroke shape, so the single-color
    // legacy members it inherits from SkiaShapeRuntime (the base for ALL Skia shapes) are
    // obsolete on this surface — users typed as RectangleRuntime are steered to FillColor /
    // StrokeColor, matching the XNA-like and raylib surfaces. The members stay live on the base
    // for the legacy single-color shapes (Arc, RoundedRectangle, ColoredCircle, ...). Color is
    // already [Obsolete] on the base, so only Red/Green/Blue/Alpha need shadowing here.
    /// <summary>Obsolete: use <see cref="SkiaShapeRuntime.FillColor"/> or <see cref="SkiaShapeRuntime.StrokeColor"/>.</summary>
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2768.")]
    public new int Alpha
    {
        get => base.Alpha;
        set => base.Alpha = value;
    }

    /// <inheritdoc cref="Alpha"/>
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2768.")]
    public new int Red
    {
        get => base.Red;
        set => base.Red = value;
    }

    /// <inheritdoc cref="Alpha"/>
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2768.")]
    public new int Green
    {
        get => base.Green;
        set => base.Green = value;
    }

    /// <inheritdoc cref="Alpha"/>
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2768.")]
    public new int Blue
    {
        get => base.Blue;
        set => base.Blue = value;
    }

    // Issue #3009 — the gradient START stop is the active body color (FillColor when filled,
    // StrokeColor otherwise), driven by the base's two-slot SyncGradientStartToBody. The standalone
    // Color1 surface inherited from SkiaShapeRuntime is unsupported on the new fill+stroke
    // Rectangle: shadow it as an error so consumers are steered to FillColor / StrokeColor (mirrors
    // the XNALIKE hard-drop). The members stay live on the base for legacy single-color shapes.
    /// <summary>Obsolete: the gradient start is the active body color. Set <see cref="SkiaShapeRuntime.FillColor"/> / <see cref="SkiaShapeRuntime.StrokeColor"/>. See issue #3009.</summary>
    [Obsolete("The gradient start is the active body color (FillColor when filled, StrokeColor otherwise). Set FillColor / StrokeColor instead of Color1. See issue #3009.", error: true)]
    public new SKColor Color1
    {
        get => base.Color1;
        set => base.Color1 = value;
    }

    /// <inheritdoc cref="Color1"/>
    [Obsolete("The gradient start is the active body color (FillColor when filled, StrokeColor otherwise). Set FillColor / StrokeColor instead of Red1. See issue #3009.", error: true)]
    public new int Red1
    {
        get => base.Red1;
        set => base.Red1 = value;
    }

    /// <inheritdoc cref="Color1"/>
    [Obsolete("The gradient start is the active body color (FillColor when filled, StrokeColor otherwise). Set FillColor / StrokeColor instead of Green1. See issue #3009.", error: true)]
    public new int Green1
    {
        get => base.Green1;
        set => base.Green1 = value;
    }

    /// <inheritdoc cref="Color1"/>
    [Obsolete("The gradient start is the active body color (FillColor when filled, StrokeColor otherwise). Set FillColor / StrokeColor instead of Blue1. See issue #3009.", error: true)]
    public new int Blue1
    {
        get => base.Blue1;
        set => base.Blue1 = value;
    }

    /// <inheritdoc cref="Color1"/>
    [Obsolete("The gradient start is the active body color (FillColor when filled, StrokeColor otherwise). Set FillColor / StrokeColor instead of Alpha1. See issue #3009.", error: true)]
    public new int Alpha1
    {
        get => base.Alpha1;
        set => base.Alpha1 = value;
    }

    /// <inheritdoc cref="CircleRuntime.DropshadowBlur"/>
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
    /// Obsolete on the plain <see cref="RectangleRuntime"/> — use <see cref="DropshadowBlur"/>.
    /// Hidden shadow of the inherited per-axis blur, kept only so existing code compiles; the
    /// new-shape contract exposes a single scalar (per-axis blur stays on the legacy Skia shapes
    /// like <c>RoundedRectangleRuntime</c> / <c>ColoredCircleRuntime</c>).
    /// </summary>
    [Obsolete("Use DropshadowBlur (scalar). The plain Rectangle dropshadow blur is a single isotropic value by design.")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public new float DropshadowBlurX
    {
        get => base.DropshadowBlurX;
        set => base.DropshadowBlurX = value;
    }

    /// <inheritdoc cref="DropshadowBlurX"/>
    [Obsolete("Use DropshadowBlur (scalar). The plain Rectangle dropshadow blur is a single isotropic value by design.")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public new float DropshadowBlurY
    {
        get => base.DropshadowBlurY;
        set => base.DropshadowBlurY = value;
    }

    /// <summary>
    /// Rounded-corner radius in pixels. Pushed to both slots each frame in
    /// <see cref="PreRender"/> so the outline traces the same rounded corners as the fill.
    /// Default of 0 keeps the historical hard-cornered visual.
    /// </summary>
    public float CornerRadius { get; set; }

    /// <inheritdoc cref="RectangleRuntime.CornerRadius"/>
    public float? CustomRadiusTopLeft { get; set; }
    /// <inheritdoc cref="RectangleRuntime.CornerRadius"/>
    public float? CustomRadiusTopRight { get; set; }
    /// <inheritdoc cref="RectangleRuntime.CornerRadius"/>
    public float? CustomRadiusBottomLeft { get; set; }
    /// <inheritdoc cref="RectangleRuntime.CornerRadius"/>
    public float? CustomRadiusBottomRight { get; set; }

    /// <summary>
    /// Unit of measurement for <see cref="CornerRadius"/> and per-corner overrides. Mirrors
    /// <see cref="StrokeWidthUnits"/>: <c>ScreenPixel</c> divides by camera zoom each frame.
    /// </summary>
    public DimensionUnitType CornerRadiusUnits { get; set; }

    /// <inheritdoc/>
    public override void PreRender()
    {
        base.PreRender();

        // Issue #2818 — resolve unit-aware corner radii then push to both slots so the
        // outline traces the same rounded corners as the fill (same pattern as
        // RoundedRectangleRuntime.PreRender / SkiaShapeRuntime.PreRender's width/height mirror).
        var cornerRadius = CornerRadius;
        var topLeft = CustomRadiusTopLeft;
        var topRight = CustomRadiusTopRight;
        var bottomLeft = CustomRadiusBottomLeft;
        var bottomRight = CustomRadiusBottomRight;

        if (CornerRadiusUnits == DimensionUnitType.ScreenPixel && this.EffectiveManagers != null)
        {
            var camera = this.EffectiveManagers.Renderer.Camera;
            cornerRadius /= camera.Zoom;
            topLeft /= camera.Zoom;
            topRight /= camera.Zoom;
            bottomLeft /= camera.Zoom;
            bottomRight /= camera.Zoom;
        }

        var fill = ContainedLineRectangle;
        fill.CornerRadius = cornerRadius;
        fill.CustomRadiusTopLeft = topLeft;
        fill.CustomRadiusTopRight = topRight;
        fill.CustomRadiusBottomLeft = bottomLeft;
        fill.CustomRadiusBottomRight = bottomRight;

        if (StrokeRenderable is SkiaGum.Renderables.RoundedRectangle strokeRounded)
        {
            strokeRounded.CornerRadius = cornerRadius;
            strokeRounded.CustomRadiusTopLeft = topLeft;
            strokeRounded.CustomRadiusTopRight = topRight;
            strokeRounded.CustomRadiusBottomLeft = bottomLeft;
            strokeRounded.CustomRadiusBottomRight = bottomRight;
        }
    }

    /// <inheritdoc/>
    public override GraphicalUiElement Clone()
    {
        RectangleRuntime toReturn = (RectangleRuntime)base.Clone();
        // Reset cached renderable reference so the clone re-resolves against its own
        // RenderableComponent on next access. The fill slot color/dimensions were copied
        // by RoundedRectangle.Clone (ICloneable, MemberwiseClone).
        toReturn.containedLineRectangle = null!;
        // Issue #2790 recipe - drop the inherited stroke-slot reference and rebuild a fresh
        // one parented to the clone fill so the clone is fully independent.
        toReturn.ClearStrokeRenderable();
        toReturn.SetStrokeRenderable(new ContainedLineRectangle { CornerRadius = 0 });
        // Re-fire StrokeColor so the user color (held on _strokeColor via MemberwiseClone)
        // is pushed into the fresh stroke slot.
        toReturn.StrokeColor = toReturn.StrokeColor;
        return toReturn;
    }
#endif

    /// <inheritdoc cref="GraphicalUiElement.AddToManagers()"/>
    [Obsolete("Use the AddToRoot extension method instead (e.g. myRectangle.AddToRoot()).")]
    public new void AddToManagers() => base.AddToManagers(SystemManagers.Default, layer: null);

    public RectangleRuntime(bool fullInstantiation = true, SystemManagers? systemManagers = null)
    {
        if (fullInstantiation)
        {
#if XNALIKE
            // Two-slot construct-time binding (#2768). Both default factories are registered by
            // core's [ModuleInitializer]s; the optional MonoGameGumShapes package overrides
            // both with Apos.Shapes' RoundedRectangle (one for IsFilled=true, one for false).
            // Defensive fallback to the core defaults: the ModuleInitializer registrations
            // don't survive a RenderableRegistry.Reset (the load-order contract gap tracked
            // in #2761 / #2768), so test teardown + a subsequent ctor would otherwise leave
            // both slots null. Mirrors the equivalent fallback in CircleRuntime.
            _fill = RenderableRegistry.Create<IFilledRectangleRenderable>(this)
                ?? new DefaultFilledRectangleRenderable();
            _stroke = RenderableRegistry.Create<IStrokedRectangleRenderable>(this)
                ?? new DefaultStrokedRectangleRenderable();

            if (_fill is IRenderableIpso fillIpso)
            {
                SetContainedObject(_fill);
                if (_stroke is IRenderableIpso strokeIpso)
                {
                    strokeIpso.Parent = fillIpso;
                }
            }
            else
            {
                SetContainedObject(_stroke);
            }

            // Initial defaults — stroke white, fill white but gated off (IsFilled = false), so
            // the fill slot is pushed a transparent color and a freshly-constructed
            // RectangleRuntime renders as a stroke-only outline, layout 50x50. Flipping
            // IsFilled = true paints the fill white without needing to assign FillColor.
            _stroke.Color = _strokeColor;
            if (_fill != null)
            {
                _fill.Color = _isFilled ? _fillColor : new Color(0, 0, 0, 0);
            }
            Width = 50;
            Height = 50;

            // Issue #3009 — seed each slot's gradient start from its (white) body color so flipping
            // UseGradient on a freshly-constructed rectangle starts from white, matching the
            // no-jump contract.
            SyncGradientStart();

            // Issue #2818 (mirror of CircleRuntime #2797): pre-seed opaque-black dropshadow
            // with a slight downward offset/blur so toggling HasDropshadow = true at runtime
            // produces a visible shadow without further setup.
            DropshadowAlpha = 255;
            DropshadowOffsetY = 3;
            DropshadowBlur = 3;

            if (_fill is IPositionedSizedObject ctorFill && _stroke is IPositionedSizedObject ctorStroke)
            {
                ctorStroke.Width = ctorFill.Width;
                ctorStroke.Height = ctorFill.Height;
            }
#elif SKIA
            // Issue #2814: two-slot fill+stroke composition for Skia (mirrors the CircleRuntime
            // wiring from issue #2790). The contained renderable becomes the fill slot; a
            // second one, registered via SetStrokeRenderable, is parented under the fill so
            // SkiaShapeRuntime.FillColor and StrokeColor each route to their own renderable.
            // Issue #2818: contained type is now RoundedRectangle so CornerRadius / per-corner
            // radii reach the renderer; CornerRadius default of 0 keeps the historical
            // hard-cornered visual (RoundedRectangle's own ctor defaults to 5).
            var rectangle = new ContainedLineRectangle { CornerRadius = 0 };
            SetContainedObject(rectangle);
            containedLineRectangle = rectangle;

            SetStrokeRenderable(new ContainedLineRectangle { CornerRadius = 0 });

            // Defaults: white fill gated off (IsFilled = false) + white stroke —
            // RectangleRuntime's stroke-only outline visual. Because FillColor defaults to
            // opaque white, flipping IsFilled = true paints a white fill without assigning a
            // color. IsFilled must be set explicitly here (the shared SkiaShapeRuntime base
            // defaults it to true for the legacy single-color shapes).
            //
            // FillColor must be explicitly assigned even though the gate forces transparent:
            // SkiaShapeRuntime.PushFillColorToSlot only runs from the FillColor / IsFilled
            // property setters, never from field init. Setting IsFilled = false re-pushes a
            // transparent color into the fill slot, so the RoundedRectangle renderable doesn't
            // retain its own white constructor default and render as a solid white block.
            FillColor = SKColors.White;
            IsFilled = false;
            StrokeColor = SKColors.White;
            StrokeWidth = 1;
            StrokeWidthUnits = DimensionUnitType.ScreenPixel;

            // Dropshadow off by default; pre-seed alpha/offset/blur so toggling HasDropshadow =
            // true at runtime produces a visible shadow without further setup. Use the scalar
            // DropshadowBlur (isotropic 3) so the default matches MonoGame/raylib; the previous
            // DropshadowBlurY-only seed left the scalar getter (X axis) reading 0.
            DropshadowAlpha = 255;
            DropshadowOffsetY = 3;
            DropshadowBlur = 3;

            Width = 50;
            Height = 50;
#else
            var rectangle = new ContainedLineRectangle(systemManagers);
            SetContainedObject(rectangle);
            containedLineRectangle = rectangle;

            rectangle.Color = ColorExtensions.White;

#if RAYLIB
            // #2757 — match Skia's default: a fresh RectangleRuntime ships with an opaque
            // 1 px white stroke so cells that only set FillColor still get a visible outline.
            // Without this, the gallery's Modes / Alignment / CornerRadius rows lost their
            // outlines on raylib but kept them on Skia. SOKOL renderable doesn't expose
            // StrokeColor yet — leave it null there so the existing outline-via-Color path
            // (LineRectangle.Render falls back to Color when StrokeColor is null and no fill
            // is requested) keeps working unchanged.
            //
            // Push runtime-held FillColor / StrokeColor / IsFilled defaults onto the renderable
            // so the runtime properties report consistent state at construction. FillColor
            // defaults to opaque white and IsFilled defaults to false → a fresh rectangle
            // renders as a stroke-only outline; flipping IsFilled = true paints it white.
            rectangle.StrokeColor = _strokeColor;
            rectangle.IsFilled = false;
            // Issue #3009 — gate the fill color by IsFilled (null when not filled) so a fresh
            // rectangle is a stroke-only outline, and seed the gradient start from the body color.
            PushFillColorToRenderable();
            SyncGradientStart();
#endif

            Width = 50;
            Height = 50;
#endif
        }
    }
}
