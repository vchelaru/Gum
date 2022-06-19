using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using SkiaGum.GueDeriving;
using SkiaSharp;
using SkiaSharp.Extended.Svg;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using SKSvg = SkiaSharp.Extended.Svg.SKSvg;

namespace SkiaGum
{
    public class VectorSprite : IRenderableIpso, IVisible
    {
        #region Fields/Properties

        public SKColor Color
        {
            get; set;
        } = SKColors.White;

        Vector2 Position;
        IRenderableIpso mParent;

        public SKSvg Texture
        {
            get; set;
        }

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

        public object Tag { get; set; }


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

        public ColorOperation ColorOperation { get; set; } = ColorOperation.Modulate;

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

        #endregion

        public VectorSprite()
        {
            Width = 32;
            Height = 32;
            this.Visible = true;
            mChildren = new ObservableCollection<IRenderableIpso>();

        }

        public void Render(SKCanvas canvas)
        {
            if (AbsoluteVisible)
            {
                var textureBox = Texture.ViewBox;
                var textureWidth = textureBox.Width;
                var textureHeight = textureBox.Height;

                var scaleX = this.Width / textureWidth;
                var scaleY = this.Height / textureHeight;

                SKMatrix scaleMatrix = SKMatrix.MakeScale(scaleX, scaleY);
                // Gum uses counter clockwise rotation, Skia uses clockwise, so invert:
                SKMatrix rotationMatrix = SKMatrix.MakeRotationDegrees(-Rotation);
                SKMatrix translateMatrix = SKMatrix.MakeTranslation(this.GetAbsoluteX(), this.GetAbsoluteY());
                SKMatrix result = SKMatrix.MakeIdentity();

                SKMatrix.Concat(
                    ref result, rotationMatrix, scaleMatrix);
                SKMatrix.Concat(
                    ref result, translateMatrix, result);

                // Currently this supports "multiply". Other color operations could be supported...
                if (Color.Red != 255 || Color.Green != 255 || Color.Blue != 255 || Color.Alpha != 255)
                {
                    var paint = new SKPaint() { Color = Color };
                    var redRatio = Color.Red / 255.0f;
                    var greenRatio = Color.Green / 255.0f;
                    var blueRatio = Color.Blue / 255.0f;

                    paint.ColorFilter =
                        SKColorFilter.CreateColorMatrix(new float[]
                        {
                        redRatio   , 0            , 0        , 0, 0,
                        0,           greenRatio   , 0        , 0, 0,
                        0,           0            , blueRatio, 0, 0,
                        0,           0            , 0        , 1, 0
                        });

                    using (paint)
                    {
                        canvas.DrawPicture(Texture.Picture, ref result, paint);
                    }
                }
                else
                {
                    canvas.DrawPicture(Texture.Picture, ref result);
                }
            }
        }

        public void PreRender() { }

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
    }
}
