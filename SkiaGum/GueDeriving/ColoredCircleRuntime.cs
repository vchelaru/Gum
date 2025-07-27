using Gum.Converters;
using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.GueDeriving;

public class ColoredCircleRuntime : SkiaShapeRuntime
{
    protected override RenderableBase ContainedRenderable => ContainedCircle;

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

    #region Gradient Colors

    public int Blue1
    {
        get => ContainedCircle.Blue1;
        set => ContainedCircle.Blue1 = value;
    }

    public int Green1
    {
        get => ContainedCircle.Green1;
        set => ContainedCircle.Green1 = value;
    }

    public int Red1
    {
        get => ContainedCircle.Red1;
        set => ContainedCircle.Red1 = value;
    }


    public int Blue2
    {
        get => ContainedCircle.Blue2;
        set => ContainedCircle.Blue2 = value;
    }

    public int Green2
    {
        get => ContainedCircle.Green2;
        set => ContainedCircle.Green2 = value;
    }

    public int Red2
    {
        get => ContainedCircle.Red2;
        set => ContainedCircle.Red2 = value;
    }

    public float GradientX1
    {
        get => ContainedCircle.GradientX1;
        set => ContainedCircle.GradientX1 = value;
    }
    public GeneralUnitType GradientX1Units
    {
        get => ContainedCircle.GradientX1Units;
        set => ContainedCircle.GradientX1Units = value;
    }
    public float GradientY1
    {
        get => ContainedCircle.GradientY1;
        set => ContainedCircle.GradientY1 = value;
    }
    public GeneralUnitType GradientY1Units
    {
        get => ContainedCircle.GradientY1Units;
        set => ContainedCircle.GradientY1Units = value;
    }

    public float GradientX2
    {
        get => ContainedCircle.GradientX2;
        set => ContainedCircle.GradientX2 = value;
    }
    public GeneralUnitType GradientX2Units
    {
        get => ContainedCircle.GradientX2Units;
        set => ContainedCircle.GradientX2Units = value;
    }
    public float GradientY2
    {
        get => ContainedCircle.GradientY2;
        set => ContainedCircle.GradientY2 = value;
    }
    public GeneralUnitType GradientY2Units
    {
        get => ContainedCircle.GradientY2Units;
        set => ContainedCircle.GradientY2Units = value;
    }

    public bool UseGradient
    {
        get => ContainedCircle.UseGradient;
        set => ContainedCircle.UseGradient = value;
    }

    public GradientType GradientType
    {
        get => ContainedCircle.GradientType;
        set => ContainedCircle.GradientType = value;
    }

    public float GradientInnerRadius
    {
        get => ContainedCircle.GradientInnerRadius;
        set => ContainedCircle.GradientInnerRadius = value;
    }

    public DimensionUnitType GradientInnerRadiusUnits
    {
        get => ContainedCircle.GradientInnerRadiusUnits;
        set => ContainedCircle.GradientInnerRadiusUnits = value;
    }

    public float GradientOuterRadius
    {
        get => ContainedCircle.GradientOuterRadius;
        set => ContainedCircle.GradientOuterRadius = value;
    }

    public DimensionUnitType GradientOuterRadiusUnits
    {
        get => ContainedCircle.GradientOuterRadiusUnits;
        set => ContainedCircle.GradientOuterRadiusUnits = value;
    }

    #endregion


    public ColoredCircleRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
            SetContainedObject(new Circle());
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
        var toReturn = (ColoredCircleRuntime)base.Clone();

        toReturn.mContainedCircle = null;

        return toReturn;
    }
}
