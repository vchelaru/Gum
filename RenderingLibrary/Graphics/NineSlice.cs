using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RenderingLibrary.Graphics
{
    public class NineSlice : IPositionedSizedObject, IRenderable, IVisible
    {
        #region Fields


        List<IPositionedSizedObject> mChildren = new List<IPositionedSizedObject>();

        Vector2 Position;

        IPositionedSizedObject mParent;

        Sprite mTopLeftSprite = new Sprite(null);
        Sprite mTopSprite = new Sprite(null);
        Sprite mTopRightSprite = new Sprite(null);
        Sprite mRightSprite = new Sprite(null);
        Sprite mBottomRightSprite = new Sprite(null);
        Sprite mBottomSprite = new Sprite(null);
        Sprite mBottomLeftSprite = new Sprite(null);
        Sprite mLeftSprite = new Sprite(null);
        Sprite mCenterSprite = new Sprite(null);

        #endregion

        #region Properties

        public string Name
        {
            get;
            set;
        }
        public object Tag { get; set; }

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

        public float EffectiveWidth
        {
            get
            {
                // I think we want to treat these individually so a 
                // width could be set but height could be default
                if (Width != 0)
                {
                    return Width;
                }
                else if (LeftTexture != null && CenterTexture != null && RightTexture != null)
                {
                    return LeftTexture.Width + CenterTexture.Width + RightTexture.Width;
                }
                else
                {
                    return 32;
                }
            }
        }

        public float EffectiveHeight
        {
            get
            {
                // See comment in Width
                if (Height != 0)
                {
                    return Height;
                }
                else if (TopTexture != null && CenterTexture != null && BottomTexture != null)
                {
                    return TopTexture.Height + CenterTexture.Height + BottomTexture.Height;
                }
                else
                {
                    return 32;
                }
            }
        }

        float IPositionedSizedObject.Width
        {
            get
            {
                return EffectiveWidth;
            }
            set
            {
                Width = value;
            }
        }

        float IPositionedSizedObject.Height
        {
            get
            {
                return EffectiveHeight;
            }
            set
            {
                Height = value;
            }
        }

        public Texture2D TopLeftTexture 
        {
            get { return mTopLeftSprite.Texture; }
            set { mTopLeftSprite.Texture = value; }
        }
        public Texture2D TopTexture 
        {
            get { return mTopSprite.Texture; }
            set { mTopSprite.Texture = value; }
        }
        public Texture2D TopRightTexture 
        {
            get { return mTopRightSprite.Texture; }
            set { mTopRightSprite.Texture = value; }
        }
        public Texture2D RightTexture 
        {
            get { return mRightSprite.Texture; }
            set { mRightSprite.Texture = value; }
        }
        public Texture2D BottomRightTexture 
        {
            get { return mBottomRightSprite.Texture; }
            set { mBottomRightSprite.Texture = value; }
        }
        public Texture2D BottomTexture 
        {
            get { return mBottomSprite.Texture; }
            set { mBottomSprite.Texture = value; }
        }
        public Texture2D BottomLeftTexture
        {
            get { return mBottomLeftSprite.Texture; }
            set { mBottomLeftSprite.Texture = value; }
        }
        public Texture2D LeftTexture
        {
            get { return mLeftSprite.Texture; }
            set { mLeftSprite.Texture = value; }
        }
        public Texture2D CenterTexture 
        {
            get { return mCenterSprite.Texture; }
            set { mCenterSprite.Texture = value; }
        }

        public BlendState BlendState
        {
            get;
            set;
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

        public IPositionedSizedObject Parent
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


        public ICollection<IPositionedSizedObject> Children
        {
            get { return mChildren; }
        }

        #endregion


        void IRenderable.Render(SpriteBatch spriteBatch, SystemManagers managers)
        {
            if (this.AbsoluteVisible)
            {
                float y = this.Y;

                mTopLeftSprite.X = this.X;
                mTopLeftSprite.Y = y;

                mTopSprite.X = mTopLeftSprite.X + mTopLeftSprite.EffectiveWidth;
                mTopSprite.Y = y;

                mTopRightSprite.X = mTopSprite.X + mTopSprite.EffectiveWidth;
                mTopRightSprite.Y = y;

                y = mTopLeftSprite.Y + mTopLeftSprite.EffectiveHeight;

                mLeftSprite.X = this.X;
                mLeftSprite.Y = y;

                mCenterSprite.X = mLeftSprite.X + mLeftSprite.EffectiveWidth;
                mCenterSprite.Y = y;

                mRightSprite.X = mCenterSprite.X + mCenterSprite.EffectiveWidth;
                mRightSprite.Y = y;

                y = mLeftSprite.Y + mLeftSprite.EffectiveHeight;

                mBottomLeftSprite.X = this.X;
                mBottomLeftSprite.Y = y;

                mBottomSprite.X = mBottomLeftSprite.X + mBottomLeftSprite.EffectiveWidth;
                mBottomSprite.Y = y;

                mBottomRightSprite.X = mBottomSprite.X + mBottomSprite.EffectiveWidth;
                mBottomRightSprite.Y = y;


                ((IRenderable)mTopLeftSprite).Render(spriteBatch, managers);
                ((IRenderable)mTopSprite).Render(spriteBatch, managers);
                ((IRenderable)mTopRightSprite).Render(spriteBatch, managers);
                ((IRenderable)mLeftSprite).Render(spriteBatch, managers);
                ((IRenderable)mCenterSprite).Render(spriteBatch, managers);
                ((IRenderable)mRightSprite).Render(spriteBatch, managers);
                ((IRenderable)mBottomLeftSprite).Render(spriteBatch, managers);
                ((IRenderable)mBottomSprite).Render(spriteBatch, managers);
                ((IRenderable)mBottomRightSprite).Render(spriteBatch, managers);

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
                return ((IPositionedSizedObject)this).Parent as IVisible;
            }
        }

        #endregion


        public NineSlice()
        {
            Visible = true;
        }
    }
}
