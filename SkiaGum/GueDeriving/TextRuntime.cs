using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace SkiaGum.GueDeriving
{
    public class TextRuntime : BindableGraphicalUiElement
    {
        public enum ColorCategory
        {
            White,
            DefaultColor,
            LightBlue,
            LightGray
        }

        ColorCategory mColorCategoryState;
        public ColorCategory ColorCategoryState
        {
            get => mColorCategoryState;
            set
            {
                mColorCategoryState = value;
                switch (value)
                {
                    case ColorCategory.White:
                        this.Blue = 255;
                        this.Green = 255;
                        this.Red = 255;
                        break;
                    case ColorCategory.DefaultColor:
                        this.Blue = 100;
                        this.Green = 90;
                        this.Red = 69;
                        break;
                    case ColorCategory.LightBlue:
                        this.Blue = 193;
                        this.Green = 145;
                        this.Red = 0;
                        break;
                    case ColorCategory.LightGray:
                        this.Blue = 227;
                        this.Green = 226;
                        this.Red = 226;
                        break;
                }
            }
        }

        Text mContainedText;
        Text ContainedText
        {
            get
            {
                if(mContainedText == null)
                {
                    mContainedText = this.RenderableComponent as Text;
                }
                return mContainedText;
            }
        }

        public string Text
        {
            get => ContainedText.RawText;
            set => ContainedText.RawText = value;
        }
        public SKColor Color
        {
            get => ContainedText.Color;
            set => ContainedText.Color = value;
        }

        public int Blue
        {
            get => ContainedText.Blue;
            set => ContainedText.Blue = value;
        }

        public int Green
        {
            get => ContainedText.Green;
            set => ContainedText.Green = value;
        }

        public int Red
        {
            get => ContainedText.Red;
            set => ContainedText.Red = value;
        }

        public int Alpha
        {
            get => ContainedText.Alpha;
            set => ContainedText.Alpha = value;
        }


        public float FontScale
        {
            get => ContainedText.FontScale;
            set => ContainedText.FontScale = value;
        }

        public int? MaximumNumberOfLines
        {
            get => ContainedText.MaximumNumberOfLines;
            set => ContainedText.MaximumNumberOfLines = value;
        }

        public new bool IsBold
        {
            get => mContainedText.BoldWeight > 1;
            set
            {
                if(value)
                {
                    mContainedText.BoldWeight = 1.5f;
                }
                else
                {
                    mContainedText.BoldWeight = 1;
                }
            }
        }

        public float BoldWeight
        {
            get => mContainedText.BoldWeight;
            set => mContainedText.BoldWeight = value;
        }
        public HorizontalAlignment HorizontalAlignment
        {
            get => ContainedText.HorizontalAlignment;
            set => ContainedText.HorizontalAlignment = value;
        }

        public VerticalAlignment VerticalAlignment
        {
            get => ContainedText.VerticalAlignment;
            set => ContainedText.VerticalAlignment = value;
        }

        //public SKTypeface FontType
        //{
        //    get => ContainedText.Font;
        //    set => ContainedText.Font = value;
        //}

        public TextRuntime (bool fullInstantiation = true)
        {
            if(fullInstantiation)
            {
                SetContainedObject(new Text());

                this.Height = 0;
                this.HeightUnits = DimensionUnitType.RelativeToChildren;


                // These values are default values matching Gum defaults. Not sure how to handle this - ultimately the Gum project
                // could change these values, in which case these would no longer be valid. We need a way to push the default states
                // from Gum here. But...for now at least we'll match defaults:
                FontSize = 18;

                Red = 255;
                Green = 255;
                Blue = 255;

                this.Text = "Hello";

                Width = 100;
            }
        }
    }
}
