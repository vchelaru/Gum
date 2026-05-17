using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;


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
using ContainedLineRectangle = SkiaGum.Renderables.LineRectangle;
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

    /// <inheritdoc cref="CircleRuntime.PreRender"/>
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

        // Mirror size to stroke when fill is the contained object — see CircleRuntime.PreRender.
        if (_fill is IPositionedSizedObject fillSized && _stroke is IPositionedSizedObject strokeSized)
        {
            strokeSized.Width = fillSized.Width;
            strokeSized.Height = fillSized.Height;
        }
    }
#endif

#if SKIA
    /// <summary>
    /// Routes SkiaShapeRuntime solid/gradient/stroke/dropshadow accessors to the
    /// contained LineRectangle. Issue #2814.
    /// </summary>
    protected override RenderableShapeBase ContainedRenderable => ContainedLineRectangle;

    /// <inheritdoc/>
    public override GraphicalUiElement Clone()
    {
        RectangleRuntime toReturn = (RectangleRuntime)base.Clone();
        // Reset cached renderable reference so the clone re-resolves against its own
        // RenderableComponent on next access. The fill slot color/dimensions were copied
        // by LineRectangle.Clone (ICloneable, MemberwiseClone).
        toReturn.containedLineRectangle = null!;
        // Issue #2790 recipe - drop the inherited stroke-slot reference and rebuild a fresh
        // one parented to the clone fill so the clone is fully independent.
        toReturn.ClearStrokeRenderable();
        toReturn.SetStrokeRenderable(new ContainedLineRectangle());
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

            if (_fill is IPositionedSizedObject ctorFill && _stroke is IPositionedSizedObject ctorStroke)
            {
                ctorStroke.Width = ctorFill.Width;
                ctorStroke.Height = ctorFill.Height;
            }
#elif SKIA
            // Issue #2814: two-slot fill+stroke composition for Skia (mirrors the CircleRuntime
            // wiring from issue #2790). The contained LineRectangle becomes the fill slot; a
            // second LineRectangle, registered via SetStrokeRenderable, is parented under the
            // fill so SkiaShapeRuntime.FillColor and StrokeColor each route to their own
            // renderable rather than fighting over a single IsFilled toggle.
            var rectangle = new ContainedLineRectangle();
            SetContainedObject(rectangle);
            containedLineRectangle = rectangle;

            SetStrokeRenderable(new ContainedLineRectangle());

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
