using Gum.Converters;
using Gum.DataTypes;
using RenderingLibrary.Graphics;
using SkiaGum.Renderables;

namespace SkiaGum.GueDeriving
{
    public class RoundedRectangleRuntime : SkiaShapeRuntime
    {
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

        public float CornerRadius
        {
            get;
            set;
        }

        public DimensionUnitType CornerRadiusUnits
        {
            get; set;
        }

        #region Gradient Colors

        public int Alpha1
        {
            get => ContainedRoundedRectangle.Alpha1;
            set => ContainedRoundedRectangle.Alpha1 = value;
        }

        public int Blue1
        {
            get => ContainedRoundedRectangle.Blue1;
            set => ContainedRoundedRectangle.Blue1 = value;
        }

        public int Green1
        {
            get => ContainedRoundedRectangle.Green1;
            set => ContainedRoundedRectangle.Green1 = value;
        }

        public int Red1
        {
            get => ContainedRoundedRectangle.Red1;
            set => ContainedRoundedRectangle.Red1 = value;
        }

        public int Alpha2
        {
            get => ContainedRoundedRectangle.Alpha2;
            set => ContainedRoundedRectangle.Alpha2 = value;
        }

        public int Blue2
        {
            get => ContainedRoundedRectangle.Blue2;
            set => ContainedRoundedRectangle.Blue2 = value;
        }

        public int Green2
        {
            get => ContainedRoundedRectangle.Green2;
            set => ContainedRoundedRectangle.Green2 = value;
        }

        public int Red2
        {
            get => ContainedRoundedRectangle.Red2;
            set => ContainedRoundedRectangle.Red2 = value;
        }

        public float GradientX1
        {
            get => ContainedRoundedRectangle.GradientX1;
            set => ContainedRoundedRectangle.GradientX1 = value;
        }
        public GeneralUnitType GradientX1Units
        {
            get => ContainedRoundedRectangle.GradientX1Units;
            set => ContainedRoundedRectangle.GradientX1Units = value;
        }
        public float GradientY1
        {
            get => ContainedRoundedRectangle.GradientY1;
            set => ContainedRoundedRectangle.GradientY1 = value;
        }
        public GeneralUnitType GradientY1Units
        {
            get => ContainedRoundedRectangle.GradientY1Units;
            set => ContainedRoundedRectangle.GradientY1Units = value;
        }

        public float GradientX2
        {
            get => ContainedRoundedRectangle.GradientX2;
            set => ContainedRoundedRectangle.GradientX2 = value;
        }
        public GeneralUnitType GradientX2Units
        {
            get => ContainedRoundedRectangle.GradientX2Units;
            set => ContainedRoundedRectangle.GradientX2Units = value;
        }
        public float GradientY2
        {
            get => ContainedRoundedRectangle.GradientY2;
            set => ContainedRoundedRectangle.GradientY2 = value;
        }
        public GeneralUnitType GradientY2Units
        {
            get => ContainedRoundedRectangle.GradientY2Units;
            set => ContainedRoundedRectangle.GradientY2Units = value;
        }

        public bool UseGradient
        {
            get => ContainedRoundedRectangle.UseGradient;
            set => ContainedRoundedRectangle.UseGradient = value;
        }

        public GradientType GradientType
        {
            get => ContainedRoundedRectangle.GradientType;
            set => ContainedRoundedRectangle.GradientType = value;
        }

        public float GradientInnerRadius
        {
            get => ContainedRoundedRectangle.GradientInnerRadius;
            set => ContainedRoundedRectangle.GradientInnerRadius = value;
        }

        public DimensionUnitType GradientInnerRadiusUnits
        {
            get => ContainedRoundedRectangle.GradientInnerRadiusUnits;
            set => ContainedRoundedRectangle.GradientInnerRadiusUnits = value;
        }

        public float GradientOuterRadius
        {
            get => ContainedRoundedRectangle.GradientOuterRadius;
            set => ContainedRoundedRectangle.GradientOuterRadius = value;
        }

        public DimensionUnitType GradientOuterRadiusUnits
        {
            get => ContainedRoundedRectangle.GradientOuterRadiusUnits;
            set => ContainedRoundedRectangle.GradientOuterRadiusUnits = value;
        }

        #endregion


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

        public override void PreRender()
        {
            if (this.EffectiveManagers != null)
            {
                var camera = this.EffectiveManagers.Renderer.Camera;
                var cornerRadius = CornerRadius;
                switch (CornerRadiusUnits)
                {
                    case DimensionUnitType.Absolute:
                        // do nothing
                        break;
                    case DimensionUnitType.ScreenPixel:
                        cornerRadius /= camera.Zoom;
                        break;
                }
                ContainedRoundedRectangle.CornerRadius = cornerRadius;
            }
            base.PreRender();
        }

    }
}
