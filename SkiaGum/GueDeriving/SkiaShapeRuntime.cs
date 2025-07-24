using Gum.Converters;
using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.GueDeriving;

public abstract class SkiaShapeRuntime : BindableGue
{
    protected abstract RenderableBase ContainedRenderable { get; }

    #region Solid colors

    public int Alpha
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


    int Blue1 { get;set; }

    int Green1 { get; set; }

    int Red1 { get; set; }


    int Blue2 { get; set; }

    int Green2 { get; set; }

    int Red2 { get; set; }

    float GradientX1 { get; set; }
    GeneralUnitType GradientX1Units { get; set; }

    float GradientY1 { get; set; }
    GeneralUnitType GradientY1Units { get; set; }

    float GradientX2 { get; set; }
    float GradientY2 { get; set; }

    bool UseGradient { get; set; }

    bool IsEndRounded { get; set; }

    GradientType GradientType { get; set; }

    float GradientInnerRadius { get; set; }

    DimensionUnitType GradientInnerRadiusUnits { get; set; }

    float GradientOuterRadius { get; set; }

    DimensionUnitType GradientOuterRadiusUnits { get; set; }



    #endregion


    #region Filled/Stroke

    public bool IsFilled
    {
        get => ContainedRenderable.IsFilled;
        set => ContainedRenderable.IsFilled = value;
    }

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
        if(this.EffectiveManagers != null)
        {
            var camera = this.EffectiveManagers.Renderer.Camera;
            var strokeWidth = StrokeWidth;

            switch(StrokeWidthUnits)
            {
                case DimensionUnitType.Absolute:
                    // do nothing
                    break;
                case DimensionUnitType.ScreenPixel:
                    strokeWidth /= camera.Zoom;
                    break;
            }

            ContainedRenderable.StrokeWidth = strokeWidth;
        }
        base.PreRender();
    }
}
