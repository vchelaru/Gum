using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Graphics;
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

                canvas.DrawPicture(Texture.Picture, ref result);
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
