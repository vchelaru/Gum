using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using SkiaGum.GueDeriving;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace SkiaGum.Renderables
{
    public enum GradientType
    {
        Linear,
        Radial
    }

    public class RenderableBase : IRenderableIpso, IVisible
    {
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
            get { return Position.X; }
            set { Position.X = value; }
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


        public ColorOperation ColorOperation { get; set; } = ColorOperation.Modulate;


        public bool UseGradient { get; set; }

        public GradientType GradientType { get; set; }

        public int Red1 { get; set; }
        public int Green1 { get; set; }
        public int Blue1 { get; set; }

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

        public RenderableBase()
        {
            Width = 32;
            Height = 32;
            this.Visible = true;
            mChildren = new ObservableCollection<IRenderableIpso>();

        }

        public void Render(SKCanvas canvas)
        {
            if (AbsoluteVisible && Width > 0 && Height > 0)
            {
                var absoluteX = this.GetAbsoluteX();
                var absoluteY = this.GetAbsoluteY();
                var rect = new SKRect(absoluteX, absoluteY, absoluteX + this.Width, absoluteY + this.Height);

                DrawBound(rect, canvas);
            }
        }

        public virtual void DrawBound(SKRect boundingRect, SKCanvas canvas)
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

        protected void ApplyGradientToPaint(SKRect boundingRect, SKPaint paint)
        {
            var firstColor = new SKColor((byte)Red1, (byte)Green1, (byte)Blue1);
            var secondColor = new SKColor((byte)Red2, (byte)Green2, (byte)Blue2);

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

            var effectiveGradientX2 = GradientX1;
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

            effectiveGradientX1 += boundingRect.Left;
            effectiveGradientY1 += boundingRect.Top;
            effectiveGradientX2 += boundingRect.Left;
            effectiveGradientY2 += boundingRect.Top;

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

    }
}
