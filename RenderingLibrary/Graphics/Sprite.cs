
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

        // todo:  Anim sizing

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
                return Width;
            }
        }

        public float EffectiveHeight
        {
            get
            {
                // See comment in Width
                return Height;
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

        public float Rotation { get; set; }

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

        bool IRenderable.Wrap
        {
            get
            {
                return this.Wrap && mTexture != null &&
                    Math.MathFunctions.IsPowerOfTwo(mTexture.Width) &&
                    Math.MathFunctions.IsPowerOfTwo(mTexture.Height);

            }

        }

        public bool Wrap
        {
            get;
            set;
        }

        public int Alpha
        {
            get
            {
                return Color.A;
            }
            set
            {
                Color.A = (byte)value;
            }
        }

        public int Red
        {
            get
            {
                return Color.R;
            }
            set
            {
                Color.R = (byte)value;
            }
        }

        public int Green
        {
            get
            {
                return Color.G;
            }
            set
            {
                Color.G = (byte)value;
            }
        }

        public int Blue
        {
            get
            {
                return Color.B;
            }
            set
            {
                Color.B = (byte)value;
            }
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

                // Right now we'll just default this to resize the Sprite, but eventually we may want more control over it
                if (SourceRectangle.HasValue)
                {
                    this.Width = SourceRectangle.Value.Width;
                    this.Height = SourceRectangle.Value.Height;
                }
            }
        }

        void IRenderable.Render(SpriteBatch spriteBatch, SystemManagers managers)
        {
            if (this.AbsoluteVisible && Width > 0 && Height > 0)
            {
                bool shouldTileByMultipleCalls = this.Wrap && (this as IRenderable).Wrap == false;
                if (shouldTileByMultipleCalls && this.Texture != null)
                {
                    RenderTiledSprite(spriteBatch, managers);
                }
                else
                {
                    Render(managers, spriteBatch, this, Texture, Color, SourceRectangle, FlipHorizontal, FlipVertical, Rotation);
                }
            }
        }

        private void RenderTiledSprite(SpriteBatch spriteBatch, SystemManagers managers)
        {
            
            float texelsWide = this.Texture.Width;
            if (SourceRectangle.HasValue)
            {
                texelsWide = SourceRectangle.Value.Width;
            }

            float texelsTall = this.Texture.Height;
            if (SourceRectangle.HasValue)
            {
                texelsTall = SourceRectangle.Value.Height;
            }

            float xRepetitions = texelsWide / (float)Texture.Width;
            float yRepetitions = texelsTall / (float)Texture.Height;

            if (xRepetitions > 0 && yRepetitions > 0)
            {
                float eachWidth = this.EffectiveWidth / xRepetitions;
                float eachHeight = this.EffectiveHeight / yRepetitions;

                float oldEffectiveWidth = this.EffectiveWidth;
                float oldEffectiveHeight = this.EffectiveHeight;

                float oldWidth = this.Width;
                float oldHeight = this.Height;

                float oldX = this.X;
                float oldY = this.Y;

                var oldSource = this.SourceRectangle.Value;


                float texelsPerWorldUnitX = (float)Texture.Width / eachWidth;
                float texelsPerWorldUnitY = (float)Texture.Height / eachHeight;

                int oldSourceY = oldSource.Y;

                if (oldSourceY < 0)
                {
                    int amountToAdd = 1 - (oldSourceY / Texture.Height);

                    oldSourceY += amountToAdd * Texture.Height;
                }

                if (oldSourceY > 0)
                {
                    int amountToAdd = System.Math.Abs(oldSourceY) / Texture.Height;
                    oldSourceY -= amountToAdd * Texture.Height;
                }
                float startingY = -oldSourceY * (1 / texelsPerWorldUnitY);
                while (startingY < (int)oldEffectiveHeight)
                {
                    float worldUnitsChoppedOffTop = System.Math.Max(0, -startingY);
                    float worldUnitsChoppedOffBottom = System.Math.Max(0, startingY + eachHeight - (int)oldEffectiveHeight);

                    int texelsChoppedOffTop = 0;
                    if (worldUnitsChoppedOffTop > 0)
                    {
                        texelsChoppedOffTop = oldSourceY;
                    }

                    int texelsChoppedOffBottom =
                        RenderingLibrary.Math.MathFunctions.RoundToInt(worldUnitsChoppedOffBottom * texelsPerWorldUnitY);

                    this.Y = oldY + startingY + worldUnitsChoppedOffTop;

                    int sourceHeight = (int)(Texture.Height - texelsChoppedOffTop - texelsChoppedOffBottom);

                    if(sourceHeight == 0)
                    {
                        break;
                    }

                    this.Height = sourceHeight * 1 / texelsPerWorldUnitY;

                    int oldSourceX = oldSource.X;

                    if (oldSourceX < 0)
                    {
                        int amountToAdd = 1 - (oldSourceX / Texture.Width);

                        oldSourceX += amountToAdd * Texture.Width;
                    }

                    if (oldSourceX > 0)
                    {
                        int amountToAdd = System.Math.Abs(oldSourceX) / Texture.Width;

                        oldSourceX -= amountToAdd * Texture.Width;
                    }

                    float startingX = -oldSourceX * (1 / texelsPerWorldUnitX);



                    while (startingX < (int)oldEffectiveWidth)
                    {
                        float worldUnitsChoppedOffLeft = System.Math.Max(0, -startingX);
                        float worldUnitsChoppedOffRight = System.Math.Max(0, startingX + eachWidth - (int)oldEffectiveWidth);

                        int texelsChoppedOffLeft = 0;
                        if (worldUnitsChoppedOffLeft > 0)
                        {
                            // Let's use the hard number to not have any floating point issues:
                            //texelsChoppedOffLeft = worldUnitsChoppedOffLeft * texelsPerWorldUnit;
                            texelsChoppedOffLeft = oldSourceX;
                        }
                        int texelsChoppedOffRight = 
                            RenderingLibrary.Math.MathFunctions.RoundToInt(worldUnitsChoppedOffRight * texelsPerWorldUnitX);

                        this.X = oldX + startingX + worldUnitsChoppedOffLeft;

                        int sourceWidth = (int)(Texture.Width - texelsChoppedOffLeft - texelsChoppedOffRight);

                        if (sourceWidth == 0)
                        {
                            break;
                        }

                        this.Width = sourceWidth * 1 / texelsPerWorldUnitX;



                        this.SourceRectangle = new Rectangle(
                            RenderingLibrary.Math.MathFunctions.RoundToInt(texelsChoppedOffLeft), 
                            RenderingLibrary.Math.MathFunctions.RoundToInt(texelsChoppedOffTop), 
                            sourceWidth,
                            sourceHeight);
                        //this.Width = thisWidth ;
                        Render(managers, spriteBatch, this, Texture, Color, SourceRectangle, FlipHorizontal, FlipVertical);
                        startingX = System.Math.Max(0, startingX);
                        startingX += this.Width;

                    }
                    startingY = System.Math.Max(0, startingY);
                    startingY += this.Height;
                }

                this.Width = oldWidth;
                this.Height = oldHeight;

                this.X = oldX;
                this.Y = oldY;

                this.SourceRectangle = oldSource;
            }
        }

        public static void Render(SystemManagers managers, SpriteBatch spriteBatch, IPositionedSizedObject ipso, Texture2D texture)
        {
            Color color = new Color(1.0f, 1.0f, 1.0f, 1.0f); // White

            Render(managers, spriteBatch, ipso, texture, color);
        }


        public static void Render(SystemManagers managers, SpriteBatch spriteBatch,
            IPositionedSizedObject ipso, Texture2D texture, Color color,
            Rectangle? sourceRectangle = null,
            bool flipHorizontal = false,
            bool flipVertical = false,
            float rotationInDegrees = 0,
            bool treat0AsFullDimensions = false
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

            if (textureToUse == null)
            {
                textureToUse = LoaderManager.Self.InvalidTexture;

                if (textureToUse == null)
                {
                    return;
                }
            }

            SpriteEffects effects = SpriteEffects.None;
            if (flipHorizontal)
            {
                effects |= SpriteEffects.FlipHorizontally;
            }
            if (flipVertical)
            {
                effects |= SpriteEffects.FlipVertically;
            }

            if ((ipso.Width > 0 && ipso.Height > 0) || treat0AsFullDimensions == false)
            {
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

                if (textureToUse != null && textureToUse.IsDisposed)
                {
                    throw new ObjectDisposedException("Texture is disposed.  Texture name: " + textureToUse.Name + ", sprite scale: " + scale);
                }

                spriteBatch.Draw(textureToUse,
                    new Vector2(ipso.GetAbsoluteX(), ipso.GetAbsoluteY()),
                    sourceRectangle,
                    color,
                    Microsoft.Xna.Framework.MathHelper.TwoPi * -rotationInDegrees/360.0f,
                    Vector2.Zero,
                    scale,
                    effects,
                    0);
            }
            else
            {
                int width = textureToUse.Width;
                int height = textureToUse.Height;

                if (sourceRectangle != null && sourceRectangle.HasValue)
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
                    rotationInDegrees/360.0f,
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

        void IPositionedSizedObject.SetParentDirect(IPositionedSizedObject parent)
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
                return ((IPositionedSizedObject)this).Parent as IVisible;
            }
        }

        #endregion
    }
}
