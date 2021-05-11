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
