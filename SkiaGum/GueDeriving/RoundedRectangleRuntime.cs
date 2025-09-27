using Gum.Converters;
using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.GueDeriving;

public class RoundedRectangleRuntime : SkiaShapeRuntime, IClipPath
{
    #region Contained Renderable
    protected override RenderableBase ContainedRenderable => ContainedRoundedRectangle;

    RoundedRectangle mContainedRoundedRectangle;
    RoundedRectangle ContainedRoundedRectangle
    {
        get
        {
            if (mContainedRoundedRectangle == null)
            {
                mContainedRoundedRectangle = this.RenderableComponent as RoundedRectangle;
            }
            return mContainedRoundedRectangle;
        }
    }

    #endregion

    public float CornerRadius
    {
        get;
        set;
    }


    public float? CustomRadiusTopLeft { get; set; } = null;
    public float? CustomRadiusTopRight { get; set; } = null;
    public float? CustomRadiusBottomRight { get; set; } = null;
    public float? CustomRadiusBottomLeft { get; set; } = null;

    public DimensionUnitType CornerRadiusUnits
    {
        get; set;
    }

    public SKPath GetClipPath() => ContainedRoundedRectangle.GetClipPath();

    public RoundedRectangleRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
            SetContainedObject(new RoundedRectangle());

            // Make defaults 100 to match Glue
            Width = 100;
            Height = 100;

            DropshadowAlpha = 255;
            DropshadowRed = 0;
            DropshadowGreen = 0;
            DropshadowBlue = 0;

            CornerRadius = 5;
            DropshadowOffsetX = 0;
            DropshadowOffsetY = 3;
            DropshadowBlurX = 0;
            DropshadowBlurY = 3;

            GradientType = GradientType.Linear;

            GradientX1 = 0;
            GradientX1Units = GeneralUnitType.PixelsFromSmall;

            GradientY1 = 0;
            GradientY1Units = GeneralUnitType.PixelsFromSmall;

            Red1 = 255;
            Green1 = 255;
            Blue1 = 255;

            GradientX2 = 100;
            GradientX2Units = GeneralUnitType.PixelsFromSmall;

            GradientY2 = 100;
            GradientY2Units = GeneralUnitType.PixelsFromSmall;

            Red2 = 255;
            Green2 = 255;
            Blue2 = 0;
        }
    }

    public override GraphicalUiElement Clone()
    {
        var toReturn = (RoundedRectangleRuntime)base.Clone();

        toReturn.mContainedRoundedRectangle = null;

        return toReturn;
    }

    public override void PreRender()
    {
        if (this.EffectiveManagers != null)
        {
            var camera = this.EffectiveManagers.Renderer.Camera;
            var cornerRadius = CornerRadius;
            var topLeft = CustomRadiusTopLeft;
            var topRight = CustomRadiusTopRight;
            var bottomLeft = CustomRadiusBottomLeft;
            var bottomRight = CustomRadiusBottomRight;

            switch (CornerRadiusUnits)
            {
                case DimensionUnitType.Absolute:
                    // do nothing
                    break;
                case DimensionUnitType.ScreenPixel:
                    cornerRadius /= camera.Zoom;

                    topLeft /= camera.Zoom;
                    topRight /= camera.Zoom;
                    bottomLeft /= camera.Zoom;
                    bottomRight /= camera.Zoom;

                    break;
            }
            ContainedRoundedRectangle.CornerRadius = cornerRadius;
            ContainedRoundedRectangle.CustomRadiusTopLeft = topLeft;
            ContainedRoundedRectangle.CustomRadiusTopRight = topRight;
            ContainedRoundedRectangle.CustomRadiusBottomLeft = bottomLeft;
            ContainedRoundedRectangle.CustomRadiusBottomRight = bottomRight;
        }
        base.PreRender();
    }

}
