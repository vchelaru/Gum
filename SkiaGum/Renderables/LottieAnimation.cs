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
    internal class LottieAnimation : IRenderableIpso, IVisible
    {
        public SkiaSharp.Skottie.Animation Animation
        {
            get; set;
        }

        public object Tag { get; set; }


        ObservableCollection<IRenderableIpso> mChildren;
        public ObservableCollection<IRenderableIpso> Children
        {
            get { return mChildren; }
        }

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

        Vector2 Position;

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

        void IRenderableIpso.SetParentDirect(IRenderableIpso parent) => mParent = parent;

        public LottieAnimation()
        {
            mChildren = new ObservableCollection<IRenderableIpso>();
        }

        public void PreRender() { }

        public void Render(SKCanvas canvas)
        {
            if(AbsoluteVisible && Animation != null)
            {
                var textureBox = Animation.Size;
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

                Animation.Render(canvas, new SKRect(0, 0, 1, 1));
            }
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
