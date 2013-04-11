
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using RenderingLibrary.Content;
using System.Collections.ObjectModel;

namespace RenderingLibrary.Graphics
{
    public class Sprite : IPositionedSizedObject, IRenderable, IVisible
    {
        #region Fields

        Vector2 Position;
        IPositionedSizedObject mParent;

        List<IPositionedSizedObject> mChildren;

        public Color Color = Color.White;

        public Rectangle? SourceRectangle;

        Texture2D mTexture;

        #endregion

        #region Properties

        public string Name
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

        public float EffectiveWidth
        {
            get
            {
                if (Width != 0 && Height != 0)
                {
                    return Width;
                }
                else if (Texture != null)
                {
                    if (this.SourceRectangle != null && SourceRectangle.HasValue)
                    {
                        return SourceRectangle.Value.Width;
                    }
                    else
                    {
                        return Texture.Width;
                    }
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
                if (Width != 0 && Height != 0)
                {
                    return Height;
                }
                else if (Texture != null)
                {
                    if (this.SourceRectangle != null && SourceRectangle.HasValue)
                    {
                        return SourceRectangle.Value.Height;
                    }
                    else
                    {
                        return Texture.Height;
                    }
                }
                else
                {
                    return 32;
                }
            }
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

        public Texture2D Texture
        {
            get { return mTexture; }
            set
            {
                mTexture = value;
            }
        }

        public IAnimation Animation
        {
            get;
            set;
        }

        public bool Animate
        {
            get;
            set;
        }

        public ICollection<IPositionedSizedObject> Children
        {
            get { return mChildren; }
        }

        public object Tag { get; set; }

        public BlendState BlendState
        {
            get;
            set;
        }

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

        #region Methods

        public Sprite(Texture2D texture)
        {
            this.Visible = true;
            BlendState = BlendState.NonPremultiplied;
            mChildren = new List<IPositionedSizedObject>();

            Texture = texture;
        }

        public void AnimationActivity(double currentTime)
        {
            if (Animate)
            {
                Animation.AnimationActivity(currentTime);

                SourceRectangle = Animation.SourceRectangle;
                Texture = Animation.CurrentTexture;
                FlipHorizontal = Animation.FlipHorizontal;
                FlipVertical = Animation.FlipVertical;
            }
        }

        void IRenderable.Render(SpriteBatch spriteBatch, SystemManagers managers)
        {
            if (this.AbsoluteVisible)
            {
                Render(managers, spriteBatch, this, Texture, Color, SourceRectangle, FlipHorizontal, FlipVertical);
            }
        }

        public static void Render(SystemManagers managers, SpriteBatch spriteBatch, IPositionedSizedObject ipso, Texture2D texture )
        {
            Color color = new Color(1.0f, 1.0f, 1.0f, 1.0f); // White

            Render(managers, spriteBatch, ipso, texture, color);
        }
        
        
        public static void Render(SystemManagers managers, SpriteBatch spriteBatch, 
            IPositionedSizedObject ipso, Texture2D texture, Color color,
            Rectangle? sourceRectangle = null,
            bool flipHorizontal = false,
            bool flipVertical = false
            )
        {
            Renderer renderer = null;
            if (managers == null)
            {
                renderer = Renderer.Self;
            }
            else
            {
                renderer = managers.Renderer;
            }

            Texture2D textureToUse = texture;
            
            if(textureToUse == null)
            {
                textureToUse = LoaderManager.Self.InvalidTexture;
            }

            SpriteEffects effects = SpriteEffects.None;
            if(flipHorizontal)
            {
                effects |= SpriteEffects.FlipHorizontally;
            }
            if(flipVertical)
            {
                effects |= SpriteEffects.FlipVertically;
            }

            if (ipso.Width > 0 && ipso.Height > 0)
            {
                //Rectangle destinationRectangle = new Rectangle(
                //    (int)(ipso.GetAbsoluteX()), 
                //    (int)(ipso.GetAbsoluteY()),
                //    (int)ipso.Width, 
                //    (int)ipso.Height);

                Vector2 scale = Vector2.One;

                if (textureToUse == null)
                {
                    scale = new Vector2(ipso.Width, ipso.Height);
                }
                else
                {
                    float ratioWidth = 1;
                    float ratioHeight = 1;
                    if (sourceRectangle.HasValue)
                    {
                        ratioWidth = sourceRectangle.Value.Width / (float)textureToUse.Width;
                        ratioHeight = sourceRectangle.Value.Height / (float)textureToUse.Height;
                    }

                    scale = new Vector2(ipso.Width / (ratioWidth * textureToUse.Width), 
                        ipso.Height / (ratioHeight * textureToUse.Height));
                }

                spriteBatch.Draw(textureToUse, 
                    new Vector2(ipso.GetAbsoluteX(), ipso.GetAbsoluteY()),
                    sourceRectangle,
                    color,
                    0,
                    Vector2.Zero,
                    scale,
                    effects,
                    0);
            }
            else
            {
                int width = textureToUse.Width;
                int height = textureToUse.Height;

                if (sourceRectangle != null && sourceRectangle.HasValue != null)
                {
                    width = sourceRectangle.Value.Width;
                    height = sourceRectangle.Value.Height;
                }

                Rectangle destinationRectangle = new Rectangle(
                    (int)(ipso.GetAbsoluteX()),
                    (int)(ipso.GetAbsoluteY()),
                    width,
                    height);


                spriteBatch.Draw(textureToUse,
                    destinationRectangle, 
                    sourceRectangle, 
                    color,
                    0,
                    Vector2.Zero,
                    effects,
                    0
                    );
            }
        }

        public override string ToString()
        {
            return Name;
        }

        #endregion

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
    }
}
