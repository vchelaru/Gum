﻿using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkiaGum.Renderables
{
    class Circle : RenderableBase
    {
        public SKColor Color
        {
            get; set;
        } = SKColors.Red;

        public bool IsDimmed { get; set; }

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

        SKPaint Paint
        {
            get
            {
                var effectiveColor = this.Color;
                if(IsDimmed)
                {
                    const double dimmingMuliplier = .9;

                    effectiveColor = new SKColor(
                        (byte)(this.Color.Red * dimmingMuliplier),
                        (byte)(this.Color.Green * dimmingMuliplier),
                        (byte)(this.Color.Blue * dimmingMuliplier),
                        this.Color.Alpha);
                }
                return new SKPaint
                {
                    Color = effectiveColor,
                    IsAntialias = true
                };
            }
        }

        public override void DrawBound(SKRect boundingRect, SKCanvas canvas)
        {
            using (var paint = Paint)
            {
                var radius = System.Math.Min(boundingRect.Width, boundingRect.Height) / 2.0f;
                canvas.DrawCircle(boundingRect.MidX, boundingRect.MidY, radius, Paint);
            }
        }
    }
}
