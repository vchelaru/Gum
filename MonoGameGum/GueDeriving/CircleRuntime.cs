using Gum.Wireframe;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if RAYLIB
using Gum.Renderables;
using Color = Raylib_cs.Color;
#elif SOKOL
using Gum.Renderables;
using Color = SokolGum.Color;
#elif SKIA
using SkiaGum.Renderables;
using Color = SkiaSharp.SKColor;
#else
using Gum.RenderingLibrary;
using Color = Microsoft.Xna.Framework.Color;
using global::RenderingLibrary.Math.Geometry;
#endif

#if FRB
namespace MonoGameGum.GueDeriving;
#else
namespace Gum.GueDeriving;
#endif

public class CircleRuntime : GraphicalUiElement
{
    LineCircle containedLineCircle;

    LineCircle ContainedLineCircle
    {
        get
        {
            if (containedLineCircle == null)
            {
                containedLineCircle = this.RenderableComponent as LineCircle;
            }
            return containedLineCircle;
        }
    }

    public int Alpha
    {
        get
        {
            return ContainedLineCircle.Color.A;
        }
        set
        {
#if RAYLIB
            var color = ContainedLineCircle.Color;
            color.A = (byte)value;
            ContainedLineCircle.Color = color;
#else
            // The new version of Glue is moving away from XNA color values. This code converts color values. If this doesn't run, you need to upgrade your GLUX version.
            // More info here: https://flatredball.com/documentation/tools/glue-reference/glujglux/
            var color = ToolsUtilitiesStandard.Helpers.ColorExtensions.WithAlpha(ContainedLineCircle.Color, (byte)value);
            ContainedLineCircle.Color = color;
#endif
        }
    }
    public int Blue
    {
        get
        {
            return ContainedLineCircle.Color.B;
        }
        set
        {
#if RAYLIB
            var color = ContainedLineCircle.Color;
            color.B = (byte)value;
            ContainedLineCircle.Color = color;
#else
            // The new version of Glue is moving away from XNA color values. This code converts color values. If this doesn't run, you need to upgrade your GLUX version.
            // More info here: https://flatredball.com/documentation/tools/glue-reference/glujglux/
            var color = ToolsUtilitiesStandard.Helpers.ColorExtensions.WithBlue(ContainedLineCircle.Color, (byte)value);
            ContainedLineCircle.Color = color;
#endif
        }
    }
    public int Green
    {
        get
        {
            return ContainedLineCircle.Color.G;
        }
        set
        {
#if RAYLIB
            var color = ContainedLineCircle.Color;
            color.G = (byte)value;
            ContainedLineCircle.Color = color;
#else
            // The new version of Glue is moving away from XNA color values. This code converts color values. If this doesn't run, you need to upgrade your GLUX version.
            // More info here: https://flatredball.com/documentation/tools/glue-reference/glujglux/
            var color = ToolsUtilitiesStandard.Helpers.ColorExtensions.WithGreen(ContainedLineCircle.Color, (byte)value);
            ContainedLineCircle.Color = color;
#endif
        }
    }

    public int Red
    {
        get
        {
            return ContainedLineCircle.Color.R;
        }
        set
        {
#if RAYLIB
            var color = ContainedLineCircle.Color;
            color.R = (byte)value;
            ContainedLineCircle.Color = color;
#else
            // The new version of Glue is moving away from XNA color values. This code converts color values. If this doesn't run, you need to upgrade your GLUX version.
            // More info here: https://flatredball.com/documentation/tools/glue-reference/glujglux/
            var color = ToolsUtilitiesStandard.Helpers.ColorExtensions.WithRed(ContainedLineCircle.Color, (byte)value);
            ContainedLineCircle.Color = color;
#endif
        }
    }

    public float Radius
    {
        get
        {
            return ContainedLineCircle.Radius;
        }
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
         get
        {
            return global::RenderingLibrary.Graphics.XNAExtensions.ToXNA(ContainedLineCircle.Color);
        }
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
            var circle = new LineCircle();
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
