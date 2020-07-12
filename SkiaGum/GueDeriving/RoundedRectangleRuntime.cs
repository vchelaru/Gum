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
