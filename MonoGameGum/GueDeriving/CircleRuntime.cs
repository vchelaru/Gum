using Gum.Wireframe;
using RenderingLibrary;
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
#endif

#if XNALIKE
using Gum.DataTypes;
#endif

#if FRB
namespace MonoGameGum.GueDeriving;
#else
namespace Gum.GueDeriving;
#endif

public class CircleRuntime : GraphicalUiElement
{
    ContainedCircleType containedLineCircle;

    ContainedCircleType ContainedLineCircle
    {
        get
        {
            if (containedLineCircle == null)
            {
                containedLineCircle = this.RenderableComponent as ContainedCircleType;
            }
            return containedLineCircle;
        }
    }

    /// <summary>
    /// Obsolete: use <see cref="FillColor"/> or <see cref="StrokeColor"/>. The legacy member
    /// routes through whichever renderable is currently contained — <c>LineCircle</c> when
    /// outlining, an <see cref="IFilledShapeRenderable"/> after a fill swap.
    /// </summary>
#if XNALIKE
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2761.")]
#endif
    public int Alpha
    {
        get
        {
#if XNALIKE
            if (RenderableComponent is IFilledShapeRenderable filled)
            {
                return filled.Color.A;
            }
#endif
            return ContainedLineCircle.Color.A;
        }
        set
        {
#if XNALIKE
            if (RenderableComponent is IFilledShapeRenderable filled)
            {
                var current = filled.Color;
                filled.Color = new Color(current.R, current.G, current.B, (byte)value);
                NotifyPropertyChanged();
                return;
            }
#endif
            ContainedLineCircle.Color = ColorExtensions.WithAlpha(ContainedLineCircle.Color, (byte)value);
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// Obsolete: use <see cref="FillColor"/> or <see cref="StrokeColor"/>. The legacy member
    /// routes through whichever renderable is currently contained.
    /// </summary>
#if XNALIKE
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2761.")]
#endif
    public int Red
    {
        get
        {
#if XNALIKE
            if (RenderableComponent is IFilledShapeRenderable filled)
            {
                return filled.Color.R;
            }
#endif
            return ContainedLineCircle.Color.R;
        }
        set
        {
#if XNALIKE
            if (RenderableComponent is IFilledShapeRenderable filled)
            {
                var current = filled.Color;
                filled.Color = new Color((byte)value, current.G, current.B, current.A);
                NotifyPropertyChanged();
                return;
            }
#endif
            ContainedLineCircle.Color = ColorExtensions.WithRed(ContainedLineCircle.Color, (byte)value);
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// Obsolete: use <see cref="FillColor"/> or <see cref="StrokeColor"/>. The legacy member
    /// routes through whichever renderable is currently contained.
    /// </summary>
#if XNALIKE
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2761.")]
#endif
    public int Green
    {
        get
        {
#if XNALIKE
            if (RenderableComponent is IFilledShapeRenderable filled)
            {
                return filled.Color.G;
            }
#endif
            return ContainedLineCircle.Color.G;
        }
        set
        {
#if XNALIKE
            if (RenderableComponent is IFilledShapeRenderable filled)
            {
                var current = filled.Color;
                filled.Color = new Color(current.R, (byte)value, current.B, current.A);
                NotifyPropertyChanged();
                return;
            }
#endif
            ContainedLineCircle.Color = ColorExtensions.WithGreen(ContainedLineCircle.Color, (byte)value);
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// Obsolete: use <see cref="FillColor"/> or <see cref="StrokeColor"/>. The legacy member
    /// routes through whichever renderable is currently contained.
    /// </summary>
#if XNALIKE
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2761.")]
#endif
    public int Blue
    {
        get
        {
#if XNALIKE
            if (RenderableComponent is IFilledShapeRenderable filled)
            {
                return filled.Color.B;
            }
#endif
            return ContainedLineCircle.Color.B;
        }
        set
        {
#if XNALIKE
            if (RenderableComponent is IFilledShapeRenderable filled)
            {
                var current = filled.Color;
                filled.Color = new Color(current.R, current.G, (byte)value, current.A);
                NotifyPropertyChanged();
                return;
            }
#endif
            ContainedLineCircle.Color = ColorExtensions.WithBlue(ContainedLineCircle.Color, (byte)value);
            NotifyPropertyChanged();
        }
    }

    public float Radius
    {
        get
        {
#if XNALIKE
            if (RenderableComponent is IFilledShapeRenderable)
            {
                // IFilledShapeRenderable has no Radius member — Apos.Shapes' Circle reads
                // Width/2 directly. Mirror that here so Radius round-trips after a swap.
                return mWidth / 2f;
            }
#endif
            return ContainedLineCircle.Radius;
        }
        set
        {
            mWidth = value * 2;
            mHeight = value * 2;
#if XNALIKE
            if (RenderableComponent is IFilledShapeRenderable)
            {
                // No Radius to push — the renderable reads Width/2 each frame.
                return;
            }
#endif
            ContainedLineCircle.Radius = value;
        }
    }

    /// <summary>
    /// Obsolete: use <see cref="FillColor"/> or <see cref="StrokeColor"/>. The legacy member
    /// routes through whichever renderable is currently contained.
    /// </summary>
#if XNALIKE
    [Obsolete("Use FillColor or StrokeColor instead. See migration guide for issue #2761.")]
#endif
    public Color Color
    {
#if XNALIKE
        get
        {
            if (RenderableComponent is IFilledShapeRenderable filled)
            {
                return filled.Color;
            }
            return global::RenderingLibrary.Graphics.XNAExtensions.ToXNA(ContainedLineCircle.Color);
        }
        set
        {
            if (RenderableComponent is IFilledShapeRenderable filled)
            {
                filled.Color = value;
                NotifyPropertyChanged();
                return;
            }
            ContainedLineCircle.Color = global::RenderingLibrary.Graphics.XNAExtensions.ToSystemDrawing(value);
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

#if XNALIKE
    Color? _fillColor;

    /// <summary>
    /// When non-null, the circle renders as a solid fill of this color. When null and
    /// <see cref="StrokeColor"/> is also null, the circle renders as the default outline.
    /// </summary>
    /// <remarks>
    /// Filled rendering requires the optional MonoGameGumShapes (Apos.Shapes) package, which
    /// registers a renderable factory via <see cref="RenderableRegistry"/>. When that package
    /// is not referenced, setting <see cref="FillColor"/> is a graceful no-op for the visual:
    /// the runtime stays on its outline <c>LineCircle</c> rather than crashing.
    /// </remarks>
    public Color? FillColor
    {
        get => _fillColor;
        set
        {
            _fillColor = value;
            UpdateContainedFromFillStrokeColors();
            NotifyPropertyChanged();
        }
    }

    Color? _strokeColor;

    /// <summary>
    /// When non-null and <see cref="FillColor"/> is null, the circle renders as a stroked
    /// outline in this color. When both are null, the circle renders as the default outline.
    /// </summary>
    /// <remarks>
    /// Prefers an <see cref="IFilledShapeRenderable"/> with <c>IsFilled=false</c> when a
    /// factory is available (Apos.Shapes via MonoGameGumShapes), so the runtime gets the
    /// full stroke API (StrokeWidth, dashed strokes, antialiasing). Falls back to applying
    /// the color to the outline <c>LineCircle</c> when the factory is absent.
    /// </remarks>
    public Color? StrokeColor
    {
        get => _strokeColor;
        set
        {
            _strokeColor = value;
            UpdateContainedFromFillStrokeColors();
            NotifyPropertyChanged();
        }
    }

    float _strokeWidth = 1;

    /// <summary>
    /// Width of the stroke when this circle is drawing an outline (no fill). Held on the
    /// runtime alongside <see cref="StrokeWidthUnits"/> so ScreenPixel scaling can be
    /// re-resolved against the current camera zoom each frame in <see cref="PreRender"/>.
    /// Pushed to the contained renderable when it implements <see cref="IFilledShapeRenderable"/>;
    /// ignored when the runtime is on the fallback <c>LineCircle</c> (which has no stroke width).
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
    /// Swaps the contained renderable to match the current <see cref="FillColor"/> /
    /// <see cref="StrokeColor"/> combination. FillColor wins when both are set — the
    /// fill-capable renderable handles either case, just with <c>IsFilled</c> flipped.
    /// </summary>
    void UpdateContainedFromFillStrokeColors()
    {
        bool wantsFill = _fillColor.HasValue;
        bool wantsStroke = _strokeColor.HasValue && !wantsFill;

        if (wantsFill)
        {
            Color color = _fillColor!.Value;
            if (RenderableComponent is IFilledShapeRenderable existingFill)
            {
                existingFill.IsFilled = true;
                existingFill.Color = color;
            }
            else
            {
                var fill = RenderableRegistry.Create<IFilledShapeRenderable>(this);
                if (fill != null)
                {
                    fill.IsFilled = true;
                    fill.Color = color;
                    SwapToFillRenderable(fill);
                }
                // else: optional package absent — stay on the outline, no visual fill.
            }
            return;
        }

        if (wantsStroke)
        {
            Color color = _strokeColor!.Value;
            if (RenderableComponent is IFilledShapeRenderable existingFill)
            {
                existingFill.IsFilled = false;
                existingFill.Color = color;
            }
            else
            {
                var fill = RenderableRegistry.Create<IFilledShapeRenderable>(this);
                if (fill != null)
                {
                    fill.IsFilled = false;
                    fill.Color = color;
                    SwapToFillRenderable(fill);
                }
                else
                {
                    // Graceful degradation: no fill renderable available. Apply the requested
                    // stroke color to the outline LineCircle so the user at least sees the
                    // intended color, even without the stroke-width / dash machinery.
                    ContainedLineCircle.Color = global::RenderingLibrary.Graphics.XNAExtensions.ToSystemDrawing(color);
                }
            }
            return;
        }

        // Both null — swap back to the default LineCircle if currently on a fill renderable.
        if (RenderableComponent is IFilledShapeRenderable)
        {
            var line = new ContainedCircleType();
            line.CircleOrigin = CircleOrigin.TopLeft;
            line.Color = ColorExtensions.White;
            containedLineCircle = line;
            SetContainedObject(line);
        }
    }

    void SwapToFillRenderable(IFilledShapeRenderable fill)
    {
        containedLineCircle = null!;
        SetContainedObject(fill);
    }

    /// <summary>
    /// Pushes runtime-held stroke values to the contained fill renderable each frame,
    /// resolving <see cref="StrokeWidthUnits"/> against the current camera zoom. No-op when
    /// the contained renderable is a plain <c>LineCircle</c> (which has no stroke width).
    /// </summary>
    public override void PreRender()
    {
        if (RenderableComponent is IFilledShapeRenderable fill)
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
            fill.StrokeWidth = strokeWidth;

            // Do NOT call base.PreRender() here. base.PreRender() forwards to the contained
            // renderable's PreRender — but the renderable's PreRender is what just called us
            // back via OnPreRender (wired by the optional MonoGameGumShapes factory). Calling
            // it again would recurse infinitely. Same caveat as AposShapeRuntime.PreRender.
            return;
        }

        base.PreRender();
    }
#endif

    /// <inheritdoc cref="GraphicalUiElement.AddToManagers()"/>
    [Obsolete("Use the AddToRoot extension method instead (e.g. myCircle.AddToRoot()).")]
    public void AddToManagers() => base.AddToManagers(SystemManagers.Default, layer: null);

    public CircleRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
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
        }
    }
}
