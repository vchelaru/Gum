using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.GueDeriving;

public class CircleRuntime : BindableGue
{
    LineCircle containedLineCircle;

    LineCircle ContainedLineCircle
    {
        get
        {
            if(containedLineCircle == null)
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
            // The new version of Glue is moving away from XNA color values. This code converts color values. If this doesn't run, you need to upgrade your GLUX version.
            // More info here: https://flatredball.com/documentation/tools/glue-reference/glujglux/
            var color = ToolsUtilitiesStandard.Helpers.ColorExtensions.WithAlpha(ContainedLineCircle.Color, (byte)value);
            ContainedLineCircle.Color = color;
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
            // The new version of Glue is moving away from XNA color values. This code converts color values. If this doesn't run, you need to upgrade your GLUX version.
            // More info here: https://flatredball.com/documentation/tools/glue-reference/glujglux/
            var color = ToolsUtilitiesStandard.Helpers.ColorExtensions.WithBlue(ContainedLineCircle.Color, (byte)value);
            ContainedLineCircle.Color = color;
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
            // The new version of Glue is moving away from XNA color values. This code converts color values. If this doesn't run, you need to upgrade your GLUX version.
            // More info here: https://flatredball.com/documentation/tools/glue-reference/glujglux/
            var color = ToolsUtilitiesStandard.Helpers.ColorExtensions.WithGreen(ContainedLineCircle.Color, (byte)value);
            ContainedLineCircle.Color = color;
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
            // The new version of Glue is moving away from XNA color values. This code converts color values. If this doesn't run, you need to upgrade your GLUX version.
            // More info here: https://flatredball.com/documentation/tools/glue-reference/glujglux/
            var color = ToolsUtilitiesStandard.Helpers.ColorExtensions.WithRed(ContainedLineCircle.Color, (byte)value);
            ContainedLineCircle.Color = color;
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


    public Microsoft.Xna.Framework.Color Color
    {
        get
        {
            return RenderingLibrary.Graphics.XNAExtensions.ToXNA(ContainedLineCircle.Color);
        }
        set
        {
            ContainedLineCircle.Color = RenderingLibrary.Graphics.XNAExtensions.ToSystemDrawing(value);
            NotifyPropertyChanged();
        }
    }

    public void AddToManagers() => base.AddToManagers(SystemManagers.Default, layer: null);

    public CircleRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
            var circle = new RenderingLibrary.Math.Geometry.LineCircle();
            SetContainedObject(circle);
            containedLineCircle = circle;

            circle.Color = System.Drawing.Color.White;
            Width = 16;
            Height = 16;
            circle.Radius = 16;
        }
    }
}
