using SkiaGum.Renderables;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkiaGum.GueDeriving
{
    public class ArcRuntime : BindableGraphicalUiElement
    {
        Arc mContainedArc;
        Arc ContainedArc
        {
            get
            {
                if(mContainedArc == null)
                {
                    mContainedArc = this.RenderableComponent as Arc;
                }
                return mContainedArc;
            }
        }

        #region Solid colors

        public int Alpha
        {
            get => ContainedArc.Alpha;
            set => ContainedArc.Alpha = value;
        }

        public int Blue
        {
            get => ContainedArc.Blue;
            set => ContainedArc.Blue = value;
        }

        public int Green
        {
            get => ContainedArc.Green;
            set => ContainedArc.Green = value;
        }

        public int Red
        {
            get => ContainedArc.Red;
            set => ContainedArc.Red = value;
        }

        public SKColor Color
        {
            get => ContainedArc.Color;
            set => ContainedArc.Color = value;
        }
        #endregion

        #region Gradient Colors


        public int Blue1
        {
            get => ContainedArc.Blue1;
            set => ContainedArc.Blue1 = value;
        }

        public int Green1
        {
            get => ContainedArc.Green1;
            set => ContainedArc.Green1 = value;
        }

        public int Red1
        {
            get => ContainedArc.Red1;
            set => ContainedArc.Red1 = value;
        }


        public int Blue2
        {
            get => ContainedArc.Blue2;
            set => ContainedArc.Blue2 = value;
        }

        public int Green2
        {
            get => ContainedArc.Green2;
            set => ContainedArc.Green2 = value;
        }

        public int Red2
        {
            get => ContainedArc.Red2;
            set => ContainedArc.Red2 = value;
        }

        public float GradientX1
        {
            get => ContainedArc.GradientX1;
            set => ContainedArc.GradientX1 = value;
        }
        public float GradientY1
        {
            get => ContainedArc.GradientY1;
            set => ContainedArc.GradientY1 = value;
        }


        public float GradientX2
        {
            get => ContainedArc.GradientX2;
            set => ContainedArc.GradientX2 = value;
        }
        public float GradientY2
        {
            get => ContainedArc.GradientY2;
            set => ContainedArc.GradientY2 = value;
        }

        public bool UseGradient
        {
            get => ContainedArc.UseGradient;
            set => ContainedArc.UseGradient = value;
        }

        public bool IsEndRounded
        {
            get => ContainedArc.IsEndRounded;
            set => ContainedArc.IsEndRounded = value;
        }

        public GradientType GradientType
        {
            get => ContainedArc.GradientType;
            set => ContainedArc.GradientType = value;
        }

        public float GradientInnerRadius
        {
            get => ContainedArc.GradientInnerRadius;
            set => ContainedArc.GradientInnerRadius = value;
        }

        public float GradientOuterRadius
        {
            get => ContainedArc.GradientOuterRadius;
            set => ContainedArc.GradientOuterRadius = value;
        }

        #endregion


        public float Thickness
        {
            get => ContainedArc.Thickness;
            set => ContainedArc.Thickness = value;
        }

        public float StartAngle
        {
            get => ContainedArc.StartAngle;
            set => ContainedArc.StartAngle = value;
        } 

        public float SweepAngle
        {
            get => ContainedArc.SweepAngle;
            set => ContainedArc.SweepAngle = value;
        }

        public ArcRuntime(bool fullInstantiation = true)
        {
            if (fullInstantiation)
            {
                SetContainedObject(new Arc());
                this.Color = SKColors.White;
                Width = 100;
                Height = 100;
            }
        }

    }
}
