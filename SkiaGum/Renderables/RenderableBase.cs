using Gum.Converters;
using Gum.DataTypes;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;
using SkiaGum.GueDeriving;
using SkiaSharp;
using System.Collections.ObjectModel;
using ToolsUtilitiesStandard.Helpers;
using Vector2 = System.Numerics.Vector2;
using Matrix = System.Numerics.Matrix4x4;

namespace SkiaGum.Renderables
{
    public class RenderableBase : IRenderableIpso, IVisible
    {
        #region Fields/Properties

        public SKColor Color
        {
            get; set;
        } = SKColors.Red;

        public int Alpha
        {
            get => Color.Alpha;
            set
            {
                this.Color = new SKColor(this.Color.Red, this.Color.Green, this.Color.Blue, (byte)value);
            }
        }

        public int Blue
        {
            get => Color.Blue;
            set
            {
                this.Color = new SKColor(this.Color.Red, this.Color.Green, (byte)value, this.Color.Alpha);
            }
        }

        public int Green
        {
            get => Color.Green;
            set
            {
                this.Color = new SKColor(this.Color.Red, (byte)value, this.Color.Blue, this.Color.Alpha);
            }
        }

        public int Red
        {
            get => Color.Red;
            set
            {
                this.Color = new SKColor((byte)value, this.Color.Green, this.Color.Blue, this.Color.Alpha);
            }
        }

        Vector2 Position;
        IRenderableIpso mParent;

        public IRenderableIpso Parent
        {
            get { return mParent; }
            set
            {
                if (mParent != value)
                {
                    if (mParent != null)
                    {
                        mParent.Children.Remove(this);
                    }
                    mParent = value;
                    if (mParent != null)
                    {
                        mParent.Children.Add(this);
                    }
                }
            }
        }

        ObservableCollection<IRenderableIpso> mChildren;
        public ObservableCollection<IRenderableIpso> Children
        {
            get { return mChildren; }
        }

        public float X
        {
            get => Position.X;
            set => Position.X = value;
        }

        public float Y
        {
            get { return Position.Y; }
            set { Position.Y = value; }
        }

        public float Z
        {
            get;
            set;
        }
        public float Width
        {
            get;
            set;
        }

        public float Height
        {
            get;
            set;
        }
        public string Name
        {
            get;
            set;
        }
        public float Rotation { get; set; }

        public bool Wrap => false;

        protected bool CanRenderAt0Dimension { get; set; } = false;
        protected bool IsOffsetAppliedForStroke { get; set; } = true;

        public ColorOperation ColorOperation { get; set; } = ColorOperation.Modulate;


        public bool UseGradient { get; set; }

        public GradientType GradientType { get; set; }

        public int Alpha1 { get; set; }
        public int Red1 { get; set; }
        public int Green1 { get; set; }
        public int Blue1 { get; set; }

        public int Alpha2 { get; set; }
        public int Red2 { get; set; }
        public int Green2 { get; set; }
        public int Blue2 { get; set; }

        public float GradientX1 { get; set; }
        public GeneralUnitType GradientX1Units { get; set; }
        public float GradientY1 { get; set; }
        public GeneralUnitType GradientY1Units { get; set; }

        public float GradientX2 { get; set; }
        public GeneralUnitType GradientX2Units { get; set; }

        public float GradientY2 { get; set; }
        public GeneralUnitType GradientY2Units { get; set; }


        public float GradientInnerRadius { get; set; }
        public DimensionUnitType GradientInnerRadiusUnits { get; set; }

        public float GradientOuterRadius { get; set; }
        public DimensionUnitType GradientOuterRadiusUnits { get; set; }

        public bool IsDimmed { get; set; }


        public bool IsFilled { get; set; } = true;
        public float StrokeWidth { get; set; } = 2;

        #region Dropshadow

        public SKColor DropshadowColor
        {
            get; set;
        }

        public int DropshadowAlpha
        {
            get => DropshadowColor.Alpha;
            set
            {
                this.DropshadowColor = new SKColor(this.DropshadowColor.Red, this.DropshadowColor.Green, this.DropshadowColor.Blue, (byte)value);
            }
        }

        public int DropshadowBlue
        {
            get => DropshadowColor.Blue;
            set
            {
                this.DropshadowColor = new SKColor(this.DropshadowColor.Red, this.DropshadowColor.Green, (byte)value, this.DropshadowColor.Alpha);
            }
        }

        public int DropshadowGreen
        {
            get => DropshadowColor.Green;
            set
            {
                this.DropshadowColor = new SKColor(this.DropshadowColor.Red, (byte)value, this.DropshadowColor.Blue, this.DropshadowColor.Alpha);
            }
        }

        public int DropshadowRed
        {
            get => DropshadowColor.Red;
            set
            {
                this.DropshadowColor = new SKColor((byte)value, this.DropshadowColor.Green, this.DropshadowColor.Blue, this.DropshadowColor.Alpha);
            }
        }

        public bool HasDropshadow { get; set; }

        public float DropshadowOffsetX { get; set; }
        public float DropshadowOffsetY { get; set; }

        public float DropshadowBlurX { get; set; }
        public float DropshadowBlurY { get; set; }

        #endregion


        public bool FlipHorizontal
        {
            get;
            set;
        }

        public bool FlipVertical
        {
            get;
            set;
        }

        public object Tag { get; set; }

        #endregion

        public RenderableBase()
        {
            Width = 32;
            Height = 32; 
            Alpha = 255;
            Alpha1 = 255;
            Alpha2 = 255;
            this.Visible = true;
            mChildren = new ObservableCollection<IRenderableIpso>();

        }

        public void PreRender() {}

#if SKIA
        public void Render(SKCanvas canvas)
        {
            var canRender =
                AbsoluteVisible &&
                    ((Width > 0 && Height > 0) || CanRenderAt0Dimension);
            if (canRender)
            {
                var absoluteX = this.GetAbsoluteX();
                var absoluteY = this.GetAbsoluteY();
                var boundingRect = new SKRect(absoluteX, absoluteY, absoluteX + this.Width, absoluteY + this.Height);

                var rotation = this.GetAbsoluteRotation();
                var applyRotation = rotation != 0;
                if (applyRotation)
                {
                    var oldX = boundingRect.Left;
                    var oldY = boundingRect.Top;

                    canvas.Save();

                    boundingRect.Left = 0;
                    boundingRect.Right -= oldX;
                    boundingRect.Top = 0;
                    boundingRect.Bottom -= oldY;

                    canvas.Translate(oldX, oldY);
                    canvas.RotateDegrees(-rotation);
                }

                // If this is stroke-only, then the stroke is centered around the bounds 
                // we pass in. Therefore, we need to move the bounds "in" by half of the 
                // stroke width
                if (IsFilled == false && IsOffsetAppliedForStroke)
                {
                    boundingRect.Left += StrokeWidth / 2.0f;
                    boundingRect.Top += StrokeWidth / 2.0f;
                    boundingRect.Right -= StrokeWidth / 2.0f;
                    boundingRect.Bottom -= StrokeWidth / 2.0f;
                }

                DrawBound(boundingRect, canvas, this.GetAbsoluteRotation());

                if (applyRotation)
                {
                    canvas.Restore();
                }
            }
        }
#else
        public BlendState BlendState
        {
            get
            {
                return BlendState.AlphaBlend; //?

            }

        }
        public void Render(SpriteRenderer spriteRenderer, SystemManagers managers)
        {

        }
#endif
        public bool ClipsChildren { get; set; }

        protected virtual SKPaint GetPaint(SKRect boundingRect, float absoluteRotation)
        {
            var effectiveColor = this.Color;
            if (IsDimmed)
            {
                const double dimmingMuliplier = .9;

                effectiveColor = new SKColor(
                    (byte)(this.Color.Red * dimmingMuliplier),
                    (byte)(this.Color.Green * dimmingMuliplier),
                    (byte)(this.Color.Blue * dimmingMuliplier),
                    this.Color.Alpha);
            }



            var paint = new SKPaint
            {
                Color = effectiveColor,
                Style = IsFilled ? SKPaintStyle.Fill : SKPaintStyle.Stroke,
                StrokeWidth = StrokeWidth,
                IsAntialias = true
            };

            if (HasDropshadow)
            {
                paint.ImageFilter = SKImageFilter.CreateDropShadow(
                            DropshadowOffsetX,
                            // See https://stackoverflow.com/questions/60456526/how-can-i-tell-the-amount-of-space-needed-for-a-skia-dropshadow
                            DropshadowOffsetY,
                            DropshadowBlurX / 3.0f,
                            DropshadowBlurY / 3.0f,
                            DropshadowColor,
                            SKDropShadowImageFilterShadowMode.DrawShadowAndForeground);
            }

            if (UseGradient)
            {
                ApplyGradientToPaint(boundingRect, paint, absoluteRotation);
            }

            return paint;
        }

        public virtual void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
        {

        }

        void IRenderableIpso.SetParentDirect(IRenderableIpso parent)
        {
            mParent = parent;
        }

        #region IVisible Implementation

        public bool Visible
        {
            get;
            set;
        }

        public bool AbsoluteVisible
        {
            get
            {
                if (((IVisible)this).Parent == null)
                {
                    return Visible;
                }
                else
                {
                    return Visible && ((IVisible)this).Parent.AbsoluteVisible;
                }
            }
        }

        IVisible IVisible.Parent
        {
            get
            {
                return ((IRenderableIpso)this).Parent as IVisible;
            }
        }

#endregion

        protected void ApplyGradientToPaint(SKRect boundingRect, SKPaint paint, float absoluteRotation)
        {
            var firstColor = new SKColor((byte)Red1, (byte)Green1, (byte)Blue1, (byte)Alpha1);
            var secondColor = new SKColor((byte)Red2, (byte)Green2, (byte)Blue2, (byte)Alpha2);

            var effectiveGradientX1 = GradientX1;
            switch (this.GradientX1Units)
            {
                case GeneralUnitType.PixelsFromMiddle:
                    effectiveGradientX1 += Width / 2.0f;
                    break;
                case GeneralUnitType.PixelsFromLarge:
                    effectiveGradientX1 += Width;
                    break;
                case GeneralUnitType.Percentage:
                    effectiveGradientX1 = Width * GradientX1 / 100;
                    break;
            }

            var effectiveGradientX2 = GradientX2;
            switch (this.GradientX2Units)
            {
                case GeneralUnitType.PixelsFromMiddle:
                    effectiveGradientX2 += Width / 2.0f;
                    break;
                case GeneralUnitType.PixelsFromLarge:
                    effectiveGradientX2 += Width;
                    break;
                case GeneralUnitType.Percentage:
                    effectiveGradientX2 = Width * GradientX2 / 100;
                    break;
            }

            var effectiveGradientY1 = GradientY1;
            switch (this.GradientY1Units)
            {
                case GeneralUnitType.PixelsFromMiddle:
                    effectiveGradientY1 += Height / 2.0f;
                    break;
                case GeneralUnitType.PixelsFromLarge:
                    effectiveGradientY1 += Height;
                    break;
                case GeneralUnitType.Percentage:
                    effectiveGradientY1 = Height * GradientY1 / 100;
                    break;
            }

            var effectiveGradientY2 = GradientY2;
            switch (this.GradientY2Units)
            {
                case GeneralUnitType.PixelsFromMiddle:
                    effectiveGradientY2 += Height / 2.0f;
                    break;
                case GeneralUnitType.PixelsFromLarge:
                    effectiveGradientY2 += Height;
                    break;
                case GeneralUnitType.Percentage:
                    effectiveGradientY2 = Height * GradientY2 / 100;
                    break;
            }

            var rectToUse = boundingRect;
            if (absoluteRotation != 0)
            {
                rectToUse = Unrotate(boundingRect, absoluteRotation);
            }
            else
            {
                // If we apply rotation, then the camera coordinates are adjusted such that the gradient coordiantes are relative to the object.
                // Otherwise, they are not so we need to offset:
                effectiveGradientX1 += rectToUse.Left;
                effectiveGradientY1 += rectToUse.Top;
                effectiveGradientX2 += rectToUse.Left;
                effectiveGradientY2 += rectToUse.Top;
            }

            if (GradientType == GradientType.Linear)
            {

                paint.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(effectiveGradientX1, effectiveGradientY1), // left, top
                    new SKPoint(effectiveGradientX2, effectiveGradientY2), // right, bottom
                    new SKColor[] { firstColor, secondColor },
                    new float[] { 0, 1 },
                    SKShaderTileMode.Clamp);
            }
            else if (GradientType == GradientType.Radial)
            {
                var effectiveOuterRadius = GradientOuterRadius;

                switch (GradientOuterRadiusUnits)
                {
                    case Gum.DataTypes.DimensionUnitType.Percentage:
                        effectiveOuterRadius = Width * GradientOuterRadius / 100;
                        break;
                    case Gum.DataTypes.DimensionUnitType.RelativeToContainer:
                        effectiveOuterRadius = Width / 2 + GradientOuterRadius;
                        break;
                }

                if (effectiveOuterRadius <= 0)
                {
                    effectiveOuterRadius = 100;
                }

                var effectiveInnerRadius = GradientInnerRadius;

                switch (GradientInnerRadiusUnits)
                {
                    case Gum.DataTypes.DimensionUnitType.Percentage:
                        effectiveInnerRadius = Width * GradientInnerRadius / 100;
                        break;
                    case Gum.DataTypes.DimensionUnitType.RelativeToContainer:
                        effectiveInnerRadius = Width / 2 + GradientInnerRadius;
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

        SKRect Unrotate(SKRect beforeRotation, float angleToUndoDegrees)
        {
            var pointBefore = new Vector2(beforeRotation.Left, beforeRotation.Top);

            var middleOffset = new Vector2(beforeRotation.Width / 2.0f, beforeRotation.Height / 2.0f);
            MathFunctions.RotateVector(ref middleOffset, MathHelper.ToRadians(-angleToUndoDegrees));

            var middle = pointBefore + middleOffset;

            // convert to a "positive-Y-is-up" system:
            pointBefore.Y *= -1;
            middle.Y *= -1;
            MathFunctions.RotatePointAroundPoint(middle, ref pointBefore, MathHelper.ToRadians(-angleToUndoDegrees));
            pointBefore.Y *= -1;

            return new SKRect(pointBefore.X, pointBefore.Y, pointBefore.X + beforeRotation.Width, pointBefore.Y + beforeRotation.Height);
        }
    }
}
