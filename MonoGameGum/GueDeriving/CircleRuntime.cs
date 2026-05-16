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
using ContainedCircleType = Gum.Renderables.LineCircle;
#elif SOKOL
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

#if XNALIKE
    Color? _fillColor;

    /// <summary>
    /// Color of the filled disk. When set non-null, the fill slot is painted with this color.
    /// When set null, the fill slot is hidden (alpha 0) so only the stroke draws.
    /// </summary>
    /// <remarks>
    /// Visual fill requires a fill-capable <see cref="IFilledCircleRenderable"/> implementation —
    /// supplied by the optional MonoGameGumShapes (Apos.Shapes) package. Without that package
    /// the fill slot is <c>null</c> and this setter is a visual no-op; the backing field still
    /// round-trips so getter results are consistent and a later install of MonoGameGumShapes
    /// (re-creating the runtime) will honor the stored color.
    /// </remarks>
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
    /// Color of the outline. <c>null</c> hides the stroke (alpha 0) so only the fill draws.
    /// The stroke slot is always non-null on supported backends — core ships
    /// <see cref="DefaultStrokedCircleRenderable"/> as the default.
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
        if (_strokeWidthUnits == DimensionUnitType.ScreenPixel)
        {
            var camera = this.EffectiveManagers?.Renderer?.Camera;
            if (camera != null)
            {
                strokeWidth /= camera.Zoom;
            }
        }
        _stroke.StrokeWidth = strokeWidth;

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

        // Do NOT call base.PreRender() — same caveat as AposShapeRuntime.PreRender. Forwarding
        // to the contained renderable's PreRender would recurse via the OnPreRender hook the
        // MonoGameGumShapes factory wires up.
    }
#endif

#if SKIA
    /// <summary>
    /// Routes <see cref="SkiaShapeRuntime"/>'s solid/gradient/stroke/dropshadow accessors
    /// to the contained <see cref="Circle"/>.
    /// </summary>
    protected override RenderableShapeBase ContainedRenderable => ContainedLineCircle;

    /// <inheritdoc/>
    public override GraphicalUiElement Clone()
    {
        CircleRuntime toReturn = (CircleRuntime)base.Clone();
        // Reset cached renderable reference so the clone re-resolves against its own
        // RenderableComponent on next access.
        toReturn.containedLineCircle = null!;
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

            // Initial defaults — stroke white, no fill, radius 16, layout 32x32. Fill alpha 0
            // until the user sets FillColor so it doesn't paint over the stroke at startup.
            _stroke.Color = Color.White;
            _stroke.Radius = 16;
            if (_fill != null)
            {
                _fill.Color = new Color(0, 0, 0, 0);
                _fill.Radius = 16;
            }
            Width = 32;
            Height = 32;

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
            // Skia's Circle draws from its bounding rect's center — no CircleOrigin or
            // CornerRadius properties. StrokeColor inherited from SkiaShapeRuntime; setting
            // it both selects stroke mode (IsFilled = false) and assigns the color in one go.
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
#endif
            Width = 32;
            Height = 32;
            circle.Radius = 16;
#endif
        }
    }
}
