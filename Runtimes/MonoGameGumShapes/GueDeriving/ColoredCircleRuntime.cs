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

public class ColoredCircleRuntime : AposShapeRuntime
{
    protected override AposShapeBase ContainedRenderable => ContainedCircle;

    Circle mContainedCircle;
    Circle ContainedCircle
    {
        get
        {
            if (mContainedCircle == null)
            {
                mContainedCircle = this.RenderableComponent as Circle;
            }
            return mContainedCircle;
        }
    }

    public ColoredCircleRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
            SetContainedObject(new Circle());
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

        toReturn.mContainedCircle = null;

        return toReturn;
    }
}
