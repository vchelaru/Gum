#if MONOGAME || FNA || KNI
#define XNALIKE
#endif

using Gum.Wireframe;
using RenderingLibrary;
using System;


#if RAYLIB
using Gum.Renderables;
using Color = Raylib_cs.Color;
using ContainedLineRectangle = Gum.Renderables.LineRectangle;
#elif SOKOL
using Gum.Renderables;
using Color = SokolGum.Color;
using ContainedLineRectangle = Gum.Renderables.LineRectangle;
#elif SKIA
using SkiaGum.Renderables;
using Color = SkiaSharp.SKColor;
using ContainedLineRectangle = SkiaGum.Renderables.LineRectangle;
#else
using Color = Microsoft.Xna.Framework.Color;
using ContainedLineRectangle = global::RenderingLibrary.Math.Geometry.LineRectangle;
using global::RenderingLibrary.Math.Geometry;
#endif

#if FRB
namespace MonoGameGum.GueDeriving;
#else
namespace Gum.GueDeriving;
#endif

public class RectangleRuntime : GraphicalUiElement
{
    ContainedLineRectangle containedLineRectangle;
    ContainedLineRectangle ContainedLineRectangle
    {
        get
        {
            if (containedLineRectangle == null)
            {
                containedLineRectangle = this.RenderableComponent as ContainedLineRectangle;
            }
            return containedLineRectangle;
        }
    }

    public float LineWidth
    {
       get => ContainedLineRectangle.LinePixelWidth;
       set
       {
           ContainedLineRectangle.LinePixelWidth = value;
           NotifyPropertyChanged();
       }
       
    }

    public bool IsDotted
    {
        get => ContainedLineRectangle.IsDotted;
        set
        {
            ContainedLineRectangle.IsDotted = value;
            NotifyPropertyChanged();
        }
    }

    public int Alpha
    {
        get => ContainedLineRectangle.Color.A;
        set
        {
#if RAYLIB
            var color = ContainedLineRectangle.Color;
            color.A = (byte)value;
            ContainedLineRectangle.Color = color;
#else
            // ColorExtensions.WithAlpha is defined for System.Drawing.Color (the XNA-side LineCircle's Color type).
            // The new version of Glue is moving away from XNA color values. This code converts color values. If this doesn't run, you need to upgrade your GLUX version.
            // More info here: https://flatredball.com/documentation/tools/glue-reference/glujglux/
            var color = ToolsUtilitiesStandard.Helpers.ColorExtensions.WithAlpha(ContainedLineRectangle.Color, (byte)value);
            ContainedLineRectangle.Color = color;
#endif
            NotifyPropertyChanged();
        }
    }

    public int Red
    {
        get => ContainedLineRectangle.Color.R;
        set
        {
#if RAYLIB
            var color = ContainedLineRectangle.Color;
            color.R = (byte)value;
            ContainedLineRectangle.Color = color;
#else
            var color = ToolsUtilitiesStandard.Helpers.ColorExtensions.WithRed(ContainedLineRectangle.Color, (byte)value);
            ContainedLineRectangle.Color = color;
#endif
            NotifyPropertyChanged();
        }
    }

    public int Green
    {
        get => ContainedLineRectangle.Color.G;
        set
        {
#if RAYLIB
            var color = ContainedLineRectangle.Color;
            color.G = (byte)value;
            ContainedLineRectangle.Color = color;
#else
            var color = ToolsUtilitiesStandard.Helpers.ColorExtensions.WithGreen(ContainedLineRectangle.Color, (byte)value);
            ContainedLineRectangle.Color = color;
#endif
            NotifyPropertyChanged();
        }
    }

    public int Blue
    {
        get => ContainedLineRectangle.Color.B;
        set
        {
#if RAYLIB
            var color = ContainedLineRectangle.Color;
            color.B = (byte)value;
            ContainedLineRectangle.Color = color;
#else
            var color = ToolsUtilitiesStandard.Helpers.ColorExtensions.WithBlue(ContainedLineRectangle.Color, (byte)value);
            ContainedLineRectangle.Color = color;
#endif
            NotifyPropertyChanged();
        }
    }
    public Color Color
    {
#if XNALIKE
        get => global::RenderingLibrary.Graphics.XNAExtensions.ToXNA(ContainedLineRectangle.Color);
        set
        {
            ContainedLineRectangle.Color = global::RenderingLibrary.Graphics.XNAExtensions.ToSystemDrawing(value);
            NotifyPropertyChanged();
        }
#else
        get => ContainedLineRectangle.Color;
        set
        {
            ContainedLineRectangle.Color = value;
            NotifyPropertyChanged();
        }
#endif
    }

    /// <inheritdoc cref="GraphicalUiElement.AddToManagers()"/>
    [Obsolete("Use the AddToRoot extension method instead (e.g. myRectangle.AddToRoot()).")]
    public void AddToManagers() => base.AddToManagers(SystemManagers.Default, layer: null);

    public RectangleRuntime(bool fullInstantiation = true, SystemManagers systemManagers = null)
    {
        if (fullInstantiation)
        {
            var rectangle = new ContainedLineRectangle();
            SetContainedObject(rectangle);
            containedLineRectangle = rectangle;

#if SKIA
            rectangle.Color = SkiaSharp.SKColors.White;
#elif RAYLIB
            rectangle.Color = Raylib_cs.Color.White;
#elif SOKOL
            rectangle.Color = SokolGum.Color.White;
#else
            rectangle.Color = System.Drawing.Color.White;
#endif

            Width = 50;
            Height = 50;
        }
    }
}
