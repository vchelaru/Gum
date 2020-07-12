using SkiaGum.Renderables;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkiaGum.GueDeriving
{
    public class ColoredCircleRuntime : BindableGraphicalUiElement
    {
        Circle mContainedCircle;
        Circle ContainedCircle
        {
            get
            {
                if (mContainedCircle == null)
                {
                    mContainedCircle = this.RenderableComponent as Circle;
                }
                return mContainedCircle;
            }
        }

        public int Alpha
        {
            get => ContainedCircle.Alpha;
            set => ContainedCircle.Alpha = value;
        }

        public int Blue
        {
            get => ContainedCircle.Blue;
            set => ContainedCircle.Blue = value;
        }

        public int Green
        {
            get => ContainedCircle.Green;
            set => ContainedCircle.Green = value;
        }

        public int Red
        {
            get => ContainedCircle.Red;
            set => ContainedCircle.Red = value;
        }

        public SKColor Color
        {
            get => ContainedCircle.Color;
            set => ContainedCircle.Color = value;
        }

        public ColoredCircleRuntime(bool fullInstantiation = true)
        {
            if (fullInstantiation)
            {
                SetContainedObject(new Circle());
                this.Color = SKColors.White;
                Width = 100;
                Height = 100;
            }
        }
    }
}
