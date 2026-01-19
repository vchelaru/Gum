using Gum.Wireframe;
using SkiaGum.Renderables;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkiaGum.GueDeriving;
public class CircleRuntime : SkiaShapeRuntime
{
    protected override RenderableShapeBase ContainedRenderable => ContainedCircle;

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

    public CircleRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
            SetContainedObject(new Circle());
            ContainedCircle.StrokeWidth = 1;
            StrokeWidthUnits = Gum.DataTypes.DimensionUnitType.ScreenPixel;

            ContainedCircle.IsFilled = false;
            this.Color = SKColors.White;
            Width = 100;
            Height = 100;

            // Dropshadow is false, but let's keep the alpha at 255 so if the user sets it to true, it "just works"
            DropshadowAlpha = 255;
            DropshadowOffsetY = 3;
            DropshadowBlurY = 3;
        }
    }


    public override GraphicalUiElement Clone()
    {
        var toReturn = (CircleRuntime)base.Clone();

        toReturn.mContainedCircle = null;

        return toReturn;
    }
}
