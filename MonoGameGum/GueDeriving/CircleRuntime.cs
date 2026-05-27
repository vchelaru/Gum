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
using ContainedCircleType = Gum.Renderables.LineCircle;
#elif SOKOL
using Gum.DataTypes;
using Gum.Renderables;
using Color = SokolGum.Color;
using ContainedCircleType = Gum.Renderables.LineCircle;
#elif SKIA
using Gum.DataTypes;
using SkiaGum.Renderables;
using SkiaSharp;
using Color = SkiaSharp.SKColor;
using ContainedCircleType = SkiaGum.Renderables.Circle;
#else
using Color = Microsoft.Xna.Framework.Color;
using ColorExtensions = ToolsUtilitiesStandard.Helpers.ColorExtensions;
using ContainedCircleType = global::RenderingLibrary.Math.Geometry.LineCircle;
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
/// Runtime wrapping the fill + stroke renderable pair that draws a circle. Under XNA-likes
/// (MonoGame/FNA/KNI), each slot is resolved once at construction by
/// <see cref="RenderableRegistry"/> and kept for life — no per-property swap.
/// </summary>
/// <remarks>
/// <para>
/// Issue #2768 replaces the single-renderable Phase 2 model from #2761 with a two-slot model
/// (<see cref="IFilledCircleRenderable"/> + <see cref="IStrokedCircleRenderable"/>) so a
/// single runtime can draw fill and outline simultaneously. Core MonoGameGum ships only a
/// stroke default (<see cref="DefaultStrokedCircleRenderable"/>); without the optional
/// MonoGameGumShapes / Apos.Shapes package the fill slot resolves to <c>null</c> and
/// <see cref="FillColor"/> setters are no-ops. Backing fields still round-trip so user code
/// is forward-compatible with adding the package later. Backends other than XNA-like are
/// still on the single <c>LineCircle</c> model — see issue #2761's "out of scope" list.
/// </para>
/// <para>
/// Containment: when both slots exist, <c>_fill</c> is the contained object and <c>_stroke</c>
/// is its first child. The renderer draws parent before children, so the visual order is fill
/// under stroke under user-added children. When the fill slot resolves to <c>null</c>,
/// <c>_stroke</c> becomes the contained object directly.
/// </para>
/// </remarks>
#if SKIA
public class CircleRuntime : SkiaShapeRuntime
#else
public class CircleRuntime : GraphicalUiElement
#endif
{
#if XNALIKE
    IFilledCircleRenderable? _fill;
    IStrokedCircleRenderable _stroke = null!;
#else
    ContainedCircleType containedLineCircle = null!;

    ContainedCircleType ContainedLineCircle
    {
        get
        {
            if (containedLineCircle == null)
            {
                containedLineCircle = (ContainedCircleType)this.RenderableComponent!;
            }
            return containedLineCircle;
        }
    }
#endif

    /// <summary>
    /// Obsolete: use <see cref="FillColor"/> or <see cref="StrokeColor"/>. Legacy member that
    /// writes the alpha channel of the stroke renderable's color slot directly.
    /// </summary>
#if XNALIKE
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2761.")]
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
        get => ContainedLineCircle.Color.A;
        set
        {
            ContainedLineCircle.Color = ColorExtensions.WithAlpha(ContainedLineCircle.Color, (byte)value);
            NotifyPropertyChanged();
        }
    }
#endif

    /// <summary>
    /// Obsolete: use <see cref="FillColor"/> or <see cref="StrokeColor"/>. Legacy member that
    /// writes the red channel of the stroke renderable's color slot directly.
    /// </summary>
#if XNALIKE
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2761.")]
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
        get => ContainedLineCircle.Color.R;
        set
        {
            ContainedLineCircle.Color = ColorExtensions.WithRed(ContainedLineCircle.Color, (byte)value);
            NotifyPropertyChanged();
        }
    }
#endif

    /// <summary>
    /// Obsolete: use <see cref="FillColor"/> or <see cref="StrokeColor"/>. Legacy member that
    /// writes the green channel of the stroke renderable's color slot directly.
    /// </summary>
#if XNALIKE
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2761.")]
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
        get => ContainedLineCircle.Color.G;
        set
        {
            ContainedLineCircle.Color = ColorExtensions.WithGreen(ContainedLineCircle.Color, (byte)value);
            NotifyPropertyChanged();
        }
    }
#endif

    /// <summary>
    /// Obsolete: use <see cref="FillColor"/> or <see cref="StrokeColor"/>. Legacy member that
    /// writes the blue channel of the stroke renderable's color slot directly.
    /// </summary>
#if XNALIKE
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2761.")]
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
        get => ContainedLineCircle.Color.B;
        set
        {
            ContainedLineCircle.Color = ColorExtensions.WithBlue(ContainedLineCircle.Color, (byte)value);
            NotifyPropertyChanged();
        }
    }
#endif

    public float Radius
    {
#if XNALIKE
        get => _stroke.Radius;
        set
        {
            mWidth = value * 2;
            mHeight = value * 2;
            _stroke.Radius = value;
            if (_fill != null) _fill.Radius = value;
        }
#else
        get => ContainedLineCircle.Radius;
        set
        {
            mWidth = value * 2;
            mHeight = value * 2;
            ContainedLineCircle.Radius = value;
        }
#endif
    }

    /// <summary>
    /// Obsolete: use <see cref="FillColor"/> or <see cref="StrokeColor"/>. Legacy member that
    /// writes the stroke renderable's color slot directly. Routes to stroke for back-compat —
    /// <see cref="CircleRuntime"/> was historically outline-only.
    /// </summary>
#if XNALIKE
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2761.")]
    public Color Color
    {
        get => _stroke.Color;
        set
        {
            _stroke.Color = value;
            NotifyPropertyChanged();
        }
    }
#elif !SKIA
    public Color Color
    {
        get => ContainedLineCircle.Color;
        set
        {
            ContainedLineCircle.Color = value;
            NotifyPropertyChanged();
        }
    }
#endif

#if RAYLIB
    // Issue #2757 — surface the same property names the XNALIKE/SKIA branches expose so the
    // cross-backend Circle gallery compiles against raylib too. Setters push to the contained
    // LineCircle, whose Render() handles the fill/stroke/gradient composition. Visual support
    // is intentionally a subset of MG/Skia in this pass — linear gradients, dashed strokes,
    // dropshadow, per-shape AA, and StrokeWidthUnits scaling are not yet implemented on the
    // raylib renderable (tracked as #2757 follow-ups). The runtime does not expose those
    // properties at all yet; the raylib sample (CirclesScreen) renders only the supported
    // sections.

    // Issue #2938 regression fix — transparent default so a freshly-constructed CircleRuntime
    // renders as a stroke-only outline (existing raylib gallery / cross-backend samples).
    Color _fillColor = new Color((byte)0, (byte)0, (byte)0, (byte)0);

    /// <inheritdoc cref="Gum.Renderables.LineCircle.FillColor"/>
    public Color FillColor
    {
        get => _fillColor;
        set
        {
            _fillColor = value;
            ContainedLineCircle.FillColor = value;
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

    /// <inheritdoc cref="Gum.Renderables.LineCircle.StrokeColor"/>
    public Color StrokeColor
    {
        get => _strokeColor;
        set
        {
            _strokeColor = value;
            ContainedLineCircle.StrokeColor = value;
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

    /// <inheritdoc cref="Gum.Renderables.LineCircle.IsFilled"/>
    public bool IsFilled
    {
        get => ContainedLineCircle.IsFilled;
        set
        {
            ContainedLineCircle.IsFilled = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="Gum.Renderables.LineCircle.UseGradient"/>
    public bool UseGradient
    {
        get => ContainedLineCircle.UseGradient;
        set
        {
            ContainedLineCircle.UseGradient = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="Gum.Renderables.LineCircle.GradientType"/>
    public GradientType GradientType
    {
        get => ContainedLineCircle.GradientType;
        set
        {
            ContainedLineCircle.GradientType = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="Gum.Renderables.LineCircle.Color1"/>
    public Color Color1
    {
        get => ContainedLineCircle.Color1;
        set
        {
            ContainedLineCircle.Color1 = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="Gum.Renderables.LineCircle.Color2"/>
    public Color Color2
    {
        get => ContainedLineCircle.Color2;
        set
        {
            ContainedLineCircle.Color2 = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="Gum.Renderables.LineCircle.GradientX1"/>
    public float GradientX1
    {
        get => ContainedLineCircle.GradientX1;
        set
        {
            ContainedLineCircle.GradientX1 = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="Gum.Renderables.LineCircle.GradientY1"/>
    public float GradientY1
    {
        get => ContainedLineCircle.GradientY1;
        set
        {
            ContainedLineCircle.GradientY1 = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="Gum.Renderables.LineCircle.GradientX2"/>
    public float GradientX2
    {
        get => ContainedLineCircle.GradientX2;
        set
        {
            ContainedLineCircle.GradientX2 = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="Gum.Renderables.LineCircle.GradientY2"/>
    public float GradientY2
    {
        get => ContainedLineCircle.GradientY2;
        set
        {
            ContainedLineCircle.GradientY2 = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="Gum.Renderables.LineCircle.GradientInnerRadius"/>
    public float GradientInnerRadius
    {
        get => ContainedLineCircle.GradientInnerRadius;
        set
        {
            ContainedLineCircle.GradientInnerRadius = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc cref="Gum.Renderables.LineCircle.GradientOuterRadius"/>
    public float GradientOuterRadius
    {
        get => ContainedLineCircle.GradientOuterRadius;
        set
        {
            ContainedLineCircle.GradientOuterRadius = value;
            NotifyPropertyChanged();
        }
    }
#endif

#if XNALIKE
    // Issue #2938 regression fix: FillColor defaults to transparent (alpha 0) so a freshly-
    // constructed CircleRuntime renders as a stroke-only outline — matching the pre-#2938
    // visual that all existing sample code assumes ("construct + only set StrokeColor").
    // IsFilled is true by default; the gate semantics still work — explicitly setting
    // FillColor to a visible color lights the fill up. Setting IsFilled = false zeros the
    // fill slot's alpha regardless of FillColor.
    Color _fillColor = new Color(0, 0, 0, 0);

    /// <summary>
    /// Color of the filled disk. Painted into the fill slot when <see cref="IsFilled"/> is
    /// <c>true</c>; ignored visually when <c>IsFilled</c> is <c>false</c> (the fill slot is
    /// pushed a transparent color so only the stroke draws). Defaults to transparent so a
    /// freshly-constructed runtime renders a stroke-only outline; assign a visible color to
    /// light the fill up (IsFilled is already true by default).
    /// </summary>
    /// <remarks>
    /// Visual fill requires a fill-capable <see cref="IFilledCircleRenderable"/> implementation —
    /// supplied by the optional MonoGameGumShapes (Apos.Shapes) package. Without that package
    /// the fill slot is <c>null</c> and this setter is a visual no-op; the backing field still
    /// round-trips so getter results are consistent and a later install of MonoGameGumShapes
    /// (re-creating the runtime) will honor the stored color.
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
            // Dropshadow routing depends on IsFilled — re-push so the active slot owns the
            // shadow flag and the inactive slot releases it. Otherwise toggling IsFilled
            // either ghosts the previous target or never wakes the new one up.
            SyncDropshadowToTarget();
            NotifyPropertyChanged();
        }
    }

    Color _strokeColor = Color.White;

    /// <summary>
    /// Color of the outline. Defaults to white so a freshly-constructed CircleRuntime renders
    /// the same visible outline as legacy code did. Set <see cref="StrokeWidth"/> to 0 to hide
    /// the stroke. The stroke slot is always non-null on supported backends — core ships
    /// <see cref="DefaultStrokedCircleRenderable"/> as the default.
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

    /// <summary>
    /// Width of the outline. Held on the runtime alongside <see cref="StrokeWidthUnits"/> so
    /// ScreenPixel scaling can be re-resolved against the current camera zoom each frame in
    /// <see cref="PreRender"/>. Pushed to the stroke slot each frame; ignored by the core
    /// default (which has no stroke-width concept).
    /// </summary>
    public float StrokeWidth
    {
        get => _strokeWidth;
        set
        {
            _strokeWidth = value;
            NotifyPropertyChanged();
        }
    }

    bool _isAntialiased = true;

    /// <summary>
    /// When <c>true</c> (the default, matching Apos.Shapes) the circle's edge is rendered
    /// with 1 px of anti-aliasing. When <c>false</c> the AA radius drops to 0 for crisp
    /// rasterization — useful for pixel-art / retro themes (Win95 dotted focus rect, hairline
    /// borders, 1 px dash/gap patterns where AA bloom widens a nominal 1 px stroke).
    /// </summary>
    /// <remarks>
    /// Pushed to both fill and stroke slots each frame in <see cref="PreRender"/> via
    /// <see cref="IAntialiasedRenderable"/>. Visual effect requires the optional
    /// MonoGameGumShapes (Apos.Shapes) package; without it neither slot implements the
    /// interface and the value round-trips on the runtime but renders as a no-op —
    /// <see cref="MonoGameGum.Renderables.DefaultStrokedCircleRenderable"/> wraps
    /// <c>LineCircle</c>, which has no AA concept.
    /// </remarks>
    public bool IsAntialiased
    {
        get => _isAntialiased;
        set
        {
            _isAntialiased = value;
            NotifyPropertyChanged();
        }
    }

    DimensionUnitType _strokeWidthUnits;

    /// <summary>
    /// Unit of measurement for <see cref="StrokeWidth"/>. <c>Absolute</c> means world-space
    /// pixels; <c>ScreenPixel</c> divides by the camera zoom each frame so the stroke holds
    /// a constant on-screen size.
    /// </summary>
    public DimensionUnitType StrokeWidthUnits
    {
        get => _strokeWidthUnits;
        set
        {
            _strokeWidthUnits = value;
            NotifyPropertyChanged();
        }
    }

    #region Gradient

    // Issue #2791: gradient pass-through. Backing fields live on the runtime so values
    // round-trip even when neither slot implements IGradientedRenderable (e.g. core-only stroke
    // = DefaultStrokedCircleRenderable, no fill). Setters push to whichever slot(s) implement
    // it. Pushing to both slots matches Skia's single-renderable behavior, where fill mode and
    // dashed-stroke mode both consult the same gradient parameters; an Apos-backed
    // CircleRuntime with both FillColor and StrokeColor + UseGradient = true renders a
    // gradient-filled disk with a gradient stroke sharing the same gradient.

    bool _useGradient;
    /// <summary>
    /// When <c>true</c>, the gradient color/coordinate properties drive rendering instead of
    /// <see cref="FillColor"/> / <see cref="StrokeColor"/>. Visual effect requires the optional
    /// MonoGameGumShapes (Apos.Shapes) package; without it the value round-trips but does not
    /// render.
    /// </summary>
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

    // Issue #2797: dropshadow pass-through. Backing fields live on the runtime so values
    // round-trip even when neither slot implements IDropshadowRenderable (core-only stroke
    // = DefaultStrokedCircleRenderable, no fill). Unlike gradient (#2791) and AA (#2798),
    // which push to BOTH slots so a single setter covers fill and stroke, the shadow is
    // drawn once per renderable — pushing to both would render the shadow twice and
    // visibly double up. So the routing picks one slot: the fill when IsFilled = true (the
    // disc casts the shadow), the stroke when IsFilled = false (the disc is gated to
    // transparent and can't cast a visible shadow). The push-target helper is shared across
    // every setter so the rule lives in one place. SyncDropshadowToTarget below re-routes
    // shadow state when IsFilled toggles — without that the previous target keeps its
    // HasDropshadow flag set and the new target never receives it.

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
            target.DropshadowBlurX = _dropshadowBlurX;
            target.DropshadowBlurY = _dropshadowBlurY;
        }
    }

    bool _hasDropshadow;
    /// <summary>
    /// When <c>true</c>, the dropshadow color/offset/blur properties drive an extra render
    /// pass behind the circle. Visual effect requires the optional MonoGameGumShapes
    /// (Apos.Shapes) package; without it the value round-trips but does not render.
    /// </summary>
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
    /// <inheritdoc cref="SkiaGum.GueDeriving.SkiaShapeRuntime.DropshadowBlurX"/>
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
    /// <inheritdoc cref="SkiaGum.GueDeriving.SkiaShapeRuntime.DropshadowBlurX"/>
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

    // Issue #2796: dashed-stroke pass-through. Backing fields live on the runtime so values
    // round-trip even when the stroke slot does not implement IDashedStrokeRenderable (the
    // core DefaultStrokedCircleRenderable wraps LineCircle, which has no dash concept).
    // Pushed to the stroke slot only — dashing is a stroke-mode operation (Apos.Shapes'
    // Circle.RenderDashed path is guarded by !IsFilled), and the fill slot would ignore it.
    // The push happens in PreRender alongside StrokeWidth so ScreenPixel scaling stays in
    // sync with camera zoom — mirroring AposShapeRuntime.PreRender, which divides all three
    // (strokeWidth, dashLen, gapLen) by the same zoom under ScreenPixel units.

    float _strokeDashLength;
    /// <summary>
    /// Length of each dash segment in <see cref="StrokeWidthUnits"/>. A value of 0 (the
    /// default) produces a solid stroke; both <see cref="StrokeDashLength"/> and
    /// <see cref="StrokeGapLength"/> must be &gt; 0 for dashed rendering to engage.
    /// </summary>
    /// <remarks>
    /// Visual dashing requires the optional MonoGameGumShapes (Apos.Shapes) package; without
    /// it the stroke slot is <see cref="MonoGameGum.Renderables.DefaultStrokedCircleRenderable"/>
    /// (wraps <c>LineCircle</c>, no dash concept) and this setter is a visual no-op. The
    /// backing field still round-trips so user code is forward-compatible with adding the
    /// package later. Pushed each frame in <see cref="PreRender"/> with the same ScreenPixel
    /// scaling as <see cref="StrokeWidth"/>.
    /// </remarks>
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
    /// <summary>
    /// Length of each gap between dashes in <see cref="StrokeWidthUnits"/>. Ignored when
    /// <see cref="StrokeDashLength"/> is 0.
    /// </summary>
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

    /// <summary>
    /// Pushes runtime-held stroke values to the stroke renderable each frame, resolving
    /// <see cref="StrokeWidthUnits"/> against the current camera zoom. Reached through the
    /// <c>OnPreRender</c> hook the MonoGameGumShapes factory wires onto each Apos shape; the
    /// core defaults don't wire it but the value still gets pushed when layout invokes
    /// PreRender.
    /// </summary>
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
                // Mirrors AposShapeRuntime.PreRender — dash and gap scale alongside stroke
                // width so a "1 px dotted" pattern stays 1 px on screen regardless of zoom.
                strokeWidth /= camera.Zoom;
                strokeDashLength /= camera.Zoom;
                strokeGapLength /= camera.Zoom;
            }
        }
        // Issue #2790 — Apos.Shapes' DrawCircle takes a stroke thickness plus an aaSize and
        // renders aaSize pixels of antialiased halo OUTSIDE the nominal thickness. Gum/Apos's
        // Circle.Render passes aaSize = 1 when IsAntialiased is true (see
        // Runtimes/GumShapes/Renderables/Circle.cs), so Apos always contributes exactly 1 px
        // to the visible thickness. Skia fits its AA WITHIN the nominal thickness, so the
        // same user-set StrokeWidth would otherwise read 1 px wider on Apos than on Skia.
        // Subtract that 1 px before pushing. Floored at a tiny positive epsilon (not 0) as a
        // hedge against Apos's shader interpreting thickness = 0 as "don't draw"; the 1 px AA
        // halo dominates the visible width either way, so the sub-pixel under-draw of the
        // nominal stroke is invisible. Gated by IAntialiasedRenderable so the core stroke
        // default (LineCircle wrapper, no AA concept) still receives the raw value.
        const float aposAaContribution = 1f;
        const float aposMinThicknessEpsilon = 0.01f;
        float renderableStrokeWidth = strokeWidth;
        if (_isAntialiased && _stroke is IAntialiasedRenderable)
        {
            renderableStrokeWidth = Math.Max(aposMinThicknessEpsilon, strokeWidth - aposAaContribution);
        }
        _stroke.StrokeWidth = renderableStrokeWidth;

        // Issue #2796: push dash/gap to the stroke slot when it supports dashing. Skipped
        // for slots that don't implement the interface (core DefaultStrokedCircleRenderable
        // wraps LineCircle, no dash concept). Stroke-only — dashing is guarded by !IsFilled
        // in the Apos renderable, so pushing to fill would be ignored anyway. The Apos
        // renderable itself inflates the effective gap by aaSize when AA is on (see
        // Runtimes/GumShapes/Renderables/Circle.cs RenderDashed) so dashes stay visually
        // distinct — runtime just pushes the user's values through unchanged.
        if (_stroke is IDashedStrokeRenderable strokeDashed)
        {
            strokeDashed.StrokeDashLength = strokeDashLength;
            strokeDashed.StrokeGapLength = strokeGapLength;
        }

        // Issue #2798: push AA to both slots so a single setter flips fill + stroke together.
        // Mirrors AposShapeRuntime.PreRender. Skipped for slots that don't implement the
        // interface (core DefaultStrokedCircleRenderable has no AA concept).
        if (_fill is IAntialiasedRenderable fillAa) fillAa.IsAntialiased = _isAntialiased;
        if (_stroke is IAntialiasedRenderable strokeAa) strokeAa.IsAntialiased = _isAntialiased;

        // When fill is the contained object and stroke is its child, the layout system pushes
        // size onto fill but not stroke (stroke is a plain renderable, not a layout-aware
        // GraphicalUiElement). Mirror the runtime's current dimensions onto stroke each frame
        // so its render bounds match fill's. When fill is null this is redundant but cheap.
        if (_fill is IPositionedSizedObject fillSized && _stroke is IPositionedSizedObject strokeSized)
        {
            strokeSized.Width = fillSized.Width;
            strokeSized.Height = fillSized.Height;
        }

        // Issue #2834 — when both slots are visible, push a radius inset to the fill so its
        // rendered outer AA halo sits inside the stroke's opaque band. Two separate
        // antialiased Apos draws at the same radius composite their AA pixels, producing a
        // red fringe outside the white stroke (the Apos symptom; Skia shows a mirror-image
        // pink halo on the inside).
        //
        // We can't mutate fillSized.Width/Height to do this — the fill IS the runtime's
        // contained sizing object, so mutating its Width feeds back into layout and the
        // shrink accumulates each frame until the circle vanishes. Inset is pushed via the
        // dedicated FillRadiusInset property instead; Width/Height stay layout-owned, and
        // the Apos Circle subtracts the inset from its computed radius at render time.
        //
        // Inset per side = max(renderableStrokeWidth, aposAaContribution when AA on).
        // renderableStrokeWidth alone aligns fine for thick strokes, but hairline (1 px)
        // strokes push a sub-pixel epsilon to Apos so the AA halo (still ~1 px) dominates
        // and would re-create the overlap without the floor.
        //
        // Gated on stroke visibility: alpha 0 OR StrokeWidth 0 means the stroke isn't drawn,
        // and inset would render a thin background ring where the stroke would have been.
        // Issue #2938 made StrokeWidth = 0 the canonical hide-stroke gate (StrokeColor is now
        // non-nullable); the alpha guard stays as the pre-existing #2834 path.
        if (_fill != null)
        {
            float fillRadiusInset = 0f;
            if (_stroke.Color.A > 0 && _strokeWidth > 0)
            {
                fillRadiusInset = renderableStrokeWidth;
                if (_isAntialiased && _stroke is IAntialiasedRenderable)
                {
                    fillRadiusInset = Math.Max(fillRadiusInset, aposAaContribution);
                }
            }
            _fill.FillRadiusInset = fillRadiusInset;
        }

        // Do NOT call base.PreRender() — same caveat as AposShapeRuntime.PreRender. Forwarding
        // to the contained renderable's PreRender would recurse via the OnPreRender hook the
        // MonoGameGumShapes factory wires up.
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Issue #2790: rebuild both slots so the clone is fully independent of the source.
    /// <list type="number">
    /// <item><c>base.Clone</c> deep-copies the fill renderable via <c>ICloneable</c> (Apos
    /// <c>Circle.Clone</c>) and sets it as the clone's contained object. <c>MemberwiseClone</c>
    /// leaves the clone's <c>_fill</c> field pointing at the source's fill instance — we
    /// re-assign it to the freshly-cloned contained object.</item>
    /// <item>The <c>_stroke</c> field is similarly stale; re-resolve via
    /// <see cref="RenderableRegistry"/> so the new stroke instance's <c>OnPreRender</c> hook is
    /// wired against the clone, not the source.</item>
    /// <item>Re-wire the stroke's parent to the new fill so the renderer's hierarchy walk
    /// draws stroke after fill (visual order preserved).</item>
    /// <item>Push <see cref="StrokeColor"/> through its setter so the freshly-built stroke
    /// renderable picks up the user's color (the runtime's <c>_strokeColor</c> field was
    /// MemberwiseCloned but the new slot is at its default).</item>
    /// </list>
    /// </remarks>
    public override GraphicalUiElement Clone()
    {
        CircleRuntime toReturn = (CircleRuntime)base.Clone();

        toReturn._fill = (IFilledCircleRenderable?)toReturn.mContainedObjectAsIpso;
        toReturn._stroke = RenderableRegistry.Create<IStrokedCircleRenderable>(toReturn)
            ?? new DefaultStrokedCircleRenderable();

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
        if (toReturn._fill != null)
        {
            toReturn._stroke.Radius = toReturn._fill.Radius;
        }

        return toReturn;
    }
#endif

#if RAYLIB || SOKOL
    float _strokeWidth = 1;

    /// <inheritdoc cref="CircleRuntime.StrokeWidth"/>
    /// <remarks>
    /// Issue #2757 cross-backend API parity. On Raylib the value is pushed to the contained
    /// <see cref="Gum.Renderables.LineCircle"/>, which routes thick strokes through raylib's
    /// <c>DrawRing</c>. On Sokol the renderable has no thick-stroke support yet, so the value
    /// round-trips but is visually inert (same forward-compat pattern as MonoGameGum's
    /// <c>DefaultStrokedCircleRenderable</c> pre-Apos.Shapes).
    /// </remarks>
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
    /// <remarks>
    /// On Raylib (#2757) the value is pushed to the contained <see cref="Gum.Renderables.LineCircle"/>,
    /// which renders the dashed pattern as a loop of <c>DrawRing</c> arcs. On Sokol the
    /// renderable has no dash support yet, so the value round-trips but is visually inert.
    /// Skia honors the lengths verbatim; XNALIKE with MonoGameGumShapes routes through the
    /// Apos stroke slot.
    /// </remarks>
    public float StrokeDashLength
    {
        get => _strokeDashLength;
        set
        {
            _strokeDashLength = value;
#if RAYLIB
            ContainedLineCircle.StrokeDashLength = value;
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
            ContainedLineCircle.StrokeGapLength = value;
#endif
            NotifyPropertyChanged();
        }
    }

#if RAYLIB
    /// <summary>
    /// Pushes the stroke width to the contained <see cref="Gum.Renderables.LineCircle"/> each
    /// frame, resolving <see cref="StrokeWidthUnits"/> against the current camera zoom so a
    /// ScreenPixel stroke holds its on-screen pixel width regardless of zoom. Mirrors the
    /// XNALIKE <see cref="PreRender"/> above and the Rectangle/Polygon equivalents (#2757).
    /// raylib's <c>Renderer.Draw</c> walks the visual tree calling this before render so the
    /// resolved width lands in time for the first frame.
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
        ContainedLineCircle.StrokeWidth = strokeWidth;
    }
#endif

    // #2757 dropshadow on raylib (#12 follow-up). Approximated via concentric semi-transparent
    // rings on the renderable side — no shader, no render-to-texture. Anisotropic blur
    // collapses to max(BlurX, BlurY); the per-axis props are kept for API parity with Skia/MG.
    // Backing fields live on the runtime so values round-trip on Sokol (renderable has no
    // dropshadow support there); RAYLIB pushes to the renderable.

    bool _hasDropshadow;
    /// <inheritdoc cref="Gum.Renderables.LineCircle.HasDropshadow"/>
    public bool HasDropshadow
    {
        get => _hasDropshadow;
        set
        {
            _hasDropshadow = value;
#if RAYLIB
            ContainedLineCircle.HasDropshadow = value;
#endif
            NotifyPropertyChanged();
        }
    }

    Color _dropshadowColor = new Color((byte)0, (byte)0, (byte)0, (byte)255);
    /// <inheritdoc cref="Gum.Renderables.LineCircle.DropshadowColor"/>
    public Color DropshadowColor
    {
        get => _dropshadowColor;
        set
        {
            _dropshadowColor = value;
#if RAYLIB
            ContainedLineCircle.DropshadowColor = value;
#endif
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
    /// <inheritdoc cref="Gum.Renderables.LineCircle.DropshadowOffsetX"/>
    public float DropshadowOffsetX
    {
        get => _dropshadowOffsetX;
        set
        {
            _dropshadowOffsetX = value;
#if RAYLIB
            ContainedLineCircle.DropshadowOffsetX = value;
#endif
            NotifyPropertyChanged();
        }
    }

    float _dropshadowOffsetY;
    /// <inheritdoc cref="Gum.Renderables.LineCircle.DropshadowOffsetY"/>
    public float DropshadowOffsetY
    {
        get => _dropshadowOffsetY;
        set
        {
            _dropshadowOffsetY = value;
#if RAYLIB
            ContainedLineCircle.DropshadowOffsetY = value;
#endif
            NotifyPropertyChanged();
        }
    }

    float _dropshadowBlurX;
    /// <inheritdoc cref="Gum.Renderables.LineCircle.DropshadowBlurX"/>
    public float DropshadowBlurX
    {
        get => _dropshadowBlurX;
        set
        {
            _dropshadowBlurX = value;
#if RAYLIB
            ContainedLineCircle.DropshadowBlurX = value;
#endif
            NotifyPropertyChanged();
        }
    }

    float _dropshadowBlurY;
    /// <inheritdoc cref="Gum.Renderables.LineCircle.DropshadowBlurY"/>
    public float DropshadowBlurY
    {
        get => _dropshadowBlurY;
        set
        {
            _dropshadowBlurY = value;
#if RAYLIB
            ContainedLineCircle.DropshadowBlurY = value;
#endif
            NotifyPropertyChanged();
        }
    }
#endif

#if SKIA
    /// <summary>
    /// Routes <see cref="SkiaShapeRuntime"/>'s solid/gradient/stroke/dropshadow accessors
    /// to the contained <see cref="Circle"/>.
    /// </summary>
    protected override RenderableShapeBase ContainedRenderable => ContainedLineCircle;

    /// <summary>
    /// Pushes the issue #2834 fill radius inset after the base runs its stroke-width and
    /// W/H mirror. Inset equals the post-ScreenPixel-scaled stroke width (matching what the
    /// base just pushed onto the stroke slot); zero when the stroke is hidden so fill-only
    /// mode renders at full radius. Skia fits AA within the stroke thickness so no AA-bloom
    /// adjustment is needed — the user's StrokeWidth IS the rendered thickness.
    /// </summary>
    public override void PreRender()
    {
        base.PreRender();

        if (StrokeRenderable != null && ContainedLineCircle != null)
        {
            float fillRadiusInset = 0f;
            if (StrokeRenderable.Color.Alpha > 0)
            {
                fillRadiusInset = StrokeRenderable.StrokeWidth;
            }
            ContainedLineCircle.FillRadiusInset = fillRadiusInset;
        }
    }

    /// <inheritdoc/>
    public override GraphicalUiElement Clone()
    {
        CircleRuntime toReturn = (CircleRuntime)base.Clone();
        // Reset cached renderable reference so the clone re-resolves against its own
        // RenderableComponent on next access. The fill slot's Color/Radius/etc. were copied
        // by Circle.Clone (ICloneable, MemberwiseClone) so the clone's fill matches source.
        toReturn.containedLineCircle = null!;
        // Issue #2790: drop the inherited reference to the source's stroke slot and rebuild a
        // fresh one parented to the clone's fill so the clone is fully independent.
        toReturn.ClearStrokeRenderable();
        toReturn.SetStrokeRenderable(new ContainedCircleType());
        // The fresh stroke renderable defaults to red; re-fire the StrokeColor setter so the
        // user's color (held on _strokeColor via MemberwiseClone) is pushed into the new
        // slot. Same trick for Width/Height mirrored in PreRender — no need here.
        toReturn.StrokeColor = toReturn.StrokeColor;
        return toReturn;
    }
#endif

    /// <inheritdoc cref="GraphicalUiElement.AddToManagers()"/>
    [Obsolete("Use the AddToRoot extension method instead (e.g. myCircle.AddToRoot()).")]
    public new void AddToManagers() => base.AddToManagers(SystemManagers.Default, layer: null);

    public CircleRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
#if XNALIKE
            // Construct-time binding (#2768 two-slot model). Both slots are resolved once and
            // kept for the lifetime of the runtime. The fill slot may resolve to null — core
            // ships no IFilledCircleRenderable default, so without MonoGameGumShapes the fill
            // is genuinely unavailable (the design's honest graceful-degradation point).
            // TODO(#2768 follow-up): lazy if profiling shows it matters.
            _fill = RenderableRegistry.Create<IFilledCircleRenderable>(this);
            _stroke = RenderableRegistry.Create<IStrokedCircleRenderable>(this)
                ?? new DefaultStrokedCircleRenderable();

            // Containment: when fill exists, fill is the contained object and stroke is its
            // first child. The renderer draws parent before children, so fill underneath
            // stroke underneath user-added children (which append after stroke into the same
            // collection). When fill is null, stroke is the contained object directly.
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

            // Initial defaults — stroke white, fill transparent (IsFilled = true so the gate
            // is open, but the color is alpha-0 so nothing visible draws), radius 16, layout
            // 32x32. Issue #2938 (regression fix): a freshly-constructed CircleRuntime renders
            // as a stroke-only outline — matching the pre-#2938 visual that existing sample
            // code (raylib gallery, GumShapesGallery, SilkNetGum) assumes. Setting FillColor to
            // a visible color lights up the fill without needing to also flip IsFilled.
            _stroke.Color = _strokeColor;
            _stroke.Radius = 16;
            if (_fill != null)
            {
                _fill.Color = _isFilled ? _fillColor : new Color(0, 0, 0, 0);
                _fill.Radius = 16;
            }
            Width = 32;
            Height = 32;

            // Dropshadow defaults mirror SkiaShapeRuntime: opaque black, slight downward
            // offset, slight Y blur. HasDropshadow is false so the values are inert until
            // toggled on at runtime, at which point a visible shadow appears without further
            // setup. Issue #2797.
            DropshadowAlpha = 255;
            DropshadowOffsetY = 3;
            DropshadowBlurY = 3;

            // Mirror initial size to the stroke renderable when it's a child of fill — see
            // SyncStrokeSize comment in PreRender. Layout pushed Width/Height to fill but not
            // through to stroke (it's not a GraphicalUiElement, doesn't auto-track its parent).
            if (_fill is IPositionedSizedObject ctorFill && _stroke is IPositionedSizedObject ctorStroke)
            {
                ctorStroke.Width = ctorFill.Width;
                ctorStroke.Height = ctorFill.Height;
            }
#else
            var circle = new ContainedCircleType();
            SetContainedObject(circle);
            containedLineCircle = circle;

#if SKIA
            // Issue #2790: opt this Skia runtime into two-slot fill+stroke composition. The
            // contained circle (created above) becomes the fill slot; this second Circle is the
            // stroke slot, registered with the base so SkiaShapeRuntime.FillColor and StrokeColor
            // each route to their own renderable. Without this call the runtime stays on the
            // single-slot legacy model (last-non-null-setter-wins).
            SetStrokeRenderable(new ContainedCircleType());

            // Issue #2938 (regression fix): FillColor defaults to transparent (alpha 0) +
            // white stroke. IsFilled defaults to true so the gate is open, but the transparent
            // color means a freshly-constructed runtime renders as a stroke-only outline —
            // matching the pre-#2938 visual that existing sample code assumes. Assigning
            // FillColor to a visible color lights up the fill without flipping IsFilled.
            FillColor = new SKColor(0, 0, 0, 0);
            StrokeColor = SKColors.White;
            StrokeWidth = 1;
            StrokeWidthUnits = DimensionUnitType.ScreenPixel;

            // Dropshadow is off by default; pre-seed alpha + offset/blur so toggling
            // HasDropshadow = true at runtime produces a visible shadow without further setup.
            DropshadowAlpha = 255;
            DropshadowOffsetY = 3;
            DropshadowBlurY = 3;
#else
            circle.CircleOrigin = CircleOrigin.TopLeft;
            circle.Color = ColorExtensions.White;
#if RAYLIB
            // #2757 — match Skia's default: a fresh CircleRuntime ships with an opaque 1 px
            // white stroke so cells that only set FillColor still get a visible outline.
            // Without this the gallery's Modes / Alignment rows lost their outlines on raylib
            // but kept them on Skia. SOKOL renderable doesn't expose StrokeColor yet — leave
            // it null there so the existing outline-via-Color path keeps working unchanged.
            // Same fix landed for RectangleRuntime earlier in this PR.
            //
            // #2938 (regression fix) — push runtime-held FillColor / StrokeColor / IsFilled
            // defaults onto the renderable so the runtime properties report consistent state at
            // construction. FillColor defaults to transparent (alpha 0) and IsFilled defaults
            // to true → a fresh circle renders as a stroke-only outline (matching the pre-#2938
            // visual that the raylib gallery / cross-backend samples assume). Assigning
            // FillColor to a visible color lights up the fill without flipping IsFilled.
            circle.StrokeColor = _strokeColor;
            circle.FillColor = _fillColor;
            circle.IsFilled = true;
#endif
#endif
            Width = 32;
            Height = 32;
            circle.Radius = 16;
#endif
        }
    }
}
