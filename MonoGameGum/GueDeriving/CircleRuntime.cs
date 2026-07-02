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
using Gum.Renderables;
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
    ShapeGradientState _gradientState;
    ShapeDropshadowState _dropshadowState;
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

#if !SKIA
    /// <summary>
    /// Per-platform routing for the obsolete single-color surface (<see cref="Color"/>,
    /// <see cref="Alpha"/>, <see cref="Red"/>, <see cref="Green"/>, <see cref="Blue"/>).
    /// XNALIKE writes the color directly onto the stroke renderable slot; everything else
    /// (Raylib/Sokol) writes through <see cref="ContainedLineCircle"/>.
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
#else
        get => ContainedLineCircle.Color;
        set
        {
            ContainedLineCircle.Color = value;
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
    /// writes the alpha channel of the stroke renderable's color slot directly.
    /// </summary>
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2761.")]
    public int Alpha
    {
        get => ObsoleteStrokeColor.A;
        set => ObsoleteStrokeColor = WithAlpha(ObsoleteStrokeColor, (byte)value);
    }

    /// <summary>
    /// Obsolete: use <see cref="FillColor"/> or <see cref="StrokeColor"/>. Legacy member that
    /// writes the red channel of the stroke renderable's color slot directly.
    /// </summary>
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2761.")]
    public int Red
    {
        get => ObsoleteStrokeColor.R;
        set => ObsoleteStrokeColor = WithRed(ObsoleteStrokeColor, (byte)value);
    }

    /// <summary>
    /// Obsolete: use <see cref="FillColor"/> or <see cref="StrokeColor"/>. Legacy member that
    /// writes the green channel of the stroke renderable's color slot directly.
    /// </summary>
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2761.")]
    public int Green
    {
        get => ObsoleteStrokeColor.G;
        set => ObsoleteStrokeColor = WithGreen(ObsoleteStrokeColor, (byte)value);
    }

    /// <summary>
    /// Obsolete: use <see cref="FillColor"/> or <see cref="StrokeColor"/>. Legacy member that
    /// writes the blue channel of the stroke renderable's color slot directly.
    /// </summary>
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2761.")]
    public int Blue
    {
        get => ObsoleteStrokeColor.B;
        set => ObsoleteStrokeColor = WithBlue(ObsoleteStrokeColor, (byte)value);
    }
#endif

    /// <summary>
    /// Obsolete: set <c>Width</c> and <c>Height</c> instead. Retained for back-compat — the
    /// setter now only proxies size (sets <c>Width</c> = <c>Height</c> = <c>Radius</c> * 2) and
    /// pushes the radius to the renderable. See the June 2026 migration guide (issue #2761).
    /// </summary>
    [Obsolete("Set Width and Height instead (Radius now proxies Width = Height = Radius * 2). See migration guide for issue #2761.")]
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

#if !SKIA
    /// <summary>
    /// Obsolete: use <see cref="FillColor"/> or <see cref="StrokeColor"/>. Legacy member that
    /// writes the stroke renderable's color slot directly. Routes to stroke for back-compat —
    /// <see cref="CircleRuntime"/> was historically outline-only.
    /// </summary>
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2761.")]
    public Color Color
    {
        get => ObsoleteStrokeColor;
        set => ObsoleteStrokeColor = value;
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

    // FillColor defaults to opaque white while IsFilled defaults to false, so a freshly-
    // constructed CircleRuntime renders as a stroke-only outline (the white fill is gated off).
    // Flipping IsFilled = true fills it white without needing to assign FillColor.
    Color _fillColor = new Color((byte)255, (byte)255, (byte)255, (byte)255);

    /// <inheritdoc cref="Gum.Renderables.LineCircle.FillColor"/>
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

    // Issue #3009 follow-up — gate the renderable's fill color by IsFilled so a stroke-only circle
    // (IsFilled = false) leaves the renderable's FillColor null and its fill pass (which runs when
    // FillColor.HasValue || IsFilled) stays off — a fresh circle is outline-only. Mirrors how the
    // Apos/Skia two-slot model pushes a transparent fill when the shape isn't filled; previously
    // the default opaque-white FillColor was pushed unconditionally, filling every default circle.
    void PushFillColorToRenderable()
    {
        ContainedLineCircle.FillColor = ContainedLineCircle.IsFilled ? (Color?)_fillColor : null;
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

    /// <inheritdoc cref="Gum.Renderables.LineCircle.IsFilled"/>
    public bool IsFilled
    {
        get => ContainedLineCircle.IsFilled;
        set
        {
            ContainedLineCircle.IsFilled = value;
            // Issue #3009 — re-gate the renderable fill color and re-route the gradient start to
            // the now-active body color.
            PushFillColorToRenderable();
            SyncGradientStart();
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

    // Issue #3009 — Circle/Rectangle no longer expose a standalone gradient Color1. The gradient
    // start mirrors the active body color (FillColor when filled, StrokeColor otherwise), synced
    // from the FillColor / StrokeColor / IsFilled setters into the renderable's Color1.
    void SyncGradientStart()
    {
        ContainedLineCircle.Color1 = ContainedLineCircle.IsFilled ? _fillColor : _strokeColor;
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

    // Issue #3175 — additional raylib parity surface so theme source written against the richer
    // Apos.Shapes gradient feature set (endpoint / radius units) compiles unchanged on raylib. The
    // raylib LineCircle renderable does not consume these yet, so the values round-trip on the
    // runtime as forward compat (mirrors the RectangleRuntime block). When the renderable gains
    // support, push them through like the GradientX1 setter above.

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

#if XNALIKE
    // FillColor defaults to opaque white while IsFilled defaults to false, so a freshly-
    // constructed CircleRuntime renders as a stroke-only outline (the white fill is gated off).
    // This is the "pit of success" default: flipping IsFilled = true fills the disk white
    // without needing to also assign FillColor.
    Color _fillColor = Color.White;

    /// <summary>
    /// Color of the filled disk. Painted into the fill slot when <see cref="IsFilled"/> is
    /// <c>true</c>; ignored visually when <c>IsFilled</c> is <c>false</c> (the fill slot is
    /// pushed a transparent color so only the stroke draws). Defaults to opaque white so that
    /// flipping <see cref="IsFilled"/> on alone produces a visible (white) fill.
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
            // Issue #3009 — the fill slot's gradient start mirrors FillColor (no standalone Color1).
            _gradientState.PushGradientStart(_fillColor, _strokeColor, _fill as IGradientedRenderable, _stroke as IGradientedRenderable);
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
            // Dropshadow routing depends on IsFilled — re-push so the active slot owns the
            // shadow flag and the inactive slot releases it. Otherwise toggling IsFilled
            // either ghosts the previous target or never wakes the new one up.
            _dropshadowState.SyncTarget(_fill as IDropshadowRenderable, _stroke as IDropshadowRenderable, _isFilled);
            // Gradient routing also depends on IsFilled: the gradient paints the active body
            // (fill when filled, stroke when stroke-only), so re-route on toggle.
            _gradientState.PushGradientGate(_fill as IGradientedRenderable, _stroke as IGradientedRenderable, _isFilled);
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
            // Issue #3009 — the stroke slot's gradient start mirrors StrokeColor (no standalone Color1).
            _gradientState.PushGradientStart(_fillColor, _strokeColor, _fill as IGradientedRenderable, _stroke as IGradientedRenderable);
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
    /// <see cref="Gum.Renderables.DefaultStrokedCircleRenderable"/> wraps
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

    Gum.RenderingLibrary.Blend _blend = Gum.RenderingLibrary.Blend.Normal;
    /// <summary>
    /// Issue #2937 — blend mode for the circle. Pushed to BOTH slots (matching UseGradient /
    /// IsAntialiased) because blend is folded into each renderable's BatchKey: a fill/stroke
    /// disagreement would split them across two ShapeBatches and render the stroke with the
    /// wrong blend. Visual effect requires the optional MonoGameGumShapes (Apos.Shapes) package;
    /// without it the value round-trips but renders as a no-op (graceful degradation).
    /// </summary>
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

    #region Gradient

    // Issue #2791 (extracted into ShapeGradientState — issue "shape-gradient-dropshadow-dedup"
    // — shared with RectangleRuntime): gradient pass-through. Backing fields live in
    // _gradientState so values round-trip even when neither slot implements
    // IGradientedRenderable (e.g. core-only stroke = DefaultStrokedCircleRenderable, no fill).
    // The coordinate/color setters push to whichever slot(s) implement it so the values
    // round-trip on either; the UseGradient GATE routes to the active body slot (see
    // ShapeGradientState.PushGradientGate). The gradient is the active body's paint — a
    // gradient on the other slot would share the single gradient and composite invisibly, so
    // the inactive slot renders solid.

    /// <summary>
    /// When <c>true</c>, the gradient color/coordinate properties drive rendering instead of
    /// <see cref="FillColor"/> / <see cref="StrokeColor"/>. Visual effect requires the optional
    /// MonoGameGumShapes (Apos.Shapes) package; without it the value round-trips but does not
    /// render.
    /// </summary>
    public bool UseGradient
    {
        get => _gradientState.UseGradient;
        set
        {
            _gradientState.SetUseGradient(value, _fill as IGradientedRenderable, _stroke as IGradientedRenderable, _isFilled);
            NotifyPropertyChanged();
        }
    }

    public GradientType GradientType
    {
        get => _gradientState.GradientType;
        set
        {
            _gradientState.SetGradientType(value, _fill as IGradientedRenderable, _stroke as IGradientedRenderable);
            NotifyPropertyChanged();
        }
    }

    // Issue #3009 — the gradient start (Red1/Green1/Blue1/Alpha1 / Color1) is no longer stored on
    // the runtime. It is driven from the active body color via ShapeGradientState.PushGradientStart;
    // the standalone Color1 surface was dropped for Circle/Rectangle (gradient support is
    // unshipped, so no data to preserve). Color2 below remains the only standalone gradient color.

    public int Alpha2
    {
        get => _gradientState.Alpha2;
        set
        {
            _gradientState.SetAlpha2(value, _fill as IGradientedRenderable, _stroke as IGradientedRenderable);
            NotifyPropertyChanged();
        }
    }

    public int Red2
    {
        get => _gradientState.Red2;
        set
        {
            _gradientState.SetRed2(value, _fill as IGradientedRenderable, _stroke as IGradientedRenderable);
            NotifyPropertyChanged();
        }
    }

    public int Green2
    {
        get => _gradientState.Green2;
        set
        {
            _gradientState.SetGreen2(value, _fill as IGradientedRenderable, _stroke as IGradientedRenderable);
            NotifyPropertyChanged();
        }
    }

    public int Blue2
    {
        get => _gradientState.Blue2;
        set
        {
            _gradientState.SetBlue2(value, _fill as IGradientedRenderable, _stroke as IGradientedRenderable);
            NotifyPropertyChanged();
        }
    }

    public Color Color2
    {
        get => _gradientState.Color2;
        set
        {
            Red2 = value.R;
            Green2 = value.G;
            Blue2 = value.B;
            Alpha2 = value.A;
        }
    }

    public float GradientX1
    {
        get => _gradientState.GradientX1;
        set
        {
            _gradientState.SetGradientX1(value, _fill as IGradientedRenderable, _stroke as IGradientedRenderable);
            NotifyPropertyChanged();
        }
    }

    public GeneralUnitType GradientX1Units
    {
        get => _gradientState.GradientX1Units;
        set
        {
            _gradientState.SetGradientX1Units(value, _fill as IGradientedRenderable, _stroke as IGradientedRenderable);
            NotifyPropertyChanged();
        }
    }

    public float GradientY1
    {
        get => _gradientState.GradientY1;
        set
        {
            _gradientState.SetGradientY1(value, _fill as IGradientedRenderable, _stroke as IGradientedRenderable);
            NotifyPropertyChanged();
        }
    }

    public GeneralUnitType GradientY1Units
    {
        get => _gradientState.GradientY1Units;
        set
        {
            _gradientState.SetGradientY1Units(value, _fill as IGradientedRenderable, _stroke as IGradientedRenderable);
            NotifyPropertyChanged();
        }
    }

    public float GradientX2
    {
        get => _gradientState.GradientX2;
        set
        {
            _gradientState.SetGradientX2(value, _fill as IGradientedRenderable, _stroke as IGradientedRenderable);
            NotifyPropertyChanged();
        }
    }

    public GeneralUnitType GradientX2Units
    {
        get => _gradientState.GradientX2Units;
        set
        {
            _gradientState.SetGradientX2Units(value, _fill as IGradientedRenderable, _stroke as IGradientedRenderable);
            NotifyPropertyChanged();
        }
    }

    public float GradientY2
    {
        get => _gradientState.GradientY2;
        set
        {
            _gradientState.SetGradientY2(value, _fill as IGradientedRenderable, _stroke as IGradientedRenderable);
            NotifyPropertyChanged();
        }
    }

    public GeneralUnitType GradientY2Units
    {
        get => _gradientState.GradientY2Units;
        set
        {
            _gradientState.SetGradientY2Units(value, _fill as IGradientedRenderable, _stroke as IGradientedRenderable);
            NotifyPropertyChanged();
        }
    }

    public float GradientInnerRadius
    {
        get => _gradientState.GradientInnerRadius;
        set
        {
            _gradientState.SetGradientInnerRadius(value, _fill as IGradientedRenderable, _stroke as IGradientedRenderable);
            NotifyPropertyChanged();
        }
    }

    public DimensionUnitType GradientInnerRadiusUnits
    {
        get => _gradientState.GradientInnerRadiusUnits;
        set
        {
            _gradientState.SetGradientInnerRadiusUnits(value, _fill as IGradientedRenderable, _stroke as IGradientedRenderable);
            NotifyPropertyChanged();
        }
    }

    public float GradientOuterRadius
    {
        get => _gradientState.GradientOuterRadius;
        set
        {
            _gradientState.SetGradientOuterRadius(value, _fill as IGradientedRenderable, _stroke as IGradientedRenderable);
            NotifyPropertyChanged();
        }
    }

    public DimensionUnitType GradientOuterRadiusUnits
    {
        get => _gradientState.GradientOuterRadiusUnits;
        set
        {
            _gradientState.SetGradientOuterRadiusUnits(value, _fill as IGradientedRenderable, _stroke as IGradientedRenderable);
            NotifyPropertyChanged();
        }
    }

    #endregion

    #region Dropshadow

    // Issue #2797 (extracted into ShapeDropshadowState — shared with RectangleRuntime):
    // dropshadow pass-through. Backing fields live in _dropshadowState so values round-trip
    // even when neither slot implements IDropshadowRenderable (core-only stroke =
    // DefaultStrokedCircleRenderable, no fill). Unlike gradient (#2791) and AA (#2798), which
    // push to BOTH slots so a single setter covers fill and stroke, the shadow is drawn once
    // per renderable — pushing to both would render the shadow twice and visibly double up. So
    // the routing picks one slot: the fill when IsFilled = true (the disc casts the shadow),
    // the stroke when IsFilled = false (the disc is gated to transparent and can't cast a
    // visible shadow). See ShapeDropshadowState.GetTarget / SyncTarget for the shared rule.

    /// <summary>
    /// When <c>true</c>, the dropshadow color/offset/blur properties drive an extra render
    /// pass behind the circle. Visual effect requires the optional MonoGameGumShapes
    /// (Apos.Shapes) package; without it the value round-trips but does not render.
    /// </summary>
    public bool HasDropshadow
    {
        get => _dropshadowState.HasDropshadow;
        set
        {
            _dropshadowState.SetHasDropshadow(value, _fill as IDropshadowRenderable, _stroke as IDropshadowRenderable, _isFilled);
            NotifyPropertyChanged();
        }
    }

    public Color DropshadowColor
    {
        get => _dropshadowState.DropshadowColor;
        set
        {
            _dropshadowState.SetDropshadowColor(value, _fill as IDropshadowRenderable, _stroke as IDropshadowRenderable, _isFilled);
            NotifyPropertyChanged();
        }
    }

    public int DropshadowAlpha
    {
        get => _dropshadowState.DropshadowColor.A;
        set
        {
            DropshadowColor = new Color(_dropshadowState.DropshadowColor.R, _dropshadowState.DropshadowColor.G, _dropshadowState.DropshadowColor.B, (byte)value);
        }
    }

    public int DropshadowRed
    {
        get => _dropshadowState.DropshadowColor.R;
        set
        {
            DropshadowColor = new Color((byte)value, _dropshadowState.DropshadowColor.G, _dropshadowState.DropshadowColor.B, _dropshadowState.DropshadowColor.A);
        }
    }

    public int DropshadowGreen
    {
        get => _dropshadowState.DropshadowColor.G;
        set
        {
            DropshadowColor = new Color(_dropshadowState.DropshadowColor.R, (byte)value, _dropshadowState.DropshadowColor.B, _dropshadowState.DropshadowColor.A);
        }
    }

    public int DropshadowBlue
    {
        get => _dropshadowState.DropshadowColor.B;
        set
        {
            DropshadowColor = new Color(_dropshadowState.DropshadowColor.R, _dropshadowState.DropshadowColor.G, (byte)value, _dropshadowState.DropshadowColor.A);
        }
    }

    public float DropshadowOffsetX
    {
        get => _dropshadowState.DropshadowOffsetX;
        set
        {
            _dropshadowState.SetDropshadowOffsetX(value, _fill as IDropshadowRenderable, _stroke as IDropshadowRenderable, _isFilled);
            NotifyPropertyChanged();
        }
    }

    public float DropshadowOffsetY
    {
        get => _dropshadowState.DropshadowOffsetY;
        set
        {
            _dropshadowState.SetDropshadowOffsetY(value, _fill as IDropshadowRenderable, _stroke as IDropshadowRenderable, _isFilled);
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// Isotropic blur radius in pixels for the dropshadow. Pushes a single value to both
    /// the underlying renderable's X and Y blur fields — Apos.Shapes approximates the
    /// visible falloff via its <c>antiAliasSize</c> parameter and does not support a
    /// per-axis blur. Mirrors industry convention (CSS <c>box-shadow</c> blur-radius,
    /// Figma effects, Photoshop) where dropshadow blur is a single scalar.
    /// </summary>
    public float DropshadowBlur
    {
        get => _dropshadowState.DropshadowBlur;
        set
        {
            _dropshadowState.SetDropshadowBlur(value, _fill as IDropshadowRenderable, _stroke as IDropshadowRenderable, _isFilled);
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
    /// it the stroke slot is <see cref="Gum.Renderables.DefaultStrokedCircleRenderable"/>
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
        // Resolve the camera once up front. It drives two unit conversions below: the
        // ScreenPixel → world division for StrokeWidth (and dash/gap), and the screen → world
        // division for aposAaContribution (which represents Apos's hardcoded 1-pixel AA halo,
        // see #2936). Falls back to zoom = 1 in unit tests / pre-mount.
        var camera = this.EffectiveManagers?.Renderer?.Camera;
        float cameraZoom = camera?.Zoom ?? 1f;

        float strokeWidth = _strokeWidth;
        float strokeDashLength = _strokeDashLength;
        float strokeGapLength = _strokeGapLength;
        if (_strokeWidthUnits == DimensionUnitType.ScreenPixel && camera != null)
        {
            // Mirrors AposShapeRuntime.PreRender — dash and gap scale alongside stroke
            // width so a "1 px dotted" pattern stays 1 px on screen regardless of zoom.
            strokeWidth /= cameraZoom;
            strokeDashLength /= cameraZoom;
            strokeGapLength /= cameraZoom;
        }
        // Two distinct cases for what to push to the renderable's StrokeWidth — don't collapse
        // them, the difference is load-bearing:
        //
        // 1. User explicitly set StrokeWidth <= 0 → push a literal 0 (#2950 follow-up).
        //    StrokeWidth = 0 is the canonical hide-stroke gate since #2938 made StrokeColor
        //    non-nullable, so the user wants NO stroke at all. The renderable's
        //    HasVisibleOutput predicate then short-circuits Circle.Render to skip the
        //    stroke-slot draw entirely. **Do NOT route this case through the AA-compensation
        //    path below** — the epsilon floor would push 0.01, the renderable's
        //    HasVisibleOutput would return true (StrokeWidth > 0), and Apos's 1 px AA fringe
        //    would render a hairline of stroke color the user thought they had disabled.
        //
        // 2. User set a positive StrokeWidth → subtract the 1 px Apos AA contribution
        //    (#2790). Apos.Shapes' DrawCircle renders aaSize pixels of AA halo OUTSIDE the
        //    nominal thickness; Circle.Render passes aaSize = 1 when IsAntialiased is true.
        //    Skia fits AA WITHIN the thickness, so the same user-set StrokeWidth would
        //    otherwise read 1 px wider on Apos than on Skia. The result is floored at a tiny
        //    positive epsilon — NOT to hide a "0 means don't draw" case (that's handled
        //    above by case 1), but to keep thin strokes like StrokeWidth = 1 visible: after
        //    subtracting the AA contribution the math would be 0, which Apos refuses to draw
        //    even with aaSize > 0. The epsilon push pairs with the 1 px AA halo to produce
        //    the intended ~1 px visible stroke. Gated by IAntialiasedRenderable so the core
        //    stroke default (LineCircle wrapper, no AA concept) still receives the raw value.
        const float aposAaContribution = 1f;
        const float aposMinThicknessEpsilon = 0.01f;
        // Issue #2936 — aposAaContribution is in SCREEN pixels (Apos's hardcoded 1 px AA halo).
        // Convert to world units before mixing with strokeWidth, which has already been
        // resolved to world units above. At cameraZoom = 1 this is a no-op (original #2790
        // behavior preserved); at cameraZoom > 1 the world value shrinks proportionally, which
        // is what closes the gap below.
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
                    // #2936: aaContribution in WORLD units (= 1 screen px / zoom). At Zoom > 1
                    // this is < 1 world unit, so the inset no longer over-shrinks the fill
                    // relative to the visible stroke band's inward extent.
                    fillRadiusInset = Math.Max(fillRadiusInset, aposAaContributionWorld);
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
        // Issue #2937 — re-fire Blend onto the freshly-built slots for the same reason.
        toReturn.Blend = toReturn.Blend;
        // Boyscout fix (shape-gradient-dropshadow-dedup) — push the Gradient/Dropshadow state
        // onto the freshly-built slots too. Previously neither region re-fired here: the
        // backing fields survived via MemberwiseClone but the clone's new slots never received
        // them, so a clone with UseGradient/HasDropshadow = true silently rendered without its
        // gradient/shadow until some other property write happened to re-trigger it.
        toReturn._gradientState.PushAll(toReturn._fill as IGradientedRenderable, toReturn._stroke as IGradientedRenderable, toReturn._isFilled, toReturn._fillColor, toReturn._strokeColor);
        toReturn._dropshadowState.PushAll(toReturn._fill as IDropshadowRenderable, toReturn._stroke as IDropshadowRenderable, toReturn._isFilled);
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
        var camera = this.EffectiveManagers?.Renderer?.Camera;
        float cameraZoom = camera?.Zoom ?? 1f;

        float strokeWidth = _strokeWidth;
        if (_strokeWidthUnits == DimensionUnitType.ScreenPixel && camera != null)
        {
            strokeWidth /= cameraZoom;
        }

        // Issue #3183 — raylib gets NO AA compensation; see RectangleRuntime.PreRender for the full
        // rationale. Apos subtracts a ~1px geometric contribution but adds it back as an AA band;
        // raylib's MSAA adds no width, so the geometric width IS the visible width. Pushing
        // StrokeWidth straight through matches the Apos reference (shape-parity harness). The prior
        // #3179 subtraction collapsed a nominal 1px ring to ~0.01px (invisible). StrokeWidth <= 0
        // still pushes literal 0 so LineCircle's positive-width gate suppresses the ring.
        ContainedLineCircle.StrokeWidth = strokeWidth <= 0 ? 0f : strokeWidth;
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

    float _dropshadowBlur;
    /// <summary>
    /// Isotropic blur radius in pixels for the dropshadow. The raylib renderable
    /// approximates blur via concentric semi-transparent rings; pushing a single value
    /// to both X and Y of the contained <see cref="Gum.Renderables.LineCircle"/>.
    /// </summary>
    public float DropshadowBlur
    {
        get => _dropshadowBlur;
        set
        {
            _dropshadowBlur = value;
#if RAYLIB
            ContainedLineCircle.DropshadowBlurX = value;
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

    // Unified-API cleanup: CircleRuntime is a new fill+stroke shape, so the single-color legacy
    // members it inherits from SkiaShapeRuntime (the base for ALL Skia shapes) are obsolete on
    // this surface — users typed as CircleRuntime are steered to FillColor / StrokeColor, the
    // same way the XNA-like and raylib surfaces do it. The members stay live on the base for the
    // legacy single-color shapes (Arc, RoundedRectangle, ColoredCircle, ...). Color is already
    // [Obsolete] on the base, so only Red/Green/Blue/Alpha need shadowing here.
    /// <summary>Obsolete: use <see cref="SkiaShapeRuntime.FillColor"/> or <see cref="SkiaShapeRuntime.StrokeColor"/>.</summary>
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2761.")]
    public new int Alpha
    {
        get => base.Alpha;
        set => base.Alpha = value;
    }

    /// <inheritdoc cref="Alpha"/>
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2761.")]
    public new int Red
    {
        get => base.Red;
        set => base.Red = value;
    }

    /// <inheritdoc cref="Alpha"/>
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2761.")]
    public new int Green
    {
        get => base.Green;
        set => base.Green = value;
    }

    /// <inheritdoc cref="Alpha"/>
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2761.")]
    public new int Blue
    {
        get => base.Blue;
        set => base.Blue = value;
    }

    // Issue #3009 — the gradient START stop is the active body color (FillColor when filled,
    // StrokeColor otherwise), driven by the base's two-slot SyncGradientStartToBody. The standalone
    // Color1 surface inherited from SkiaShapeRuntime is therefore unsupported on the new fill+stroke
    // Circle/Rectangle: shadow it as an error so consumers are steered to FillColor / StrokeColor
    // (mirrors the XNALIKE hard-drop and the Color/Red/Green/Blue obsolete shadows above). The
    // members stay live on the base for the legacy single-color shapes (Arc, RoundedRectangle, ...).
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

    /// <summary>
    /// Isotropic blur radius in pixels for the dropshadow. The plain <see cref="CircleRuntime"/>
    /// dropshadow blur is a single scalar by design (issue #2761 new-shape contract), mirroring
    /// industry convention (CSS <c>box-shadow</c>, Figma, Photoshop). Skia natively supports
    /// per-axis blur, so this pushes the value to both the inherited per-axis fields; the
    /// per-axis members are hidden/obsolete on this surface (see below).
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
    /// Obsolete on the plain <see cref="CircleRuntime"/> — use <see cref="DropshadowBlur"/>.
    /// Hidden shadow of the inherited per-axis blur, kept only so existing code compiles; the
    /// new-shape contract exposes a single scalar (per-axis blur stays on the legacy Skia shapes
    /// like <c>RoundedRectangleRuntime</c> / <c>ColoredCircleRuntime</c>).
    /// </summary>
    [Obsolete("Use DropshadowBlur (scalar). The plain Circle dropshadow blur is a single isotropic value by design.")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public new float DropshadowBlurX
    {
        get => base.DropshadowBlurX;
        set => base.DropshadowBlurX = value;
    }

    /// <inheritdoc cref="DropshadowBlurX"/>
    [Obsolete("Use DropshadowBlur (scalar). The plain Circle dropshadow blur is a single isotropic value by design.")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public new float DropshadowBlurY
    {
        get => base.DropshadowBlurY;
        set => base.DropshadowBlurY = value;
    }

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

            // Initial defaults — stroke white, fill white but gated off (IsFilled = false), so
            // the fill slot is pushed a transparent color and a freshly-constructed
            // CircleRuntime renders as a stroke-only outline, radius 16, layout 32x32. Flipping
            // IsFilled = true paints the fill white without needing to assign FillColor.
            _stroke.Color = _strokeColor;
            _stroke.Radius = 16;
            if (_fill != null)
            {
                _fill.Color = _isFilled ? _fillColor : new Color(0, 0, 0, 0);
                _fill.Radius = 16;
            }
            Width = 32;
            Height = 32;

            // Issue #3009 — seed each slot's gradient start from its (white) body color so flipping
            // UseGradient on a freshly-constructed circle starts from white rather than a stale
            // default, matching the no-jump contract.
            _gradientState.PushGradientStart(_fillColor, _strokeColor, _fill as IGradientedRenderable, _stroke as IGradientedRenderable);

            // Dropshadow defaults mirror SkiaShapeRuntime: opaque black, slight downward
            // offset, slight Y blur. HasDropshadow is false so the values are inert until
            // toggled on at runtime, at which point a visible shadow appears without further
            // setup. Issue #2797.
            DropshadowAlpha = 255;
            DropshadowOffsetY = 3;
            DropshadowBlur = 3;

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

            // Defaults: white fill gated off (IsFilled = false) + white stroke, so a freshly-
            // constructed runtime renders as a stroke-only outline. Because FillColor defaults
            // to opaque white, flipping IsFilled = true paints a white fill without assigning a
            // color. IsFilled must be set explicitly here — the shared SkiaShapeRuntime base
            // defaults it to true for the legacy single-color shapes. Setting IsFilled = false
            // re-pushes a transparent color into the fill slot so the renderable doesn't keep
            // its own white constructor default and render as a solid white disk.
            FillColor = SKColors.White;
            IsFilled = false;
            StrokeColor = SKColors.White;
            StrokeWidth = 1;
            StrokeWidthUnits = DimensionUnitType.ScreenPixel;

            // Dropshadow is off by default; pre-seed alpha + offset/blur so toggling
            // HasDropshadow = true at runtime produces a visible shadow without further setup.
            // Use the scalar DropshadowBlur (isotropic 3) so the default matches MonoGame/raylib;
            // the previous DropshadowBlurY-only seed left the scalar getter (X axis) reading 0.
            DropshadowAlpha = 255;
            DropshadowOffsetY = 3;
            DropshadowBlur = 3;
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
            // Push runtime-held FillColor / StrokeColor / IsFilled defaults onto the renderable
            // so the runtime properties report consistent state at construction. FillColor
            // defaults to opaque white and IsFilled defaults to false → a fresh circle renders
            // as a stroke-only outline; flipping IsFilled = true paints it white.
            circle.StrokeColor = _strokeColor;
            circle.IsFilled = false;
            // Issue #3009 — gate the fill color by IsFilled (null when not filled) so a fresh
            // circle is a stroke-only outline, and seed the gradient start from the body color.
            PushFillColorToRenderable();
            SyncGradientStart();
#endif
#endif
            Width = 32;
            Height = 32;
            circle.Radius = 16;
#endif
        }
    }
}
