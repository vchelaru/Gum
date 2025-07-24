using Gum.Converters;
using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.GueDeriving
{
    public class ArcRuntime : SkiaShapeRuntime
    {
        protected override RenderableBase ContainedRenderable => ContainedArc;

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
        public GeneralUnitType GradientX1Units
        {
            get => ContainedArc.GradientX1Units;
            set => ContainedArc.GradientX1Units = value;
        }

        public float GradientY1
        {
            get => ContainedArc.GradientY1;
            set => ContainedArc.GradientY1 = value;
        }
        public GeneralUnitType GradientY1Units
        {
            get => ContainedArc.GradientY1Units;
            set => ContainedArc.GradientY1Units = value;
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

        public DimensionUnitType GradientInnerRadiusUnits
        {
            get => ContainedArc.GradientInnerRadiusUnits;
            set => ContainedArc.GradientInnerRadiusUnits = value;
        }

        public float GradientOuterRadius
        {
            get => ContainedArc.GradientOuterRadius;
            set => ContainedArc.GradientOuterRadius = value;
        }

        public DimensionUnitType GradientOuterRadiusUnits
        {
            get => ContainedArc.GradientOuterRadiusUnits;
            set => ContainedArc.GradientOuterRadiusUnits = value;
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

                Red1 = 255;
                Green1 = 255;
                Blue1 = 255;

                Red2 = 255;
                Green2 = 255;
                Blue2 = 0;

                GradientX2 = 100;
                GradientY2 = 100;
            }
        }

        public override GraphicalUiElement Clone()
        {
            var toReturn = (ArcRuntime)base.Clone();

            toReturn.mContainedArc = null;

            return toReturn;
        }

    }
}
