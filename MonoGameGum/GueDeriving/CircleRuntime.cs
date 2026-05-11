#if MONOGAME || FNA || KNI
#define XNALIKE
#endif

using Gum.Wireframe;
using RenderingLibrary;
using System;

#if RAYLIB
using Gum.Renderables;
using Color = Raylib_cs.Color;
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
#if RAYLIB
            var color = ContainedLineCircle.Color;
            color.A = (byte)value;
            ContainedLineCircle.Color = color;
#else
            // ColorExtensions.WithAlpha is defined for System.Drawing.Color (the XNA-side LineCircle's Color type).
            // The new version of Glue is moving away from XNA color values. This code converts color values. If this doesn't run, you need to upgrade your GLUX version.
            // More info here: https://flatredball.com/documentation/tools/glue-reference/glujglux/
            var color = ToolsUtilitiesStandard.Helpers.ColorExtensions.WithAlpha(ContainedLineCircle.Color, (byte)value);
            ContainedLineCircle.Color = color;
#endif
            NotifyPropertyChanged();
        }
    }

    public int Red
    {
        get => ContainedLineCircle.Color.R;
        set
        {
#if RAYLIB
            var color = ContainedLineCircle.Color;
            color.R = (byte)value;
            ContainedLineCircle.Color = color;
#else
            var color = ToolsUtilitiesStandard.Helpers.ColorExtensions.WithRed(ContainedLineCircle.Color, (byte)value);
            ContainedLineCircle.Color = color;
#endif
            NotifyPropertyChanged();
        }
    }

    public int Green
    {
        get => ContainedLineCircle.Color.G;
        set
        {
#if RAYLIB
            var color = ContainedLineCircle.Color;
            color.G = (byte)value;
            ContainedLineCircle.Color = color;
#else
            var color = ToolsUtilitiesStandard.Helpers.ColorExtensions.WithGreen(ContainedLineCircle.Color, (byte)value);
            ContainedLineCircle.Color = color;
#endif
            NotifyPropertyChanged();
        }
    }

    public int Blue
    {
        get => ContainedLineCircle.Color.B;
        set
        {
#if RAYLIB
            var color = ContainedLineCircle.Color;
            color.B = (byte)value;
            ContainedLineCircle.Color = color;
#else
            var color = ToolsUtilitiesStandard.Helpers.ColorExtensions.WithBlue(ContainedLineCircle.Color, (byte)value);
            ContainedLineCircle.Color = color;
#endif
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
#elif RAYLIB
            circle.Color = Raylib_cs.Color.White;
#elif SOKOL
            circle.Color = SokolGum.Color.White;
#else
            circle.Color = System.Drawing.Color.White;
#endif
            Width = 32;
            Height = 32;
            circle.Radius = 16;
        }
    }
}
