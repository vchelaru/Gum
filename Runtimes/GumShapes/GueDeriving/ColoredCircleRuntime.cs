using Gum.Forms.DefaultVisuals;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameAndGum.Renderables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.GueDeriving;

/// <summary>
/// Runtime that draws a circle (or ellipse) sized by its Width and Height. Adds no properties beyond
/// the AposShapeRuntime common set - color, gradient, drop shadow, and fill/stroke are inherited.
/// </summary>
public class ColoredCircleRuntime : AposShapeRuntime
{
    protected override RenderableShapeBase ContainedRenderable => ContainedCircle;

    Circle mContainedCircle = default!;
    Circle ContainedCircle
    {
        get
        {
            if (mContainedCircle == null)
            {
                mContainedCircle = (Circle)this.RenderableComponent;
            }
            return mContainedCircle;
        }
    }

    /// <summary>
    /// Initializes a new ColoredCircleRuntime. When fullInstantiation is true (the default), an underlying
    /// Apos.Shapes Circle renderable is created and default values are applied (Width = Height = 100,
    /// IsFilled = true, StrokeWidth = 1, Color = White).
    /// Pass false only when the runtime is being constructed by deserialization, which sets up
    /// the renderable separately.
    /// </summary>
    public ColoredCircleRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
            SetContainedShape(new Circle());
            StrokeWidth = 1;
            IsFilled = true;
            this.Color = Color.White;
            Width = 100;
            Height = 100;

            Red1 = 255;
            Green1 = 255;
            Blue1 = 255;

            Red2 = 255;
            Green2 = 255;
            Blue2 = 0;

            GradientX1 = 0;
            GradientY1 = 0;
            GradientX2 = 100;
            GradientY2 = 100;

            DropshadowAlpha = 255;
            DropshadowRed = 0;
            DropshadowGreen = 0;
            DropshadowBlue = 0;

            DropshadowOffsetX = 0;
            DropshadowOffsetY = 3;
            DropshadowBlurX = 0;
            DropshadowBlurY = 3;
        }
    }


    public override GraphicalUiElement Clone()
    {
        var toReturn = (ColoredCircleRuntime)base.Clone();

        // Should this call SetContainedObject?
        if(this.mContainedCircle != null)
        {
            SetContainedShape(new Circle());
        }

        return toReturn;
    }
}
