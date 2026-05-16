using Gum.Converters;
using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaGum.Renderables;
using SkiaSharp;

#if FRB
namespace SkiaGum.GueDeriving;
#else
namespace Gum.GueDeriving;
#endif

public class ColoredCircleRuntime : SkiaShapeRuntime
{
    protected override RenderableShapeBase ContainedRenderable => ContainedCircle;

    SkiaGum.Renderables.Circle mContainedCircle;
    SkiaGum.Renderables.Circle ContainedCircle
    {
        get
        {
            if (mContainedCircle == null)
            {
                mContainedCircle = this.RenderableComponent as SkiaGum.Renderables.Circle;
            }
            return mContainedCircle;
        }
    }

    public bool IsDimmed 
    {
        get => ContainedCircle.IsDimmed;
        set => ContainedCircle.IsDimmed = value;
    }



    public ColoredCircleRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
            SetContainedObject(new Circle());
#pragma warning disable CS0618 // Color is obsolete; migration to FillColor/StrokeColor tracked in #2790 (depends on two-slot composition).
            this.Color = SKColors.White;
#pragma warning restore CS0618
            Width = 100;
            Height = 100;

            GradientX1 = 0;
            GradientY1 = 0;

            GradientX2 = 100;
            GradientY2 = 100;

            Red1 = 255;
            Green1 = 255;
            Blue1 = 255;

            Red2 = 255;
            Green2 = 255;
            Blue2 = 0;

            // Dropshadow is false, but let's keep the alpha at 255 so if the user sets it to true, it "just works"
            DropshadowAlpha = 255;
            DropshadowOffsetY = 3;
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
