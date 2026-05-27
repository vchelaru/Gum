using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;

#if XNALIKE
using Gum.Converters;
#endif

#if RAYLIB
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
using MonoGameGum.Renderables;
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

    /// <summary>
    /// Obsolete: use <see cref="StrokeWidth"/>. Legacy pre-collapse setter that writes the
    /// stroke renderable's stroke width directly, bypassing <see cref="StrokeWidthUnits"/>.
    /// </summary>
#if XNALIKE
    [Obsolete("Renamed to StrokeWidth in #2768 for cross-backend naming parity. Functional behavior is unchanged; switch to StrokeWidth to also pick up StrokeWidthUnits scaling.")]
    public float LineWidth
    {
       get => _stroke.StrokeWidth;
       set
       {
           _stroke.StrokeWidth = value;
           NotifyPropertyChanged();
       }
    }
#elif !SKIA
    // SKIA: SkiaShapeRuntime.StrokeWidth supersedes; #2814.
    [Obsolete("Renamed to StrokeWidth in #2757 for cross-backend naming parity. Functional behavior is unchanged; switch to StrokeWidth to also pick up StrokeWidthUnits scaling.")]
    public float LineWidth
    {
       get => ContainedLineRectangle.LinePixelWidth;
       set
       {
           ContainedLineRectangle.LinePixelWidth = value;
           NotifyPropertyChanged();
       }
    }
#endif

    /// <summary>
    /// Obsolete: superseded by the <see cref="StrokeDashLength"/> / <see cref="StrokeGapLength"/>
    /// pair for cross-backend naming parity. On MG/Raylib the underlying <c>LineRectangle</c>
    /// only has a binary dotted texture; on XNALIKE Apos.Shapes adds true per-segment dashes
    /// through the stroke slot, on Skia <c>SKPathEffect.CreateDash</c> consumes the lengths
    /// verbatim.
    /// </summary>
#if XNALIKE
    [Obsolete("Renamed to StrokeDashLength + StrokeGapLength in #2768 for cross-backend parity. With the optional MonoGameGumShapes package the lengths drive true per-segment dashes; without it the core LineRectangle stroke shows the binary dotted texture.")]
    public bool IsDotted
    {
        get => _stroke is LineRectangle lr && lr.IsDotted;
        set
        {
            if (_stroke is LineRectangle lr)
            {
                lr.IsDotted = value;
            }
            NotifyPropertyChanged();
        }
    }
#elif !SKIA
    [Obsolete("Renamed to StrokeDashLength + StrokeGapLength in #2757 for cross-backend parity. On MG/Raylib the visual is a fixed-pattern dotted texture (LineRectangle has no per-segment dash control); set both new properties to non-zero values to engage the same dotted pattern.")]
    public bool IsDotted
    {
        get => ContainedLineRectangle.IsDotted;
        set
        {
            ContainedLineRectangle.IsDotted = value;
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
        float strokeWidth = _strokeWidth;
        if (_strokeWidthUnits == DimensionUnitType.ScreenPixel)
        {
            var camera = this.EffectiveManagers?.Renderer?.Camera;
            if (camera != null)
            {
                strokeWidth /= camera.Zoom;
            }
        }
        ContainedLineRectangle.LinePixelWidth = strokeWidth;
    }
#endif

#if RAYLIB
    // Issue #2757 — raylib rectangle parity: surface the same property names the XNALIKE/SKIA
    // branches expose so the cross-backend RectanglesScreen sample compiles against raylib. The
    // setters push through to the contained LineRectangle, whose Render() handles fill/stroke/
    // gradient/dropshadow composition. SOKOL is not extended here yet — its renderable doesn't
    // implement any of this; when it gains support, lift the relevant blocks into RAYLIB ||
    // SOKOL (mirror the CircleRuntime pattern).

    // Issue #2938 regression fix — transparent default so a freshly-constructed RectangleRuntime
    // renders as a stroke-only outline (matches pre-#2938 visual the gallery / cross-backend
    // samples assume).
    Color _fillColor = new Color((byte)0, (byte)0, (byte)0, (byte)0);

    /// <inheritdoc cref="Gum.Renderables.LineRectangle.FillColor"/>
    /// <remarks>
    /// Issue #2938 — non-nullable since the visibility gate moved to <see cref="IsFilled"/>.
    /// Defaults to transparent (alpha 0) so a freshly-constructed runtime renders a
    /// stroke-only outline; IsFilled is true by default so assigning a visible color lights
    /// up the fill without flipping IsFilled.
    /// </remarks>
    public Color FillColor
    {
        get => _fillColor;
        set
        {
            _fillColor = value;
            ContainedLineRectangle.FillColor = value;
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

    Color _strokeColor = new Color((byte)255, (byte)255, (byte)255, (byte)255);

    /// <inheritdoc cref="Gum.Renderables.LineRectangle.StrokeColor"/>
    public Color StrokeColor
    {
        get => _strokeColor;
        set
        {
            _strokeColor = value;
            ContainedLineRectangle.StrokeColor = value;
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

    /// <inheritdoc cref="Gum.Renderables.LineRectangle.Color1"/>
    public Color Color1
    {
        get => ContainedLineRectangle.Color1;
        set
        {
            ContainedLineRectangle.Color1 = value;
            NotifyPropertyChanged();
        }
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
#endif

    /// <summary>
    /// Obsolete: use <see cref="FillColor"/> or <see cref="StrokeColor"/>. Legacy member that
    /// writes the alpha channel of the stroke renderable's color slot.
    /// </summary>
#if XNALIKE
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2768.")]
    public int Alpha
    {
        get => _stroke.Color.A;
        set
        {
            Color current = _stroke.Color;
            _stroke.Color = new Color(current.R, current.G, current.B, (byte)value);
            NotifyPropertyChanged();
        }
    }
#elif !SKIA
    public int Alpha
    {
        get => ContainedLineRectangle.Color.A;
        set
        {
            ContainedLineRectangle.Color = ColorExtensions.WithAlpha(ContainedLineRectangle.Color, (byte)value);
            NotifyPropertyChanged();
        }
    }
#endif

    /// <inheritdoc cref="Alpha"/>
#if XNALIKE
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2768.")]
    public int Red
    {
        get => _stroke.Color.R;
        set
        {
            Color current = _stroke.Color;
            _stroke.Color = new Color((byte)value, current.G, current.B, current.A);
            NotifyPropertyChanged();
        }
    }
#elif !SKIA
    public int Red
    {
        get => ContainedLineRectangle.Color.R;
        set
        {
            ContainedLineRectangle.Color = ColorExtensions.WithRed(ContainedLineRectangle.Color, (byte)value);
            NotifyPropertyChanged();
        }
    }
#endif

    /// <inheritdoc cref="Alpha"/>
#if XNALIKE
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2768.")]
    public int Green
    {
        get => _stroke.Color.G;
        set
        {
            Color current = _stroke.Color;
            _stroke.Color = new Color(current.R, (byte)value, current.B, current.A);
            NotifyPropertyChanged();
        }
    }
#elif !SKIA
    public int Green
    {
        get => ContainedLineRectangle.Color.G;
        set
        {
            ContainedLineRectangle.Color = ColorExtensions.WithGreen(ContainedLineRectangle.Color, (byte)value);
            NotifyPropertyChanged();
        }
    }
#endif

    /// <inheritdoc cref="Alpha"/>
#if XNALIKE
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2768.")]
    public int Blue
    {
        get => _stroke.Color.B;
        set
        {
            Color current = _stroke.Color;
            _stroke.Color = new Color(current.R, current.G, (byte)value, current.A);
            NotifyPropertyChanged();
        }
    }
#elif !SKIA
    public int Blue
    {
        get => ContainedLineRectangle.Color.B;
        set
        {
            ContainedLineRectangle.Color = ColorExtensions.WithBlue(ContainedLineRectangle.Color, (byte)value);
            NotifyPropertyChanged();
        }
    }
#endif

    /// <summary>
    /// Obsolete: use <see cref="FillColor"/> or <see cref="StrokeColor"/>. Routes to the
    /// stroke slot for back-compat — <see cref="RectangleRuntime"/> was historically
    /// outline-only.
    /// </summary>
#if !SKIA
    // SkiaShapeRuntime base supplies an obsolete Color pass-through under SKIA. #2814.
    public Color Color
    {
#if XNALIKE
        [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2768.")]
        get => _stroke.Color;
        [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2768.")]
        set
        {
            _stroke.Color = value;
            NotifyPropertyChanged();
        }
#elif RAYLIB || SOKOL
        get => ContainedLineRectangle.Color;
        set
        {
            ContainedLineRectangle.Color = value;
            NotifyPropertyChanged();
        }
#else
        get => global::RenderingLibrary.Graphics.XNAExtensions.ToXNA(ContainedLineRectangle.Color);
        set
        {
            ContainedLineRectangle.Color = global::RenderingLibrary.Graphics.XNAExtensions.ToSystemDrawing(value);
            NotifyPropertyChanged();
        }
#endif
    }
#endif

#if XNALIKE
    // Issue #2938 regression fix: FillColor defaults to transparent (alpha 0) so a freshly-
    // constructed RectangleRuntime renders as a stroke-only outline — matching the pre-#2938
    // visual that existing sample code (gallery frames) assumes. IsFilled is true by default;
    // assigning FillColor to a visible color lights up the fill without flipping IsFilled.
    Color _fillColor = new Color(0, 0, 0, 0);

    /// <summary>
    /// Color of the filled rectangle. Pushed to the fill slot when <see cref="IsFilled"/> is
    /// <c>true</c>; when <c>IsFilled</c> is <c>false</c> the fill slot is pushed a transparent
    /// color so only the stroke draws. Both core (<see cref="DefaultFilledRectangleRenderable"/>)
    /// and MonoGameGumShapes (<c>RoundedRectangle</c> with <c>IsFilled = true</c>) honor this.
    /// Defaults to transparent so a freshly-constructed runtime renders a stroke-only outline.
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

    bool _isFilled = true;

    /// <summary>
    /// Gates fill rendering. When <c>true</c> (the default) the fill slot is painted with
    /// <see cref="FillColor"/>. When <c>false</c> the fill slot is pushed a transparent color
    /// so only the stroke draws — used to render a stroke-only outline without dropping
    /// <see cref="FillColor"/>. Stroke visibility is gated separately by
    /// <see cref="StrokeWidth"/> (0 hides stroke).
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

    bool _useGradient;
    /// <inheritdoc cref="CircleRuntime.UseGradient"/>
    public bool UseGradient
    {
        get => _useGradient;
        set
        {
            _useGradient = value;
            if (_fill is IGradientedRenderable fillGrad) fillGrad.UseGradient = value;
            if (_stroke is IGradientedRenderable strokeGrad) strokeGrad.UseGradient = value;
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

    int _alpha1 = 255;
    public int Alpha1
    {
        get => _alpha1;
        set
        {
            _alpha1 = value;
            if (_fill is IGradientedRenderable fillGrad) fillGrad.Alpha1 = value;
            if (_stroke is IGradientedRenderable strokeGrad) strokeGrad.Alpha1 = value;
            NotifyPropertyChanged();
        }
    }

    int _red1;
    public int Red1
    {
        get => _red1;
        set
        {
            _red1 = value;
            if (_fill is IGradientedRenderable fillGrad) fillGrad.Red1 = value;
            if (_stroke is IGradientedRenderable strokeGrad) strokeGrad.Red1 = value;
            NotifyPropertyChanged();
        }
    }

    int _green1;
    public int Green1
    {
        get => _green1;
        set
        {
            _green1 = value;
            if (_fill is IGradientedRenderable fillGrad) fillGrad.Green1 = value;
            if (_stroke is IGradientedRenderable strokeGrad) strokeGrad.Green1 = value;
            NotifyPropertyChanged();
        }
    }

    int _blue1;
    public int Blue1
    {
        get => _blue1;
        set
        {
            _blue1 = value;
            if (_fill is IGradientedRenderable fillGrad) fillGrad.Blue1 = value;
            if (_stroke is IGradientedRenderable strokeGrad) strokeGrad.Blue1 = value;
            NotifyPropertyChanged();
        }
    }

    public Color Color1
    {
        get => new Color(_red1, _green1, _blue1, _alpha1);
        set
        {
            Red1 = value.R;
            Green1 = value.G;
            Blue1 = value.B;
            Alpha1 = value.A;
        }
    }

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
        float strokeWidth = _strokeWidth;
        float strokeDashLength = _strokeDashLength;
        float strokeGapLength = _strokeGapLength;
        if (_strokeWidthUnits == DimensionUnitType.ScreenPixel)
        {
            var camera = this.EffectiveManagers?.Renderer?.Camera;
            if (camera != null)
            {
                // Mirror AposShapeRuntime.PreRender — dash and gap scale alongside stroke width.
                strokeWidth /= camera.Zoom;
                strokeDashLength /= camera.Zoom;
                strokeGapLength /= camera.Zoom;
            }
        }
        // Issue #2818 (mirror of CircleRuntime #2790) — Apos.Shapes' DrawRectangle adds aaSize
        // pixels of AA halo OUTSIDE the nominal thickness, same shape as DrawCircle. Skia fits
        // its AA WITHIN the thickness, so without this compensation the same user-set
        // StrokeWidth reads ~1 px wider on Apos. Subtract the 1 px AA contribution before
        // pushing. Floored at a tiny positive epsilon (not 0) — Apos's shader treats
        // thickness = 0 as "don't draw", and the 1 px halo dominates the visible width so the
        // sub-pixel under-draw of the nominal stroke is invisible. Gated by
        // IAntialiasedRenderable so the core stroke default (LineRectangle wrapper, no AA
        // concept) still receives the raw value. Visible-thickness parity for axis-aligned
        // straights is the headline — rounded corner arcs still get the AA they need.
        const float aposAaContribution = 1f;
        const float aposMinThicknessEpsilon = 0.01f;
        float renderableStrokeWidth = strokeWidth;
        if (_isAntialiased && _stroke is IAntialiasedRenderable)
        {
            renderableStrokeWidth = Math.Max(aposMinThicknessEpsilon, strokeWidth - aposAaContribution);
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
            var camera = this.EffectiveManagers?.Renderer?.Camera;
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
        return toReturn;
    }
#endif

#if SKIA
    /// <summary>
    /// Routes SkiaShapeRuntime solid/gradient/stroke/dropshadow accessors to the
    /// contained RoundedRectangle. Issue #2814 / #2818.
    /// </summary>
    protected override RenderableShapeBase ContainedRenderable => ContainedLineRectangle;

    /// <inheritdoc cref="CircleRuntime.DropshadowBlur"/>
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

            // Initial defaults — stroke white, fill transparent (IsFilled = true so the gate is
            // open, but FillColor alpha = 0 so nothing visible draws), layout 50x50. Issue
            // #2938 (regression fix): a freshly-constructed RectangleRuntime renders as a
            // stroke-only outline — matching the pre-#2938 visual that existing sample code
            // (gallery frames, sample backgrounds) assumes. Assigning FillColor to a visible
            // color lights up the fill without flipping IsFilled.
            _stroke.Color = _strokeColor;
            if (_fill != null)
            {
                _fill.Color = _isFilled ? _fillColor : new Color(0, 0, 0, 0);
            }
            Width = 50;
            Height = 50;

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

            // Defaults: transparent fill, white stroke — RectangleRuntime's historical
            // outline-only visual, now expressed as IsFilled = true (base default) + FillColor
            // alpha 0. Symmetric with CircleRuntime's Skia branch: assigning FillColor to a
            // visible color lights up the fill without flipping IsFilled. Pre-#2938 the Skia
            // branch flipped IsFilled = false explicitly; that broke gallery code which does
            // `frame.FillColor = darkGray;` without setting IsFilled.
            //
            // FillColor must be explicitly assigned here even though the field default is
            // transparent: SkiaShapeRuntime.PushFillColorToSlot only runs from the FillColor /
            // IsFilled property setters, never from field init. Without this line the Skia
            // RoundedRectangle renderable retains its own constructor default (white) and the
            // rectangle renders as a solid white block. Mirrors CircleRuntime's Skia ctor.
            FillColor = new SKColor(0, 0, 0, 0);
            StrokeColor = SKColors.White;
            StrokeWidth = 1;
            StrokeWidthUnits = DimensionUnitType.ScreenPixel;

            // Dropshadow off by default; pre-seed alpha/offset/blur so toggling HasDropshadow =
            // true at runtime produces a visible shadow without further setup.
            DropshadowAlpha = 255;
            DropshadowOffsetY = 3;
            DropshadowBlurY = 3;

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
            // #2938 (regression fix) — push runtime-held FillColor / StrokeColor / IsFilled
            // defaults onto the renderable so the runtime properties report consistent state at
            // construction. FillColor defaults to transparent and IsFilled defaults to true →
            // a fresh rectangle renders as a stroke-only outline (matching the pre-#2938
            // visual that existing sample code assumes).
            rectangle.StrokeColor = _strokeColor;
            rectangle.FillColor = _fillColor;
            rectangle.IsFilled = true;
#endif

            Width = 50;
            Height = 50;
#endif
        }
    }
}
