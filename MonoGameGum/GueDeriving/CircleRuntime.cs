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

    public int Alpha
    {
        get => ContainedLineCircle.Color.A;
        set
        {
            ContainedLineCircle.Color = ColorExtensions.WithAlpha(ContainedLineCircle.Color, (byte)value);
            NotifyPropertyChanged();
        }
    }

    public int Red
    {
        get => ContainedLineCircle.Color.R;
        set
        {
            ContainedLineCircle.Color = ColorExtensions.WithRed(ContainedLineCircle.Color, (byte)value);
            NotifyPropertyChanged();
        }
    }

    public int Green
    {
        get => ContainedLineCircle.Color.G;
        set
        {
            ContainedLineCircle.Color = ColorExtensions.WithGreen(ContainedLineCircle.Color, (byte)value);
            NotifyPropertyChanged();
        }
    }

    public int Blue
    {
        get => ContainedLineCircle.Color.B;
        set
        {
            ContainedLineCircle.Color = ColorExtensions.WithBlue(ContainedLineCircle.Color, (byte)value);
            NotifyPropertyChanged();
        }
    }

    public float Radius
    {
        get => ContainedLineCircle.Radius;
        set
        {
            mWidth = value * 2;
            mHeight = value * 2;
            ContainedLineCircle.Radius = value;
        }
    }

    public Color Color
    {
#if XNALIKE
        get => global::RenderingLibrary.Graphics.XNAExtensions.ToXNA(ContainedLineCircle.Color);
        set
        {
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
    /// When non-null, the circle renders as a solid fill of this color. When null (the default),
    /// the circle renders as an outline via its <see cref="LineCircle"/> renderable.
    /// </summary>
    /// <remarks>
    /// Filled rendering requires the optional MonoGameGumShapes (Apos.Shapes) package, which
    /// registers a renderable factory with <see cref="ShapeRenderableRegistry"/>. When that
    /// package is not referenced, setting <see cref="FillColor"/> is a graceful no-op for the
    /// visual: the runtime stays on its outline renderable rather than crashing.
    ///
    /// Spike (#2758): proof-of-concept for the renderable-swap mechanism only. It does not yet
    /// route the existing Color/Radius/Alpha properties through a swapped-in fill renderable —
    /// that wiring is part of the actual ColoredCircle/Circle collapse, which is out of scope
    /// for the spike.
    /// </remarks>
    public Color? FillColor
    {
        get => _fillColor;
        set
        {
            _fillColor = value;
            UpdateRenderableFromFillColor();
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// Swaps the contained renderable to match the current <see cref="FillColor"/>: a
    /// fill-capable renderable (from <see cref="ShapeRenderableRegistry"/>) when FillColor is
    /// set, or the outline <see cref="LineCircle"/> when it is null. This is the core of the
    /// renderable-swap mechanism the spike exists to prove.
    /// </summary>
    void UpdateRenderableFromFillColor()
    {
        if (_fillColor.HasValue)
        {
            if (RenderableComponent is IFilledShapeRenderable existingFill)
            {
                // Already on a fill-capable renderable — just update it.
                existingFill.IsFilled = true;
                existingFill.Color = _fillColor.Value;
            }
            else
            {
                var factory = ShapeRenderableRegistry.CreateFilledCircleRenderable;
                if (factory != null)
                {
                    var fillRenderable = factory();
                    fillRenderable.IsFilled = true;
                    fillRenderable.Color = _fillColor.Value;
                    containedLineCircle = null;
                    SetContainedObject(fillRenderable);
                }
                // else: MonoGameGumShapes is not referenced, so no fill-capable renderable can
                // be created. Degrade gracefully — stay on the outline LineCircle, no crash.
            }
        }
        else if (RenderableComponent is not ContainedCircleType)
        {
            // FillColor cleared while on a fill renderable — swap back to the outline LineCircle.
            var lineCircle = new ContainedCircleType();
            lineCircle.CircleOrigin = CircleOrigin.TopLeft;
            lineCircle.Color = ColorExtensions.White;
            containedLineCircle = lineCircle;
            SetContainedObject(lineCircle);
        }
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
