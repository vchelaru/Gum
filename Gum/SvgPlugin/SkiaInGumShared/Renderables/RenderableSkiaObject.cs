using Gum.Converters;
using Gum.DataTypes;
using RenderingLibrary.Graphics;
using SkiaSharp;

using System;
using ToolsUtilitiesStandard.Helpers;
using Color = System.Drawing.Color;

namespace SkiaGum.Renderables
{
    public abstract class RenderableSkiaObject : ISkiaSurfaceDrawable
    {
        #region General Fields/Properties

        float width;
        public float Width
        {
            get => width;
            set
            {
                if (value != width)
                {
                    width = value;
                    needsUpdate = true;
                }
            }
        }

        float height;
        public float Height
        {
            get => height;
            set
            {
                if (value != height)
                {
                    height = value;
                    needsUpdate = true;
                }
            }
        }

        /// <summary>
        /// If this is false, then the DrawToSurface will handle applying the colors (like when creating a RoundedRectangle). If true,
        /// then this will multiply the rendering by the argument color (like when rendering an SVG).
        /// </summary>
        protected virtual bool ShouldApplyColorOnSpriteRender => false;

        bool ISkiaSurfaceDrawable.ShouldApplyColorOnSpriteRender => ShouldApplyColorOnSpriteRender;

        protected ColorOperation colorOperation = ColorOperation.Modulate;
        public ColorOperation ColorOperation
        {
            get => colorOperation;
            set => colorOperation = value;
        }

        protected bool needsUpdate = true;

        public bool NeedsUpdate
        {
            get => needsUpdate;
            set => needsUpdate = value;
        }

        #endregion

        #region Colors/Stroke

        bool isFilled = true;
        public bool IsFilled
        {
            get => isFilled;
            set { isFilled = value; needsUpdate = true; }
        }

        float strokeWidth = 1;
        public float StrokeWidth
        {
            get => strokeWidth;
            set { strokeWidth = value; needsUpdate = true; }
        }

        public int Red
        {
            get => Color.R;
            set
            {
                Color = Color.WithRed((byte)value);
                needsUpdate = true;
            }
        }

        public int Green
        {
            get => Color.G;
            set
            {
                Color = Color.WithGreen((byte)value);
                needsUpdate = true;
            }
        }

        public int Blue
        {
            get => Color.B;
            set
            {
                Color = Color.WithBlue((byte)value);
                needsUpdate = true;
            }
        }

        public Color Color { get; set; } = Color.White;

        public int Alpha
        {
            get => Color.A;
            set
            {
                Color = Color.WithAlpha((byte)value);
                needsUpdate = true;
            }
        }

        #endregion

        #region Gradients

        bool useGradient;
        public bool UseGradient
        {
            get => useGradient;
            set
            {
                if (value != useGradient)
                {
                    useGradient = value;
                    needsUpdate = true;
                }
            }
        }

        int alpha1 = 255;
        public int Alpha1
        {
            get => alpha1;
            set
            {
                if(alpha1 != value)
                {
                    alpha1 = value;
                    needsUpdate = true;
                }
            }
        }

        int red1;
        public int Red1
        {
            get => red1;
            set
            {
                if (red1 != value)
                {
                    red1 = value;
                    needsUpdate = true;
                }
            }
        }

        int green1;
        public int Green1
        {
            get => green1;
            set
            {
                if (green1 != value)
                {
                    green1 = value;
                    needsUpdate = true;
                }
            }
        }

        int blue1;
        public int Blue1
        {
            get => blue1;
            set
            {
                if (blue1 != value)
                {
                    blue1 = value;
                    needsUpdate = true;
                }
            }
        }


        int alpha2 = 255;
        public int Alpha2
        {
            get => alpha2;
            set
            {
                if (alpha2 != value)
                {
                    alpha2 = value;
                    needsUpdate = true;
                }
            }
        }

        int red2;
        public int Red2
        {
            get => red2;
            set
            {
                if (red2 != value)
                {
                    red2 = value;
                    needsUpdate = true;
                }
            }
        }

        int green2;
        public int Green2
        {
            get => green2;
            set
            {
                if (green2 != value)
                {
                    green2 = value;
                    needsUpdate = true;
                }
            }
        }

        int blue2;
        public int Blue2
        {
            get => blue2;
            set
            {
                if (blue2 != value)
                {
                    blue2 = value;
                    needsUpdate = true;
                }
            }
        }

        protected float gradientX1;
        public float GradientX1
        {
            get => gradientX1;
            set
            {
                if (value != gradientX1)
                {
                    gradientX1 = value;
                    needsUpdate = true;
                }
            }
        }

        protected float gradientY1;
        public float GradientY1
        {
            get => gradientY1;
            set
            {
                if (value != gradientY1)
                {
                    gradientY1 = value;
                    needsUpdate = true;
                }
            }
        }

        protected float gradientX2;
        public float GradientX2
        {
            get => gradientX2;
            set
            {
                if (value != gradientX2)
                {
                    gradientX2 = value;
                    needsUpdate = true;
                }
            }
        }

        protected float gradientY2;
        public float GradientY2
        {
            get => gradientY2;
            set
            {
                if (value != gradientY2)
                {
                    gradientY2 = value;
                    needsUpdate = true;
                }
            }
        }


        GeneralUnitType gradientX1Units;
        public GeneralUnitType GradientX1Units
        {
            get => gradientX1Units;
            set
            {
                if (value != gradientX1Units)
                {
                    gradientX1Units = value;
                    needsUpdate = true;
                }
            }
        }

        GeneralUnitType gradientX2Units;
        public GeneralUnitType GradientX2Units
        {
            get => gradientX2Units;
            set
            {
                if (value != gradientX2Units)
                {
                    gradientX2Units = value;
                    needsUpdate = true;
                }
            }
        }

        GeneralUnitType gradientY1Units;
        public GeneralUnitType GradientY1Units
        {
            get => gradientY1Units;
            set
            {
                if (value != gradientY1Units)
                {
                    gradientY1Units = value;
                    needsUpdate = true;
                }
            }
        }

        GeneralUnitType gradientY2Units;
        public GeneralUnitType GradientY2Units
        {
            get => gradientY2Units;
            set
            {
                if (value != gradientY2Units)
                {
                    gradientY2Units = value;
                    needsUpdate = true;
                }
            }
        }

        float gradientInnerRadius;
        public float GradientInnerRadius
        {
            get => gradientInnerRadius;
            set
            {
                if (value != gradientInnerRadius)
                {
                    gradientInnerRadius = value;
                    needsUpdate = true;
                }
            }
        }

        float gradientOuterRadius;
        public float GradientOuterRadius
        {
            get => gradientOuterRadius;
            set
            {
                if (value != gradientOuterRadius)
                {
                    gradientOuterRadius = value;
                    needsUpdate = true;
                }
            }
        }


        GradientType gradientType;
        public GradientType GradientType
        {
            get => gradientType;
            set
            {
                if (gradientType != value)
                {
                    gradientType = value;
                    needsUpdate = true;
                }
            }
        }

        protected DimensionUnitType gradientInnerRadiusUnits;
        public DimensionUnitType GradientInnerRadiusUnits
        {
            get => gradientInnerRadiusUnits;
            set
            {
                if (value != gradientInnerRadiusUnits)
                {
                    gradientInnerRadiusUnits = value;
                    needsUpdate = true;
                }
            }
        }

        protected DimensionUnitType gradientOuterRadiusUnits;
        public DimensionUnitType GradientOuterRadiusUnits
        {
            get => gradientOuterRadiusUnits;
            set
            {
                if (value != gradientOuterRadiusUnits)
                {
                    gradientOuterRadiusUnits = value;
                    needsUpdate = true;
                }
            }
        }

        #endregion

        #region Dropshadow

        public bool HasDropshadow { get; set; }

        public float DropshadowOffsetX { get; set; }
        public float DropshadowOffsetY { get; set; }

        public float DropshadowBlurX { get; set; }
        public float DropshadowBlurY { get; set; }


        public Color DropshadowColor = Color.White;

        public int DropshadowAlpha
        {
            get => DropshadowColor.A;
            set
            {
                DropshadowColor = DropshadowColor.WithAlpha((byte)value);
                needsUpdate = true;
            }
        }

        public int DropshadowRed
        {
            get => DropshadowColor.R;
            set
            {
                DropshadowColor = DropshadowColor.WithRed((byte)value);
                needsUpdate = true;
            }
        }

        public int DropshadowGreen
        {
            get => DropshadowColor.G;
            set
            {
                DropshadowColor = DropshadowColor.WithGreen((byte)value);
                needsUpdate = true;
            }
        }

        public int DropshadowBlue
        {
            get => DropshadowColor.B;
            set
            {
                DropshadowColor = DropshadowColor.WithBlue((byte)value);
                needsUpdate = true;
            }
        }

        /// <summary>
        /// The offset X when rendering a Skia shape considering the dropshadow
        /// </summary>
        public float XSizeSpillover => HasDropshadow ? DropshadowBlurX + Math.Abs(DropshadowOffsetX) : 0;

        /// <summary>
        /// The offset Y when rendering a Skia shape considering the dropshadow
        /// </summary>
        public float YSizeSpillover => HasDropshadow ? DropshadowBlurY + Math.Abs(DropshadowOffsetY) : 0;

        #endregion

        public virtual void PreRender() { }

        public abstract void DrawToSurface(SKSurface surface);

        protected void SetGradientOnPaint(SKPaint paint)
        {
            var firstColor = new SKColor((byte)red1, (byte)green1, (byte)blue1, (byte)alpha1);
            var secondColor = new SKColor((byte)red2, (byte)green2, (byte)blue2, (byte)alpha2);

            var effectiveWidth = Width + XSizeSpillover * 2;
            var effectiveHeight = Height + YSizeSpillover * 2;

            var effectiveGradientX1 = gradientX1;
            switch (this.GradientX1Units)
            {
                case GeneralUnitType.PixelsFromSmall:
                    effectiveGradientX1 += XSizeSpillover;
                    break;
                case GeneralUnitType.PixelsFromMiddle:
                    effectiveGradientX1 += effectiveWidth / 2.0f;
                    break;
                case GeneralUnitType.PixelsFromLarge:
                    effectiveGradientX1 += effectiveWidth;
                    break;
                case GeneralUnitType.Percentage:
                    effectiveGradientX1 = effectiveWidth * gradientX1 / 100;
                    break;
            }

            var effectiveGradientX2 = gradientX2;
            switch (this.GradientX2Units)
            {
                case GeneralUnitType.PixelsFromSmall:
                    effectiveGradientX2 += XSizeSpillover;
                    break;
                case GeneralUnitType.PixelsFromMiddle:
                    effectiveGradientX2 += effectiveWidth / 2.0f;
                    break;
                case GeneralUnitType.PixelsFromLarge:
                    effectiveGradientX2 += effectiveWidth;
                    break;
                case GeneralUnitType.Percentage:
                    effectiveGradientX2 = effectiveWidth * gradientX2 / 100;
                    break;
            }

            var effectiveGradientY1 = gradientY1;
            switch (this.GradientY1Units)
            {
                case GeneralUnitType.PixelsFromSmall:
                    effectiveGradientY1 += YSizeSpillover;
                    break;
                case GeneralUnitType.PixelsFromMiddle:
                    effectiveGradientY1 += effectiveHeight / 2.0f;
                    break;
                case GeneralUnitType.PixelsFromLarge:
                    effectiveGradientY1 += effectiveHeight;
                    break;
                case GeneralUnitType.Percentage:
                    effectiveGradientY1 = effectiveHeight * gradientY1 / 100;
                    break;
            }

            var effectiveGradientY2 = gradientY2;
            switch (this.GradientY2Units)
            {
                case GeneralUnitType.PixelsFromSmall:
                    effectiveGradientY2 += YSizeSpillover;
                    break;
                case GeneralUnitType.PixelsFromMiddle:
                    effectiveGradientY2 += effectiveHeight / 2.0f;
                    break;
                case GeneralUnitType.PixelsFromLarge:
                    effectiveGradientY2 += effectiveHeight;
                    break;
                case GeneralUnitType.Percentage:
                    effectiveGradientY2 = effectiveHeight * gradientY2 / 100;
                    break;
            }


            if (gradientType == GradientType.Linear)
            {

                paint.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(effectiveGradientX1, effectiveGradientY1), // left, top
                    new SKPoint(effectiveGradientX2, effectiveGradientY2), // right, bottom
                    new SKColor[] { firstColor, secondColor },
                    new float[] { 0, 1 },
                    SKShaderTileMode.Clamp);
            }
            else if (gradientType == GradientType.Radial)
            {
                var effectiveOuterRadius = gradientOuterRadius;

                switch (gradientOuterRadiusUnits)
                {
                    case Gum.DataTypes.DimensionUnitType.PercentageOfParent:
                        effectiveOuterRadius = effectiveWidth * gradientOuterRadius / 100;
                        break;
                    case Gum.DataTypes.DimensionUnitType.RelativeToParent:
                        effectiveOuterRadius = effectiveWidth / 2 + gradientOuterRadius;
                        break;
                }

                if (effectiveOuterRadius <= 0)
                {
                    effectiveOuterRadius = 100;
                }

                var effectiveInnerRadius = gradientInnerRadius;

                switch (gradientInnerRadiusUnits)
                {
                    case Gum.DataTypes.DimensionUnitType.PercentageOfParent:
                        effectiveInnerRadius = effectiveWidth * gradientInnerRadius / 100;
                        break;
                    case Gum.DataTypes.DimensionUnitType.RelativeToParent:
                        effectiveInnerRadius = effectiveWidth / 2 + gradientInnerRadius;
                        break;
                }

                var innerToOuterRatio = effectiveInnerRadius / effectiveOuterRadius;

                paint.Shader = SKShader.CreateRadialGradient(
                    new SKPoint(effectiveGradientX1, effectiveGradientY1), // center
                    effectiveOuterRadius,
                    new SKColor[] { firstColor, secondColor },
                    new float[] { innerToOuterRatio, 1 },
                    SKShaderTileMode.Clamp);
            }
        }

        protected virtual SKPaint CreatePaint()
        {
            var skColor = new SKColor(Color.R, Color.G, Color.B, Color.A);

            var paint = new SKPaint
            {
                Color = skColor,
                Style = IsFilled ? SKPaintStyle.Fill : SKPaintStyle.Stroke,
                StrokeWidth = StrokeWidth,
                IsAntialias = true
            };

            if (UseGradient)
            {
                SetGradientOnPaint(paint);
            }

            if (HasDropshadow)
            {
                var dropshadowSkColor = new SKColor(DropshadowColor.R, DropshadowColor.G, DropshadowColor.B, DropshadowColor.A);
                paint.ImageFilter = SKImageFilter.CreateDropShadow(
                            DropshadowOffsetX,
                            // See https://stackoverflow.com/questions/60456526/how-can-i-tell-the-amount-of-space-needed-for-a-skia-dropshadow
                            DropshadowOffsetY,
                            DropshadowBlurX / 3.0f,
                            DropshadowBlurY / 3.0f,
                            dropshadowSkColor);
            }

            return paint;
        }
    }
}
