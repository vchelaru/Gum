using Gum.Converters;
using Gum.DataTypes;
using SkiaGum.Renderables;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkiaGum.GueDeriving
{
    public class RoundedRectangleRuntime : BindableGraphicalUiElement
    {
        RoundedRectangle mContainedRoundedRectangle;
        RoundedRectangle ContainedRoundedRectangle
        {
            get
            {
                if(mContainedRoundedRectangle == null)
                {
                    mContainedRoundedRectangle = this.RenderableComponent as RoundedRectangle;
                }
                return mContainedRoundedRectangle;
            }
        }

        public int Alpha
        {
            get => ContainedRoundedRectangle.Alpha;
            set => ContainedRoundedRectangle.Alpha = value;
        }

        public int Blue
        {
            get => ContainedRoundedRectangle.Blue;
            set => ContainedRoundedRectangle.Blue = value;
        }

        public int Green
        {
            get => ContainedRoundedRectangle.Green;
            set => ContainedRoundedRectangle.Green = value;
        }

        public int Red
        {
            get => ContainedRoundedRectangle.Red;
            set => ContainedRoundedRectangle.Red = value;
        }

        public SKColor Color
        {
            get => ContainedRoundedRectangle.Color;
            set => ContainedRoundedRectangle.Color = value;
        }



        public int DropshadowAlpha
        {
            get => ContainedRoundedRectangle.DropshadowAlpha;
            set => ContainedRoundedRectangle.DropshadowAlpha = value;
        }

        public int DropshadowBlue
        {
            get => ContainedRoundedRectangle.DropshadowBlue;
            set => ContainedRoundedRectangle.DropshadowBlue = value;
        }

        public int DropshadowGreen
        {
            get => ContainedRoundedRectangle.DropshadowGreen;
            set => ContainedRoundedRectangle.DropshadowGreen = value;
        }

        public int DropshadowRed
        {
            get => ContainedRoundedRectangle.DropshadowRed;
            set => ContainedRoundedRectangle.DropshadowRed = value;
        }

        public float CornerRadius 
        {
            get => ContainedRoundedRectangle.CornerRadius;
            set => ContainedRoundedRectangle.CornerRadius = value;
        }


        public bool HasDropshadow
        {
            get => ContainedRoundedRectangle.HasDropshadow;
            set => ContainedRoundedRectangle.HasDropshadow = value;
        }

        public float DropshadowOffsetX
        {
            get => ContainedRoundedRectangle.DropshadowOffsetX;
            set => ContainedRoundedRectangle.DropshadowOffsetX = value;
        }
        public float DropshadowOffsetY
        {
            get => ContainedRoundedRectangle.DropshadowOffsetY;
            set => ContainedRoundedRectangle.DropshadowOffsetY = value;
        }

        public float DropshadowBlurX
        {
            get => ContainedRoundedRectangle.DropshadowBlurX;
            set => ContainedRoundedRectangle.DropshadowBlurX = value;
        }
        public float DropshadowBlurY
        {
            get => ContainedRoundedRectangle.DropshadowBlurY;
            set => ContainedRoundedRectangle.DropshadowBlurY = value;
        }

        // Doesn't do anything....yet
        public bool IsFilled
        {
            get => ContainedRoundedRectangle.IsFilled;
            set => ContainedRoundedRectangle.IsFilled = value;
        }

        // doesn't do anything yet
        public float StrokeWidth
        {
            get => ContainedRoundedRectangle.StrokeWidth;
            set => ContainedRoundedRectangle.StrokeWidth = value;
        }

        #region Gradient Colors

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
            if(fullInstantiation)
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


            }
        }

    }
}
