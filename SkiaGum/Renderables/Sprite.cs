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
    public class Sprite : IRenderableIpso, IVisible
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

        public SKBitmap Texture { get; set; }

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

        public Rectangle? SourceRectangle;


        public Rectangle? EffectiveRectangle
        {
            get
            {
                Rectangle? sourceRectangle = SourceRectangle;

                return sourceRectangle;
            }
        }

        public float AspectRatio => Texture != null ? (Texture.Width / (float)Texture.Height) : 1.0f;

        public float Rotation { get; set; }


        public bool Wrap => false;


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

        public object Tag { get; set; }


        public Sprite()
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
                SKMatrix scaleMatrix = SKMatrix.MakeScale(1, 1);
                //// Gum uses counter clockwise rotation, Skia uses clockwise, so invert:
                SKMatrix rotationMatrix = SKMatrix.MakeRotationDegrees(-Rotation);
                SKMatrix translateMatrix = SKMatrix.MakeTranslation(this.GetAbsoluteX(), this.GetAbsoluteY());
                SKMatrix result = SKMatrix.MakeIdentity();

                SKMatrix.Concat(
                    ref result, rotationMatrix, scaleMatrix);
                SKMatrix.Concat(
                    ref result, translateMatrix, result);
                canvas.Save();
                canvas.SetMatrix(result);

                var destination = new SKRect(0, 0, Width, Height);

                canvas.DrawBitmap(Texture, destination);
                canvas.Restore();
            }
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

    }
}
