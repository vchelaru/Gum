using Gum.Converters;
using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.GueDeriving;

public abstract class SkiaShapeRuntime : BindableGue
{
    protected abstract Renderables.RenderableShapeBase ContainedRenderable { get; }

    #region Solid colors

    public new int Alpha
    {
        get => ContainedRenderable.Alpha;
        set => ContainedRenderable.Alpha = value;
    }

    public int Blue
    {
        get => ContainedRenderable.Blue;
        set => ContainedRenderable.Blue = value;
    }

    public int Green
    {
        get => ContainedRenderable.Green;
        set => ContainedRenderable.Green = value;
    }

    public int Red
    {
        get => ContainedRenderable.Red;
        set => ContainedRenderable.Red = value;
    }

    public SKColor Color
    {
        get => ContainedRenderable.Color;
        set => ContainedRenderable.Color = value;
    }
    #endregion

    #region Gradient Colors


    public int Blue1
    {
        get => ContainedRenderable.Blue1;
        set => ContainedRenderable.Blue1 = value;
    }

    public int Green1
    {
        get => ContainedRenderable.Green1;
        set => ContainedRenderable.Green1 = value;
    }

    public int Red1
    {
        get => ContainedRenderable.Red1;
        set => ContainedRenderable.Red1 = value;
    }

    public int Alpha1
    {
        get => ContainedRenderable.Alpha1;
        set => ContainedRenderable.Alpha1 = value;
    }

    public SKColor Color1
    {
        get => new SKColor((byte)Red1, (byte)Green1, (byte)Blue1, (byte)Alpha1);
        set
        {
            Red1 = value.Red;
            Green1 = value.Green;
            Blue1 = value.Blue;
            Alpha1 = value.Alpha;
        }
    }


    public int Blue2
    {
        get => ContainedRenderable.Blue2;
        set => ContainedRenderable.Blue2 = value;
    }

    public int Green2
    {
        get => ContainedRenderable.Green2;
        set => ContainedRenderable.Green2 = value;
    }

    public int Red2
    {
        get => ContainedRenderable.Red2;
        set => ContainedRenderable.Red2 = value;
    }

    public int Alpha2
    {
        get => ContainedRenderable.Alpha2;
        set => ContainedRenderable.Alpha2 = value;
    }

    public SKColor Color2
    {
        get => new SKColor((byte)Red2, (byte)Green2, (byte)Blue2, (byte)Alpha2);
        set
        {
            Red2 = value.Red;
            Green2 = value.Green;
            Blue2 = value.Blue;
            Alpha2 = value.Alpha;
        }
    }

    public float GradientX1
    {
        get => ContainedRenderable.GradientX1;
        set => ContainedRenderable.GradientX1 = value;
    }
    public GeneralUnitType GradientX1Units
    {
        get => ContainedRenderable.GradientX1Units;
        set => ContainedRenderable.GradientX1Units = value;
    }
    public float GradientY1
    {
        get => ContainedRenderable.GradientY1;
        set => ContainedRenderable.GradientY1 = value;
    }
    public GeneralUnitType GradientY1Units
    {
        get => ContainedRenderable.GradientY1Units;
        set => ContainedRenderable.GradientY1Units = value;
    }

    public float GradientX2
    {
        get => ContainedRenderable.GradientX2;
        set => ContainedRenderable.GradientX2 = value;
    }
    public GeneralUnitType GradientX2Units
    {
        get => ContainedRenderable.GradientX2Units;
        set => ContainedRenderable.GradientX2Units = value;
    }
    public float GradientY2
    {
        get => ContainedRenderable.GradientY2;
        set => ContainedRenderable.GradientY2 = value;
    }
    public GeneralUnitType GradientY2Units
    {
        get => ContainedRenderable.GradientY2Units;
        set => ContainedRenderable.GradientY2Units = value;
    }

    public bool UseGradient
    {
        get => ContainedRenderable.UseGradient;
        set => ContainedRenderable.UseGradient = value;
    }

    public GradientType GradientType
    {
        get => ContainedRenderable.GradientType;
        set => ContainedRenderable.GradientType = value;
    }

    public float GradientInnerRadius
    {
        get => ContainedRenderable.GradientInnerRadius;
        set => ContainedRenderable.GradientInnerRadius = value;
    }

    public DimensionUnitType GradientInnerRadiusUnits
    {
        get => ContainedRenderable.GradientInnerRadiusUnits;
        set => ContainedRenderable.GradientInnerRadiusUnits = value;
    }

    public float GradientOuterRadius
    {
        get => ContainedRenderable.GradientOuterRadius;
        set => ContainedRenderable.GradientOuterRadius = value;
    }

    public DimensionUnitType GradientOuterRadiusUnits
    {
        get => ContainedRenderable.GradientOuterRadiusUnits;
        set => ContainedRenderable.GradientOuterRadiusUnits = value;
    }



    #endregion

    #region Filled/Stroke

    public bool IsFilled
    {
        get => ContainedRenderable.IsFilled;
        set => ContainedRenderable.IsFilled = value;
    }

    // This should NOT modify the contained renderable stroke width directly.
    // Rather it should be a value that does not affect the underlying object until
    // pre-render happens where the StrokeWidthUnits can be adjusted too:
    public float StrokeWidth
    {
        get;
        set;
    }

    public DimensionUnitType StrokeWidthUnits
    {
        get;
        set;
    }

    #endregion

    #region Dropshadow

    public int DropshadowAlpha
    {
        get => ContainedRenderable.DropshadowAlpha;
        set => ContainedRenderable.DropshadowAlpha = value;
    }

    public int DropshadowBlue
    {
        get => ContainedRenderable.DropshadowBlue;
        set => ContainedRenderable.DropshadowBlue = value;
    }

    public int DropshadowGreen
    {
        get => ContainedRenderable.DropshadowGreen;
        set => ContainedRenderable.DropshadowGreen = value;
    }

    public int DropshadowRed
    {
        get => ContainedRenderable.DropshadowRed;
        set => ContainedRenderable.DropshadowRed = value;
    }


    public bool HasDropshadow
    {
        get => ContainedRenderable.HasDropshadow;
        set => ContainedRenderable.HasDropshadow = value;
    }

    public float DropshadowOffsetX
    {
        get => ContainedRenderable.DropshadowOffsetX;
        set => ContainedRenderable.DropshadowOffsetX = value;
    }
    public float DropshadowOffsetY
    {
        get => ContainedRenderable.DropshadowOffsetY;
        set => ContainedRenderable.DropshadowOffsetY = value;
    }

    public float DropshadowBlurX
    {
        get => ContainedRenderable.DropshadowBlurX;
        set => ContainedRenderable.DropshadowBlurX = value;
    }
    public float DropshadowBlurY
    {
        get => ContainedRenderable.DropshadowBlurY;
        set => ContainedRenderable.DropshadowBlurY = value;
    }

    #endregion

    public override void PreRender()
    {
        var strokeWidth = StrokeWidth;

        switch (StrokeWidthUnits)
        {
            case DimensionUnitType.Absolute:
                // do nothing
                break;
            case DimensionUnitType.ScreenPixel:
                if (this.EffectiveManagers != null)
                {
                    var camera = this.EffectiveManagers.Renderer.Camera;
                    strokeWidth /= camera.Zoom;
                }
                break;
        }

        ContainedRenderable.StrokeWidth = strokeWidth;
        base.PreRender();
    }
}
