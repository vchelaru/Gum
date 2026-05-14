using Gum.Wireframe;
using RenderingLibrary;
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
using SkiaGum.Renderables;
using Color = SkiaSharp.SKColor;
using ContainedLineRectangle = SkiaGum.Renderables.LineRectangle;
#else
using Color = Microsoft.Xna.Framework.Color;
using ColorExtensions = ToolsUtilitiesStandard.Helpers.ColorExtensions;
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
            ContainedLineRectangle.Color = ColorExtensions.WithAlpha(ContainedLineRectangle.Color, (byte)value);
            NotifyPropertyChanged();
        }
    }

    public int Red
    {
        get => ContainedLineRectangle.Color.R;
        set
        {
            ContainedLineRectangle.Color = ColorExtensions.WithRed(ContainedLineRectangle.Color, (byte)value);
            NotifyPropertyChanged();
        }
    }

    public int Green
    {
        get => ContainedLineRectangle.Color.G;
        set
        {
            ContainedLineRectangle.Color = ColorExtensions.WithGreen(ContainedLineRectangle.Color, (byte)value);
            NotifyPropertyChanged();
        }
    }

    public int Blue
    {
        get => ContainedLineRectangle.Color.B;
        set
        {
            ContainedLineRectangle.Color = ColorExtensions.WithBlue(ContainedLineRectangle.Color, (byte)value);
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

    public RectangleRuntime(bool fullInstantiation = true, SystemManagers? systemManagers = null)
    {
        if (fullInstantiation)
        {
            var rectangle = new ContainedLineRectangle(systemManagers);
            SetContainedObject(rectangle);
            containedLineRectangle = rectangle;

#if SKIA
            rectangle.Color = SkiaSharp.SKColors.White;
#else
            rectangle.Color = ColorExtensions.White;
#endif

            Width = 50;
            Height = 50;
        }
    }
}
