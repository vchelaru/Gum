﻿using SkiaGum.Renderables;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkiaGum.GueDeriving
{
    public class ColoredRectangleRuntime :  BindableGraphicalUiElement
    {
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


        public ColoredRectangleRuntime(bool fullInstantiation = true)
        {
            if (fullInstantiation)
            {
                SetContainedObject(new RoundedRectangle());

                ContainedRoundedRectangle.CornerRadius = 0;
                ContainedRoundedRectangle.Color = SKColors.White;
                Width = 50;
                Height = 50;
            }
        }
    }
}
