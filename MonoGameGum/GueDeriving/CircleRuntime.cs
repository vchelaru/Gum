using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;

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
using SkiaGum.Renderables;
using Color = SkiaSharp.SKColor;
using ContainedCircleType = SkiaGum.Renderables.LineCircle;
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
/// Runtime wrapping a circle renderable. Under XNA-likes (MonoGame/FNA/KNI), the renderable is
/// produced once at construction by <see cref="RenderableRegistry"/> for capability
/// <see cref="ICircleRenderable"/> and kept for the lifetime of the runtime — no per-property
/// swap.
/// </summary>
/// <remarks>
/// <para>
/// Core MonoGameGum registers <see cref="DefaultCircleRenderable"/> as the default; the optional
/// MonoGameGumShapes (Apos.Shapes) package overrides it with a fill-capable
/// <c>Circle</c> renderable. <b>The override only applies to <see cref="CircleRuntime"/>s
/// constructed after MonoGameGumShapes registers its factory.</b> Existing instances are not
/// retroactively re-bound. Register early — the typical place is during
/// <c>GumService.Initialize</c>, before any user UI is built.
/// </para>
/// <para>
/// On the core default, <see cref="FillColor"/> writes to the renderable's color slot and sets
/// <c>IsFilled = true</c>, but the renderable still draws as an outline — there is no true fill
/// mode without MonoGameGumShapes installed. This is intentional graceful degradation; layout,
/// color, and radius round-trip correctly so user code is forward-compatible with adding the
/// package later.
/// </para>
/// </remarks>
public class CircleRuntime : GraphicalUiElement
{
#if XNALIKE
    ICircleRenderable _circleRenderable = null!;
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
    /// writes the alpha channel of the renderable's color slot directly.
    /// </summary>
#if XNALIKE
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2761.")]
    public int Alpha
    {
        get => _circleRenderable.Color.A;
        set
        {
            Color current = _circleRenderable.Color;
            _circleRenderable.Color = new Color(current.R, current.G, current.B, (byte)value);
            NotifyPropertyChanged();
        }
    }
#else
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
    /// writes the red channel of the renderable's color slot directly.
    /// </summary>
#if XNALIKE
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2761.")]
    public int Red
    {
        get => _circleRenderable.Color.R;
        set
        {
            Color current = _circleRenderable.Color;
            _circleRenderable.Color = new Color((byte)value, current.G, current.B, current.A);
            NotifyPropertyChanged();
        }
    }
#else
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
    /// writes the green channel of the renderable's color slot directly.
    /// </summary>
#if XNALIKE
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2761.")]
    public int Green
    {
        get => _circleRenderable.Color.G;
        set
        {
            Color current = _circleRenderable.Color;
            _circleRenderable.Color = new Color(current.R, (byte)value, current.B, current.A);
            NotifyPropertyChanged();
        }
    }
#else
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
    /// writes the blue channel of the renderable's color slot directly.
    /// </summary>
#if XNALIKE
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2761.")]
    public int Blue
    {
        get => _circleRenderable.Color.B;
        set
        {
            Color current = _circleRenderable.Color;
            _circleRenderable.Color = new Color(current.R, current.G, (byte)value, current.A);
            NotifyPropertyChanged();
        }
    }
#else
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
        get => _circleRenderable.Radius;
        set
        {
            mWidth = value * 2;
            mHeight = value * 2;
            _circleRenderable.Radius = value;
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
    /// writes the renderable's color slot directly, without changing fill/stroke mode.
    /// </summary>
#if XNALIKE
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2761.")]
    public Color Color
    {
        get => _circleRenderable.Color;
        set
        {
            _circleRenderable.Color = value;
            NotifyPropertyChanged();
        }
    }
#else
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
    /// When non-null, the circle should render as a solid fill of this color (<c>IsFilled =
    /// true</c> + this color is pushed to the renderable). When null and <see cref="StrokeColor"/>
    /// is also null, the runtime restores the renderable to its default outline state.
    /// </summary>
    /// <remarks>
    /// Visual fill requires a fill-capable <see cref="ICircleRenderable"/> implementation —
    /// supplied by the optional MonoGameGumShapes (Apos.Shapes) package. On the core default
    /// <see cref="DefaultCircleRenderable"/> the flag and color are stored, but the renderable
    /// still draws as an outline. The runtime never swaps renderables.
    /// </remarks>
    public Color? FillColor
    {
        get => _fillColor;
        set
        {
            _fillColor = value;
            ApplyFillStrokeToRenderable();
            NotifyPropertyChanged();
        }
    }

    Color? _strokeColor;

    /// <summary>
    /// When non-null and <see cref="FillColor"/> is null, the circle should render as a stroked
    /// outline in this color (<c>IsFilled = false</c> + this color is pushed to the renderable).
    /// </summary>
    /// <remarks>
    /// On the core default the color is pushed to the outline renderable's color slot;
    /// <c>IsFilled</c> is stored but has no visual effect. On the Apos-backed renderable the
    /// full stroke API (StrokeWidth, dashed strokes, antialiasing) becomes available.
    /// </remarks>
    public Color? StrokeColor
    {
        get => _strokeColor;
        set
        {
            _strokeColor = value;
            ApplyFillStrokeToRenderable();
            NotifyPropertyChanged();
        }
    }

    float _strokeWidth = 1;

    /// <summary>
    /// Width of the stroke when this circle is drawing an outline. Held on the runtime
    /// alongside <see cref="StrokeWidthUnits"/> so ScreenPixel scaling can be re-resolved
    /// against the current camera zoom each frame in <see cref="PreRender"/>. Pushed to the
    /// renderable each frame; ignored by the core default (which has no stroke width concept).
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

    DimensionUnitType _strokeWidthUnits;

    /// <summary>
    /// Unit of measurement for <see cref="StrokeWidth"/>. <c>Absolute</c> means world-space
    /// pixels; <c>ScreenPixel</c> divides by the camera zoom each frame so the stroke holds
    /// a constant on-screen size. See <c>AposShapeRuntime.PreRender</c> for the canonical
    /// implementation of this pattern.
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

    /// <summary>
    /// Writes the current FillColor / StrokeColor pair onto the bound renderable. FillColor wins
    /// when both are set. When both are null, restores the renderable to the default outline
    /// (white). Does NOT swap renderables — the renderable is bound for life.
    /// </summary>
    void ApplyFillStrokeToRenderable()
    {
        if (_fillColor.HasValue)
        {
            _circleRenderable.IsFilled = true;
            _circleRenderable.Color = _fillColor.Value;
            return;
        }

        if (_strokeColor.HasValue)
        {
            _circleRenderable.IsFilled = false;
            _circleRenderable.Color = _strokeColor.Value;
            return;
        }

        // Both null — back to default outline white. The bound renderable is preserved; only
        // its mode + color slot reset.
        _circleRenderable.IsFilled = false;
        _circleRenderable.Color = Color.White;
    }

    /// <summary>
    /// Pushes runtime-held stroke values to the bound renderable each frame, resolving
    /// <see cref="StrokeWidthUnits"/> against the current camera zoom. The Apos-backed renderable
    /// reaches this through the <c>OnPreRender</c> hook the MonoGameGumShapes factory wires up;
    /// the core default's <c>PreRender</c> never calls back, but the stroke value still gets
    /// pushed when this method is reached through the layout system.
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
        _circleRenderable.StrokeWidth = strokeWidth;

        // Do NOT call base.PreRender() here. base.PreRender() forwards to the renderable's
        // PreRender — but the Apos renderable's PreRender is what calls us back via the
        // OnPreRender hook the MonoGameGumShapes factory wires at construction. Forwarding
        // would recurse infinitely. Same caveat as AposShapeRuntime.PreRender. For the core
        // default the hook is not wired, but the simpler invariant is "never forward from
        // here"; layout-time PreRender on GraphicalUiElement runs independently.
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
            // Construct-time binding (#2761). Whatever the registry returns is what this
            // runtime renders with for the rest of its life. The optional MonoGameGumShapes
            // package overrides the default registration; without it, core's
            // DefaultCircleRenderable is bound. The null-coalesce is defensive — the default
            // factory is registered by DefaultCircleRenderable's [ModuleInitializer], so it
            // should always be present, but if a test or consumer Reset()s the registry and
            // then constructs a CircleRuntime before re-registering, we still produce a
            // working renderable rather than crashing.
            ICircleRenderable renderable = RenderableRegistry.Create<ICircleRenderable>(this)
                ?? new DefaultCircleRenderable();
            _circleRenderable = renderable;
            SetContainedObject((IRenderable)renderable);

            renderable.Color = Color.White;
            renderable.Radius = 16;
            Width = 32;
            Height = 32;
#else
            var circle = new ContainedCircleType();
            circle.CircleOrigin = CircleOrigin.TopLeft;
            SetContainedObject(circle);
            containedLineCircle = circle;

#if SKIA
            circle.CornerRadius = 0;
            circle.Color = SkiaSharp.SKColors.White;
#else
            circle.Color = ColorExtensions.White;
#endif
            Width = 32;
            Height = 32;
            circle.Radius = 16;
#endif
        }
    }
}
