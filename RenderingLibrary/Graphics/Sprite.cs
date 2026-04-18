
using System;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Content;
using System.Collections.ObjectModel;
using ToolsUtilitiesStandard.Helpers;
using BlendState = Gum.BlendState;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Vector2 = System.Numerics.Vector2;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using Gum.Graphics.Animation;
using RenderingLibrary.Graphics.Animation;
using RenderingLibrary.Math;

namespace RenderingLibrary.Graphics;


public class Sprite : SpriteBatchRenderableBase, 
    IRenderableIpso, 
    IVisible, 
    IAspectRatio, 
    ITextureCoordinate, 
    IAnimatable, 
    ICloneable,
    IRenderTargetTextureReferencer
{
    #region Fields / Properties


    static Texture2D? mInvalidTexture;
    public static Texture2D? InvalidTexture
    {
        get { return mInvalidTexture; }
        set {  mInvalidTexture = value; }
    }

    public SpriteAnimationLogic AnimationLogic { get; } = new SpriteAnimationLogic();

    public int CurrentFrameIndex
    {
        get => AnimationLogic.CurrentFrameIndex;
        set => AnimationLogic.CurrentFrameIndex = value;
    }

    public float AnimationSpeed
    {
        get => AnimationLogic.AnimationSpeed;
        set => AnimationLogic.AnimationSpeed = value;
    }

    public double TimeIntoAnimation
    {
        get => AnimationLogic.TimeIntoAnimation;
        set => AnimationLogic.TimeIntoAnimation = value;
    }

    public AnimationChainList? AnimationChains
    {
        get => AnimationLogic.AnimationChains;
        set => AnimationLogic.AnimationChains = value;
    }

    public AnimationChain? CurrentChain => AnimationLogic.CurrentChain;

    public string? CurrentChainName
    {
        get => AnimationLogic.CurrentChainName;
        set => AnimationLogic.CurrentChainName = value;
    }

    Vector2 Position;

    public Color Color = Color.White;

    public int Alpha
    {
        get
        {
            return Color.A;
        }
        set
        {
            Color = Color.WithAlpha((byte)value);
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
            Color = Color.WithRed((byte)value);
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
            Color = Color.WithGreen((byte)value);
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
            Color = Color.WithBlue((byte)value);
        }
    }

    public Rectangle? SourceRectangle;
    Rectangle? ITextureCoordinate.SourceRectangle
    {
        get
        {
            return SourceRectangle;
        }
        set
        {
            SourceRectangle = value;
        }
    }

    Texture2D mTexture;



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

    public float Width
    {
        get;
        set;
    }

    float _height;
    public float Height
    {
        get => _height;
        set
        {
#if FULL_DIAGNOSTICS
            if (float.IsNaN(value))
            {
                throw new InvalidOperationException("Cannot assign a Sprite's Height to NaN");
            }
#endif
            _height = value;
        }
    }

    bool IRenderableIpso.ClipsChildren
    {
        get
        {
            return false;
        }
    }

    IRenderableIpso mParent;
    public IRenderableIpso? Parent
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

    bool IRenderableIpso.IsRenderTarget => false;
    public Texture2D? Texture
    {
        get { return mTexture; }
        set
        {
            mTexture = value;
        }
    }

    public IRenderableIpso? RenderTargetTextureSource { get; set; }

    public float? TextureWidth => RenderTargetTextureSource?.Width ?? Texture?.Width;
    public float? TextureHeight => RenderTargetTextureSource?.Height ?? Texture?.Height;

    // October 30, 2024
    // Vic asks - is this even used?
    // @VIC Read your comment above sir
    public IAnimation Animation
    {
        get;
        set;
    }

    public float Rotation { get; set; }

    public bool Animate
    {
        get => AnimationLogic.Animate;
        set => AnimationLogic.Animate = value;
    }

    public bool IsAnimationChainLooping
    {
        get => AnimationLogic.IsAnimationChainLooping;
        set => AnimationLogic.IsAnimationChainLooping = value;
    }

    public event Action AnimationChainCycled
    {
        add => AnimationLogic.AnimationChainCycled += value;
        remove => AnimationLogic.AnimationChainCycled -= value;
    }

    ObservableCollectionNoReset<IRenderableIpso> mChildren;
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


    /// <summary>
    /// Returns the effective source rectangle, which may be the same as the SourceRectangle unless an AtlasedTexture is used.
    /// </summary>
    public Rectangle? EffectiveRectangle
    {
        get
        {
            Rectangle? sourceRectangle = SourceRectangle;

            return sourceRectangle;
        }
    }

    float IAspectRatio.AspectRatio
    {
        get
        {
            if(RenderTargetTextureSource != null)
            {
                return (RenderTargetTextureSource.Width / (float)RenderTargetTextureSource.Height);
            }
            else if (Texture != null)
            {
                return (Texture.Width / (float)Texture.Height);
            }
            else
            {
                return 1;
            }
        }
    }

    #endregion

    public Sprite(Texture2D? texture)
    {
        this.Visible = true;
        // why do we set this? It should be null so that
        // the sprite will render using the default blendop, which may differ
        // depending on whether the game uses premult or standard
        //BlendState = BlendState.NonPremultiplied;
        mChildren = new ();

        Texture = texture;
        AnimationLogic.ApplyFrame = ApplyAnimationFrame;
    }

    void ApplyAnimationFrame(AnimationFrame frame)
    {
        Texture = frame.Texture;

        if (frame.Texture != null)
        {
            var left = MathFunctions.RoundToInt(frame.LeftCoordinate * frame.Texture.Width);
            var width = MathFunctions.RoundToInt(frame.RightCoordinate * frame.Texture.Width) - left;
            var top = MathFunctions.RoundToInt(frame.TopCoordinate * frame.Texture.Height);
            var height = MathFunctions.RoundToInt(frame.BottomCoordinate * frame.Texture.Height) - top;
            SourceRectangle = new Rectangle(left, top, width, height);
        }

        FlipHorizontal = frame.FlipHorizontal;
        FlipVertical = frame.FlipVertical;
    }

    public override void Render(ISystemManagers managers)
    {
        // See NineSlice for explanation of this Visible check
        if (Width > 0 && Height > 0)
        {
            var systemManagers = managers as SystemManagers;
            var renderer = systemManagers.Renderer;
            bool shouldTileByMultipleCalls = this.Wrap && (this as IRenderable).Wrap == false;
            if (shouldTileByMultipleCalls && (this.Texture != null))
            {
                RenderTiledSprite(renderer.SpriteRenderer, systemManagers);
            }
            else
            {
                Rectangle? sourceRectangle = EffectiveRectangle;
                Texture2D texture = Texture;

                var oldX = this.X;
                var oldY = this.Y;

                var absoluteRotationDegrees = this.GetAbsoluteRotation();

                if (this.CurrentFrameIndex < CurrentChain?.Count)
                {

                    var absoluteRotationRadians = MathHelper.ToRadians(absoluteRotationDegrees);

                    var offsetVector = new System.Numerics.Vector2(
                        CurrentChain[this.CurrentFrameIndex].RelativeX,
                        CurrentChain[this.CurrentFrameIndex].RelativeY);

                    if(absoluteRotationDegrees != 0 && (offsetVector.X != 0 || offsetVector.Y != 0))
                    {
                        var length = offsetVector.Length();
                        var offsetAngleRadians = System.Math.Atan2(offsetVector.Y, offsetVector.X);
                        offsetAngleRadians += absoluteRotationRadians;

                        offsetVector = new Vector2(
                            (float)(length * System.Math.Cos(offsetAngleRadians)), 
                            (float)(length * System.Math.Sin(offsetAngleRadians)));
                    }

                    this.X += offsetVector.X;
                    this.Y -= offsetVector.Y;
                }

                Render(systemManagers, renderer.SpriteRenderer, this, texture, Color, sourceRectangle, FlipVertical, absoluteRotationDegrees);

                this.X = oldX;
                this.Y = oldY;
            }
        }
    }

    private void RenderTiledSprite(SpriteRenderer spriteRenderer, SystemManagers managers)
    {
        if(SourceRectangle == null || SourceRectangle.Value.Width <= 0 || SourceRectangle.Value.Height <= 0 || Texture == null)
        {
            return;
        }

        // We're going to change the width, height, X, and Y of "this" to make rendering code work
        // by simply passing in the object. At the end of the drawing, we'll revert the values back
        // to what they were before rendering started.
        float oldWidth = this.Width;
        float oldHeight = this.Height;

        float textureWidthScale = this.Width / SourceRectangle.Value.Width;
        float textureHeightScale = this.Height / SourceRectangle.Value.Height;

        float oldX = this.X;
        float oldY = this.Y;

        var oldSource = this.SourceRectangle.Value;

        var matrix = this.GetRotationMatrix();


        int startX = oldSource.X;
        int startY = oldSource.Y;
        int endX = oldSource.Right;
        int endY = oldSource.Bottom;

        float offsetXFromTopLeft = 0;
        float offsetYFromTopLeft = 0;

        for(int y = startY; y < endY; )
        {
            int texTop = ((y % Texture.Height) + Texture.Height) % Texture.Height;
            int texHeight = System.Math.Min(Texture.Height - texTop, endY - y);

            for(int x = startX; x < endX; )
            {
                int texLeft = ((x % Texture.Width) + Texture.Width) % Texture.Width;
                int texWidth = System.Math.Min(Texture.Width - texLeft, endX - x);

                System.Numerics.Vector3 position;
                position.X = oldX;
                position.Y = oldY;
                position.Z = 0;
                position += matrix.Right() * offsetXFromTopLeft;
                position += matrix.Up() * offsetYFromTopLeft;

                this.X = position.X;
                this.Y = position.Y;

                this.SourceRectangle = new Rectangle(texLeft, texTop, texWidth, texHeight);

                this.Width = texWidth * textureWidthScale;
                this.Height = texHeight * textureHeightScale;

                Render(managers, spriteRenderer, this, Texture, Color, SourceRectangle, FlipVertical, rotationInDegrees: Rotation);

                offsetXFromTopLeft += texWidth * textureWidthScale;
                x += texWidth;
            }

            offsetYFromTopLeft += texHeight * textureHeightScale;
            y += texHeight;
            offsetXFromTopLeft = 0;
        }

        this.Width = oldWidth;
        this.Height = oldHeight;

        this.X = oldX;
        this.Y = oldY;

        this.SourceRectangle = oldSource;

        return;

        //float texelsWide = 0;
        //float texelsTall = 0;

        //int fullTexelsWide = 0;
        //int fullTexelsTall = 0;

        //fullTexelsWide = this.Texture.Width;
        //fullTexelsTall = this.Texture.Height;

        //texelsWide = fullTexelsWide;
        //if (SourceRectangle.HasValue)
        //{
        //    texelsWide = SourceRectangle.Value.Width;
        //}
        //texelsTall = fullTexelsTall;
        //if (SourceRectangle.HasValue)
        //{
        //    texelsTall = SourceRectangle.Value.Height;
        //}


        //float xRepetitions = texelsWide / (float)fullTexelsWide;
        //float yRepetitions = texelsTall / (float)fullTexelsTall;


        //if (xRepetitions > 0 && yRepetitions > 0)
        //{
        //    float eachWidth = this.Width / xRepetitions;
        //    float eachHeight = this.Height / yRepetitions;



        //    float texelsPerWorldUnitX = (float)fullTexelsWide / eachWidth;
        //    float texelsPerWorldUnitY = (float)fullTexelsTall / eachHeight;

        //    int oldSourceY = oldSource.Y;

        //    if (oldSourceY < 0)
        //    {
        //        int amountToAdd = 1 - (oldSourceY / fullTexelsTall);

        //        oldSourceY += amountToAdd * Texture.Height;
        //    }

        //    if (oldSourceY > 0)
        //    {
        //        int amountToAdd = System.Math.Abs(oldSourceY) / fullTexelsTall;
        //        oldSourceY -= amountToAdd * Texture.Height;
        //    }
        //    float currentY = -oldSourceY * (1 / texelsPerWorldUnitY);

        //    var matrix = this.GetRotationMatrix();

        //    for (int y = 0; y < yRepetitions; y++)
        //    {
        //        float worldUnitsChoppedOffTop = System.Math.Max(0, oldSourceY * (1 / texelsPerWorldUnitY));
        //        //float worldUnitsChoppedOffBottom = System.Math.Max(0, currentY + eachHeight - (int)oldEffectiveHeight);

        //        float worldUnitsChoppedOffBottom = 0;

        //        float extraY = yRepetitions - y;
        //        if (extraY < 1)
        //        {
        //            worldUnitsChoppedOffBottom = System.Math.Max(0, (1 - extraY) * eachHeight);
        //        }



        //        int texelsChoppedOffTop = 0;
        //        if (worldUnitsChoppedOffTop > 0)
        //        {
        //            texelsChoppedOffTop = oldSourceY;
        //        }

        //        int texelsChoppedOffBottom =
        //            RenderingLibrary.Math.MathFunctions.RoundToInt(worldUnitsChoppedOffBottom * texelsPerWorldUnitY);

        //        int sourceHeight = (int)(fullTexelsTall - texelsChoppedOffTop - texelsChoppedOffBottom);

        //        if (sourceHeight == 0)
        //        {
        //            break;
        //        }

        //        this.Height = sourceHeight * 1 / texelsPerWorldUnitY;

        //        int oldSourceX = oldSource.X;

        //        if (oldSourceX < 0)
        //        {
        //            int amountToAdd = 1 - (oldSourceX / Texture.Width);

        //            oldSourceX += amountToAdd * fullTexelsWide;
        //        }

        //        if (oldSourceX > 0)
        //        {
        //            int amountToAdd = System.Math.Abs(oldSourceX) / Texture.Width;

        //            oldSourceX -= amountToAdd * fullTexelsWide;
        //        }

        //        float currentX = -oldSourceX * (1 / texelsPerWorldUnitX) + y * eachHeight * matrix.Up().X;
        //        currentY = y * eachHeight * matrix.Up().Y;

        //        for (int x = 0; x < xRepetitions; x++)
        //        {
        //            float worldUnitsChoppedOffLeft = System.Math.Max(0, oldSourceX * (1 / texelsPerWorldUnitX));
        //            float worldUnitsChoppedOffRight = 0;

        //            float extra = xRepetitions - x;
        //            if (extra < 1)
        //            {
        //                worldUnitsChoppedOffRight = System.Math.Max(0, (1 - extra) * eachWidth);
        //            }

        //            int texelsChoppedOffLeft = 0;
        //            if (worldUnitsChoppedOffLeft > 0)
        //            {
        //                // Let's use the hard number to not have any floating point issues:
        //                //texelsChoppedOffLeft = worldUnitsChoppedOffLeft * texelsPerWorldUnit;
        //                texelsChoppedOffLeft = oldSourceX;
        //            }
        //            int texelsChoppedOffRight =
        //                RenderingLibrary.Math.MathFunctions.RoundToInt(worldUnitsChoppedOffRight * texelsPerWorldUnitX);

        //            this.X = oldX + currentX + worldUnitsChoppedOffLeft;
        //            this.Y = oldY + currentY + worldUnitsChoppedOffTop;

        //            int sourceWidth = (int)(fullTexelsWide - texelsChoppedOffLeft - texelsChoppedOffRight);

        //            if (sourceWidth == 0)
        //            {
        //                break;
        //            }

        //            this.Width = sourceWidth * 1 / texelsPerWorldUnitX;

        //            this.SourceRectangle = new Rectangle(
        //                texelsChoppedOffLeft,
        //                texelsChoppedOffTop,
        //                sourceWidth,
        //                sourceHeight);

        //            Render(managers, spriteRenderer, this, Texture, Color, SourceRectangle, FlipVertical, rotationInDegrees: Rotation);

        //            currentX = System.Math.Max(0, currentX);
        //            currentX += this.Width * matrix.Right().X;
        //            currentY += this.Width * matrix.Right().Y;

        //        }
        //    }

        //    this.Width = oldWidth;
        //    this.Height = oldHeight;

        //    this.X = oldX;
        //    this.Y = oldY;

        //    this.SourceRectangle = oldSource;
        //}
    }



    public static void Render(SystemManagers managers, SpriteRenderer spriteRenderer, IRenderableIpso ipso, Texture2D texture)
    {
        Color color = Color.White;

        Render(managers, spriteRenderer, ipso, texture, color);
    }


    public static void Render(SystemManagers managers, SpriteRenderer spriteRenderer,
        IRenderableIpso ipso, Texture2D texture, Color color,
        Rectangle? sourceRectangle = null,
        bool flipVertical = false,
        float rotationInDegrees = 0,
        bool treat0AsFullDimensions = false,
        // In the case of Text objects, we send in a line rectangle, but we want the Text object to be the owner of any resulting render states
        object objectCausingRendering = null
        )
    {
        if (objectCausingRendering == null)
        {
            objectCausingRendering = ipso;
        }

        Renderer renderer = managers.Renderer ?? Renderer.Self;

        Texture2D? textureToUse = texture ?? InvalidTexture;

        if (textureToUse == null)
        {
            return;
        }

        SpriteEffects effects = SpriteEffects.None;

        var flipHorizontal = ipso.GetAbsoluteFlipHorizontal();
        var effectiveParentFlipHorizontal = ipso.Parent?.GetAbsoluteFlipHorizontal() ?? false;

        if (flipHorizontal)
        {
            effects |= SpriteEffects.FlipHorizontally;
        }

        var rotationInRadians = MathHelper.ToRadians(rotationInDegrees);


        float leftAbsolute = ipso.GetAbsoluteX();
        float topAbsolute = ipso.GetAbsoluteY();

        Vector2 origin = Vector2.Zero;

        //if(flipHorizontal)
        //{
        //    var offsetX = (float)System.Math.Cos(rotationInRadians);
        //    var offsetY = (float)System.Math.Sin(rotationInRadians);
        //    origin.X = 1;

            

        //}

        if (flipVertical)
        {
            effects |= SpriteEffects.FlipVertically;
        }

        var modifiedColor = color;

        // Custom effect already does premultiply alpha on the shader so we skip that in this case
        if (!Renderer.UseCustomEffectRendering && Renderer.NormalBlendState == BlendState.AlphaBlend)
        {
            // we are using premult textures, so we need to premult the color:
            var alphaRatio = color.A / 255.0f;

            modifiedColor = Color.FromArgb(modifiedColor.A,
                (byte)(color.R * alphaRatio),
                (byte)(color.G * alphaRatio),
                (byte)(color.B * alphaRatio));
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

                if(ratioWidth == 0)
                {
                    scale.X = 0;
                }
                if(ratioHeight == 0)
                {
                    scale.Y = 0;
                }
            }

#if FULL_DIAGNOSTICS
            if (float.IsPositiveInfinity( scale.X))
            {
                throw new Exception("scale.X is positive infinity, it shouldn't be!");
            }

            if (textureToUse != null && textureToUse.IsDisposed)
            {
                throw new ObjectDisposedException($"Texture is disposed.  Texture name: {textureToUse.Name}, sprite scale: {scale}, Sprite name: {ipso.Name}");
            }
#endif

            if (textureToUse != null)
            {
                spriteRenderer.Draw(textureToUse,
                    new Vector2(leftAbsolute, topAbsolute),
                    sourceRectangle,
                    modifiedColor,
                    -rotationInRadians,
                    origin,
                    scale,
                    effects,
                    0,
                    objectCausingRendering, renderer);
            }
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
                (int)(leftAbsolute),
                (int)(topAbsolute),
                width,
                height);


            spriteRenderer.Draw(textureToUse,
                destinationRectangle,
                sourceRectangle,
                modifiedColor,
                rotationInRadians,
                origin,
                effects,
                0,
                objectCausingRendering
                );
        }
    }



    public override string ToString()
    {
        return Name + " (Sprite)";
    }

    void IRenderableIpso.SetParentDirect(IRenderableIpso? parent)
    {
        mParent = parent;
    }

    void IRenderable.PreRender() { }


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

    public bool AnimateSelf(double secondDifference)
    {
        if (Visible == false) return false;
        return AnimationLogic.AnimateSelf(secondDifference);
    }

    public bool UpdateToCurrentAnimationFrame() => AnimationLogic.UpdateToCurrentAnimationFrame();

    public void RefreshCurrentChainToDesiredName() => AnimationLogic.RefreshCurrentChainToDesiredName();

    #region IVisible Implementation


    /// <inheritdoc/>
    public bool Visible
    {
        get;
        set;
    }

    /// <inheritdoc/>
    public bool AbsoluteVisible => ((IVisible)this).GetAbsoluteVisible();

    /// <inheritdoc/>
    IVisible? IVisible.Parent => ((IRenderableIpso)this).Parent as IVisible;

    #endregion


    public Sprite Clone()
    {
        var newInstance = (Sprite)this.MemberwiseClone();
        newInstance.mParent = null;
        newInstance.mChildren = new ();

        return newInstance;
    }

    object ICloneable.Clone()
    {
        return Clone();
    }
}



public interface IRenderTargetTextureReferencer
{
    IRenderableIpso? RenderTargetTextureSource { get; }

    Texture2D? Texture { get; set; }
}
