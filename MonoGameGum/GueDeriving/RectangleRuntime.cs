using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;

#if XNALIKE
using Gum.Converters;
#endif

#if RAYLIB
using Gum.Renderables;
using Color = Raylib_cs.Color;
using ColorExtensions = RaylibGum.Helpers.ColorExtensions;
using ContainedLineRectangle = Gum.Renderables.LineRectangle;
#elif SOKOL
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
    [Obsolete("Use StrokeWidth instead. Bypasses unit handling — preserves pre-#2768 semantics.")]
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
    /// Obsolete: Apos.Shapes exposes dashed strokes via <c>StrokeDashLength</c> /
    /// <c>StrokeGapLength</c>. <c>IsDotted</c> is preserved on the core default
    /// (<see cref="LineRectangle.IsDotted"/>) for back-compat — when the stroke slot is not a
    /// <c>LineRectangle</c> the setter is a no-op.
    /// </summary>
#if XNALIKE
    [Obsolete("Use AposShapeRuntime.StrokeDashLength + StrokeGapLength on the optional MonoGameGumShapes package for cross-backend dashed strokes.")]
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
    Color? _fillColor;

    /// <summary>
    /// Color of the filled rectangle. <c>null</c> hides the fill (alpha 0). Pushed to the
    /// fill slot when non-null. Both core (<see cref="DefaultFilledRectangleRenderable"/>)
    /// and MonoGameGumShapes (<c>RoundedRectangle</c> with <c>IsFilled = true</c>) honor this.
    /// </summary>
    public Color? FillColor
    {
        get => _fillColor;
        set
        {
            _fillColor = value;
            if (_fill != null)
            {
                _fill.Color = value ?? new Color(0, 0, 0, 0);
            }
            NotifyPropertyChanged();
        }
    }

    Color? _strokeColor;

    /// <summary>
    /// Color of the outline. <c>null</c> hides the stroke (alpha 0). The stroke slot is
    /// always non-null on XNA-like backends — core ships
    /// <see cref="DefaultStrokedRectangleRenderable"/> as the default.
    /// </summary>
    public Color? StrokeColor
    {
        get => _strokeColor;
        set
        {
            _strokeColor = value;
            _stroke.Color = value ?? new Color(0, 0, 0, 0);
            NotifyPropertyChanged();
        }
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
    /// Reserved for future ScreenPixel parity with Skia — currently the value round-trips on
    /// the runtime but Apos.Shapes' RoundedRectangle ignores it (the rendered radius is the
    /// raw pixel value).
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
    // DropshadowTarget — pushing to both would double up the visible shadow.

    IDropshadowRenderable? DropshadowTarget =>
        _fill as IDropshadowRenderable ?? _stroke as IDropshadowRenderable;

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

    float _dropshadowBlurX;
    public float DropshadowBlurX
    {
        get => _dropshadowBlurX;
        set
        {
            _dropshadowBlurX = value;
            if (DropshadowTarget is { } target) target.DropshadowBlurX = value;
            NotifyPropertyChanged();
        }
    }

    float _dropshadowBlurY;
    public float DropshadowBlurY
    {
        get => _dropshadowBlurY;
        set
        {
            _dropshadowBlurY = value;
            if (DropshadowTarget is { } target) target.DropshadowBlurY = value;
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
        _stroke.StrokeWidth = strokeWidth;

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
        return toReturn;
    }
#endif

#if SKIA
    /// <summary>
    /// Routes SkiaShapeRuntime solid/gradient/stroke/dropshadow accessors to the
    /// contained RoundedRectangle. Issue #2814 / #2818.
    /// </summary>
    protected override RenderableShapeBase ContainedRenderable => ContainedLineRectangle;

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

            _stroke.Color = Color.White;
            if (_fill != null)
            {
                _fill.Color = new Color(0, 0, 0, 0);
            }
            Width = 50;
            Height = 50;

            // Issue #2818 (mirror of CircleRuntime #2797): pre-seed opaque-black dropshadow
            // with a slight downward offset/blur so toggling HasDropshadow = true at runtime
            // produces a visible shadow without further setup.
            DropshadowAlpha = 255;
            DropshadowOffsetY = 3;
            DropshadowBlurY = 3;

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

            // Defaults: invisible fill, white stroke - supplies RectangleRuntime historical
            // outline-only visual, now via the dedicated stroke slot.
            FillColor = null;
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

            Width = 50;
            Height = 50;
#endif
        }
    }
}
