
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
    public class Sprite : IRenderableIpso, IVisible
    {
        #region Fields

        Vector2 Position;
        IRenderableIpso mParent;

        ObservableCollection<IRenderableIpso> mChildren;

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


        bool IRenderableIpso.ClipsChildren
        {
            get
            {
                return false;
            }
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

        public AtlasedTexture AtlasedTexture
        {
            get;
            set;
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

        public ObservableCollection<IRenderableIpso> Children
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

        public Rectangle? EffectiveRectangle
        {
            get
            {
                Rectangle? sourceRectangle = SourceRectangle;

                if (AtlasedTexture != null)
                {
                    sourceRectangle = AtlasedTexture.SourceRectangle;

                    // Consider this.SourceRectangle to support rendering parts of a texture from a texture atlas:
                    if (this.SourceRectangle != null)
                    {
                        var toModify = sourceRectangle.Value;
                        toModify.X += this.SourceRectangle.Value.Left;
                        toModify.Y += this.SourceRectangle.Value.Top;

                        // We won't support wrapping (yet)
                        toModify.Width = System.Math.Min(toModify.Width, this.SourceRectangle.Value.Width);
                        toModify.Height = System.Math.Min(toModify.Height, this.SourceRectangle.Value.Height);

                        sourceRectangle = toModify;

                    }
                }

                return sourceRectangle;
            }
        }

        #endregion

        #region Methods

        public Sprite(Texture2D texture)
        {
            this.Visible = true;
            BlendState = BlendState.NonPremultiplied;
            mChildren = new ObservableCollection<IRenderableIpso>();

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

        void IRenderable.Render(SpriteRenderer spriteRenderer, SystemManagers managers)
        {
            if (this.AbsoluteVisible && Width > 0 && Height > 0)
            {
                bool shouldTileByMultipleCalls = this.Wrap && (this as IRenderable).Wrap == false;
                if (shouldTileByMultipleCalls && (this.Texture != null || this.AtlasedTexture != null))
                {
                    RenderTiledSprite(spriteRenderer, managers);
                }
                else
                {
                    Rectangle? sourceRectangle = EffectiveRectangle;
                    Texture2D texture = Texture;
                    if (AtlasedTexture != null)
                    {
                        texture = AtlasedTexture.Texture;
                    }

                    Render(managers, spriteRenderer, this, texture, Color, sourceRectangle, FlipHorizontal, FlipVertical, Rotation);
                }
            }
        }

        private void RenderTiledSprite(SpriteRenderer spriteRenderer, SystemManagers managers)
        {
            float texelsWide = 0;
            float texelsTall = 0;

            int fullTexelsWide = 0;
            int fullTexelsTall = 0;

            if (this.AtlasedTexture != null)
            {
                fullTexelsWide = this.AtlasedTexture.SourceRectangle.Width;
                fullTexelsTall = this.AtlasedTexture.SourceRectangle.Height;
            }
            else
            {
                fullTexelsWide = this.Texture.Width;
                fullTexelsTall = this.Texture.Height;
            }

            texelsWide = fullTexelsWide;
            if (SourceRectangle.HasValue)
            {
                texelsWide = SourceRectangle.Value.Width;
            }
            texelsTall = fullTexelsTall;
            if (SourceRectangle.HasValue)
            {
                texelsTall = SourceRectangle.Value.Height;
            }


            float xRepetitions = texelsWide / (float)fullTexelsWide;
            float yRepetitions = texelsTall / (float)fullTexelsTall;


            if (xRepetitions > 0 && yRepetitions > 0)
            {
                float eachWidth = this.EffectiveWidth / xRepetitions;
                float eachHeight = this.EffectiveHeight / yRepetitions;

                float oldEffectiveWidth = this.EffectiveWidth;
                float oldEffectiveHeight = this.EffectiveHeight;

                // We're going to change the width, height, X, and Y of "this" to make rendering code work
                // by simply passing in the object. At the end of the drawing, we'll revert the values back
                // to what they were before rendering started.
                float oldWidth = this.Width;
                float oldHeight = this.Height;

                float oldX = this.X;
                float oldY = this.Y;

                var oldSource = this.SourceRectangle.Value;


                float texelsPerWorldUnitX = (float)fullTexelsWide / eachWidth;
                float texelsPerWorldUnitY = (float)fullTexelsTall / eachHeight;

                int oldSourceY = oldSource.Y;

                if (oldSourceY < 0)
                {
                    int amountToAdd = 1 - (oldSourceY / fullTexelsTall);

                    oldSourceY += amountToAdd * Texture.Height;
                }

                if (oldSourceY > 0)
                {
                    int amountToAdd = System.Math.Abs(oldSourceY) / fullTexelsTall;
                    oldSourceY -= amountToAdd * Texture.Height;
                }
                float currentY = -oldSourceY * (1 / texelsPerWorldUnitY);

                var matrix = this.GetRotationMatrix();

                for (int y = 0; y < yRepetitions; y++)
                {
                    float worldUnitsChoppedOffTop = System.Math.Max(0, oldSourceY * (1 / texelsPerWorldUnitY));
                    //float worldUnitsChoppedOffBottom = System.Math.Max(0, currentY + eachHeight - (int)oldEffectiveHeight);

                    float worldUnitsChoppedOffBottom = 0;

                    float extraY = yRepetitions - y;
                    if (extraY < 1)
                    {
                        worldUnitsChoppedOffBottom = System.Math.Max(0, (1 - extraY) * eachHeight);
                    }



                    int texelsChoppedOffTop = 0;
                    if (worldUnitsChoppedOffTop > 0)
                    {
                        texelsChoppedOffTop = oldSourceY;
                    }

                    int texelsChoppedOffBottom =
                        RenderingLibrary.Math.MathFunctions.RoundToInt(worldUnitsChoppedOffBottom * texelsPerWorldUnitY);

                    int sourceHeight = (int)(fullTexelsTall - texelsChoppedOffTop - texelsChoppedOffBottom);

                    if (sourceHeight == 0)
                    {
                        break;
                    }

                    this.Height = sourceHeight * 1 / texelsPerWorldUnitY;

                    int oldSourceX = oldSource.X;

                    if (oldSourceX < 0)
                    {
                        int amountToAdd = 1 - (oldSourceX / Texture.Width);

                        oldSourceX += amountToAdd * fullTexelsWide;
                    }

                    if (oldSourceX > 0)
                    {
                        int amountToAdd = System.Math.Abs(oldSourceX) / Texture.Width;

                        oldSourceX -= amountToAdd * fullTexelsWide;
                    }

                    float currentX = -oldSourceX * (1 / texelsPerWorldUnitX) + y * eachHeight * matrix.Up.X;
                    currentY = y * eachHeight * matrix.Up.Y;

                    for (int x = 0; x < xRepetitions; x++)
                    {
                        float worldUnitsChoppedOffLeft = System.Math.Max(0, oldSourceX * (1 / texelsPerWorldUnitX));
                        float worldUnitsChoppedOffRight = 0;

                        float extra = xRepetitions - x;
                        if (extra < 1)
                        {
                            worldUnitsChoppedOffRight = System.Math.Max(0, (1 - extra) * eachWidth);
                        }

                        int texelsChoppedOffLeft = 0;
                        if (worldUnitsChoppedOffLeft > 0)
                        {
                            // Let's use the hard number to not have any floating point issues:
                            //texelsChoppedOffLeft = worldUnitsChoppedOffLeft * texelsPerWorldUnit;
                            texelsChoppedOffLeft = oldSourceX;
                        }
                        int texelsChoppedOffRight =
                            RenderingLibrary.Math.MathFunctions.RoundToInt(worldUnitsChoppedOffRight * texelsPerWorldUnitX);

                        this.X = oldX + currentX + worldUnitsChoppedOffLeft;
                        this.Y = oldY + currentY + worldUnitsChoppedOffTop;

                        int sourceWidth = (int)(fullTexelsWide - texelsChoppedOffLeft - texelsChoppedOffRight);

                        if (sourceWidth == 0)
                        {
                            break;
                        }

                        this.Width = sourceWidth * 1 / texelsPerWorldUnitX;




                        if (AtlasedTexture != null)
                        {
                            var rectangle = new Rectangle(
                                AtlasedTexture.SourceRectangle.X + RenderingLibrary.Math.MathFunctions.RoundToInt(texelsChoppedOffLeft),
                                AtlasedTexture.SourceRectangle.Y + RenderingLibrary.Math.MathFunctions.RoundToInt(texelsChoppedOffTop),
                                sourceWidth,
                                sourceHeight);

                            Render(managers, spriteRenderer, this, AtlasedTexture.Texture, Color, rectangle, FlipHorizontal, FlipVertical, rotationInDegrees: Rotation);
                        }
                        else
                        {
                            this.SourceRectangle = new Rectangle(
                                RenderingLibrary.Math.MathFunctions.RoundToInt(texelsChoppedOffLeft),
                                RenderingLibrary.Math.MathFunctions.RoundToInt(texelsChoppedOffTop),
                                sourceWidth,
                                sourceHeight);

                            Render(managers, spriteRenderer, this, Texture, Color, SourceRectangle, FlipHorizontal, FlipVertical, rotationInDegrees: Rotation);
                        }
                        currentX = System.Math.Max(0, currentX);
                        currentX += this.Width * matrix.Right.X;
                        currentY += this.Width * matrix.Right.Y;

                    }
                }

                this.Width = oldWidth;
                this.Height = oldHeight;

                this.X = oldX;
                this.Y = oldY;

                this.SourceRectangle = oldSource;
            }
        }



        public static void Render(SystemManagers managers, SpriteRenderer spriteRenderer, IRenderableIpso ipso, Texture2D texture)
        {
            Color color = new Color(1.0f, 1.0f, 1.0f, 1.0f); // White

            Render(managers, spriteRenderer, ipso, texture, color);
        }


        public static void Render(SystemManagers managers, SpriteRenderer spriteRenderer,
            IRenderableIpso ipso, Texture2D texture, Color color,
            Rectangle? sourceRectangle = null,
            bool flipHorizontal = false,
            bool flipVertical = false,
            float rotationInDegrees = 0,
            bool treat0AsFullDimensions = false,
            // In the case of Text objects, we send in a line rectangle, but we want the Text object to be the owner of any resulting render states
            object objectCausingRenering = null
            )
        {
            if (objectCausingRenering == null)
            {
                objectCausingRenering = ipso;
            }

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

            var modifiedColor = color;

            if (Renderer.NormalBlendState == BlendState.AlphaBlend)
            {
                // we are using premult textures, so we need to premult the color:
                var alphaRatio = color.A / 255.0f;

                modifiedColor.R = (byte)(color.R * alphaRatio);
                modifiedColor.G = (byte)(color.G * alphaRatio);
                modifiedColor.B = (byte)(color.B * alphaRatio);
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
                    throw new ObjectDisposedException($"Texture is disposed.  Texture name: {textureToUse.Name}, sprite scale: {scale}, Sprite name: {ipso.Name}");
                }

                spriteRenderer.Draw(textureToUse,
                    new Vector2(ipso.GetAbsoluteX(), ipso.GetAbsoluteY()),
                    sourceRectangle,
                    modifiedColor,
                    Microsoft.Xna.Framework.MathHelper.TwoPi * -rotationInDegrees / 360.0f,
                    Vector2.Zero,
                    scale,
                    effects,
                    0,
                    objectCausingRenering);
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


                spriteRenderer.Draw(textureToUse,
                    destinationRectangle,
                    sourceRectangle,
                    modifiedColor,
                    rotationInDegrees / 360.0f,
                    Vector2.Zero,
                    effects,
                    0,
                    objectCausingRenering
                    );
            }
        }

        public override string ToString()
        {
            return Name + " (Sprite)";
        }

        #endregion

        void IRenderableIpso.SetParentDirect(IRenderableIpso parent)
        {
            mParent = parent;
        }

        void IRenderable.PreRender() { }

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
