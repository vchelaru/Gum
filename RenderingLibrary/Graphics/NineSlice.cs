using System;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using ToolsUtilities;
using RenderingLibrary.Content;
using System.Collections.ObjectModel;
using RenderingLibrary.Math;
using ToolsUtilitiesStandard.Helpers;
using BlendState = Gum.BlendState;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Rectangle = System.Drawing.Rectangle;
using Color = System.Drawing.Color;
using Matrix = System.Numerics.Matrix4x4;
using Gum;
using Gum.Graphics.Animation;
using System.Runtime.CompilerServices;

namespace RenderingLibrary.Graphics;

public class NineSlice : SpriteBatchRenderableBase, 
    IRenderableIpso, 
    IVisible, 
    IAspectRatio,
    ITextureCoordinate, 
    IAnimatable, ICloneable
{
    #region Fields


    ObservableCollectionNoReset<IRenderableIpso> mChildren = new ();

    //Sprites which make up NineSlice indexed by NineSliceSections enum
    private Sprite[] mSprites;


    int mCurrentChainIndex;
    protected int mCurrentFrameIndex;
    public int CurrentFrameIndex
    {
        get => mCurrentFrameIndex;
        set => mCurrentFrameIndex = value;
    }

    protected float mAnimationSpeed = 1;
    protected double mTimeIntoAnimation;

    public double TimeIntoAnimation
    {
        get => mTimeIntoAnimation;
        set => mTimeIntoAnimation = value;
    }

    AnimationChainList mAnimationChains;
    public AnimationChainList AnimationChains
    {
        get => mAnimationChains;
        set => mAnimationChains = value;
    }
    public AnimationChain CurrentChain
    {
        get
        {
            if (mCurrentChainIndex != -1 && mAnimationChains.Count > 0 && mCurrentChainIndex < mAnimationChains.Count)
            {
                return mAnimationChains[mCurrentChainIndex];
            }
            else
                return null;
        }
    }
    bool mJustCycled;

    string desiredCurrentChainName;
    public string CurrentChainName
    {
        get => CurrentChain?.Name;
        set
        {
            desiredCurrentChainName = value;
            mCurrentChainIndex = -1;
            if (mAnimationChains?.Count > 0)
            {
                RefreshCurrentChainToDesiredName();
                UpdateToCurrentAnimationFrame();
            }
        }
    }

    Vector2 Position;

    IRenderableIpso mParent;

//      Sprite mTopLeftSprite = new Sprite(null);
//      Sprite mTopSprite = new Sprite(null);
//      Sprite mTopRightSprite = new Sprite(null);
//      Sprite mRightSprite = new Sprite(null);
//      Sprite mBottomRightSprite = new Sprite(null);
//      Sprite mBottomSprite = new Sprite(null);
//      Sprite mBottomLeftSprite = new Sprite(null);
//      Sprite mLeftSprite = new Sprite(null);
//      Sprite mCenterSprite = new Sprite(null);

    int mFullOutsideWidth;
    int mFullInsideWidth;

    int mFullOutsideHeight;
    int mFullInsideHeight;

    public Rectangle? SourceRectangle;

    bool ITextureCoordinate.Wrap
    {
        get => false;
        set { }// do nothing, wrapping is not supported yet
    }

    public float? TextureWidth => this.mSprites[0].Texture?.Width;
    public float? TextureHeight => this.mSprites[0].Texture?.Height;

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

    #endregion

    #region Properties

    bool IRenderableIpso.IsRenderTarget => false;
    ColorOperation IRenderableIpso.ColorOperation => ColorOperation.Modulate;

    public int Alpha
    {
        get
        {
            return Color.A;
        }
        set
        {
            if (value != Color.A)
            {
                Color = Color.WithAlpha((byte)value);
            }
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
            if (value != Color.R)
            {
                Color = Color.WithRed((byte)value);
            }
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
            if (value != Color.G)
            {
                Color = Color.WithGreen((byte)value);
            }
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
            if (value != Color.B)
            {
                Color = Color.WithBlue((byte)value);
            }
        }
    }

    public float Rotation { get; set; }

    public bool FlipHorizontal { get; set; }

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

    bool IRenderableIpso.ClipsChildren
    {
        get
        {
            return false;
        }
    }

    float IPositionedSizedObject.Width
    {
        get
        {
            return Width;
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
            return Height;
        }
        set
        {
            Height = value;
        }
    }

    public float? CustomFrameTextureCoordinateWidth
    {
        get;
        set;
    }

    bool IsOnlyRenderingCenterSprite => CustomFrameTextureCoordinateWidth <= 0;

    public Texture2D TopLeftTexture 
    {
        get { return mSprites[(int)NineSliceSections.TopLeft].Texture; }
        set { mSprites[(int) NineSliceSections.TopLeft].Texture = value; }
    }
    public Texture2D TopTexture 
    {
        get { return mSprites[(int)NineSliceSections.Top].Texture; }
        set { mSprites[(int)NineSliceSections.Top].Texture = value; }
    }
    public Texture2D TopRightTexture 
    {
        get { return mSprites[(int)NineSliceSections.TopRight].Texture; }
        set { mSprites[(int)NineSliceSections.TopRight].Texture = value; }
    }
    public Texture2D RightTexture 
    {
        get { return mSprites[(int)NineSliceSections.Right].Texture; }
        set { mSprites[(int)NineSliceSections.Right].Texture = value; }
    }
    public Texture2D BottomRightTexture 
    {
        get { return mSprites[(int)NineSliceSections.BottomRight].Texture; }
        set { mSprites[(int)NineSliceSections.BottomRight].Texture = value; }
    }
    public Texture2D BottomTexture 
    {
        get { return mSprites[(int)NineSliceSections.Bottom].Texture; }
        set { mSprites[(int)NineSliceSections.Bottom].Texture = value; }
    }
    public Texture2D BottomLeftTexture
    {
        get { return mSprites[(int)NineSliceSections.BottomLeft].Texture; }
        set { mSprites[(int)NineSliceSections.BottomLeft].Texture = value; }
    }
    public Texture2D LeftTexture
    {
        get { return mSprites[(int)NineSliceSections.Left].Texture; }
        set { mSprites[(int)NineSliceSections.Left].Texture = value; }
    }
    public Texture2D CenterTexture 
    {
        get { return mSprites[(int)NineSliceSections.Center].Texture; }
        set { mSprites[(int)NineSliceSections.Center].Texture = value; }
    }



    public bool Wrap
    {
        get { return false; }
    }

    public float X
    {
        get { return Position.X; }
        set 
        {
#if FULL_DIAGNOSTICS
            if (float.IsNaN(value))
            {
                throw new Exception("NaN is not an acceptable value");
            }
#endif
            Position.X = value; 
        }
    }

    public float Y
    {
        get { return Position.Y; }
        set 
        {
#if FULL_DIAGNOSTICS
            if (float.IsNaN(value))
            {
                throw new Exception("NaN is not an acceptable value");
            }
#endif
            Position.Y = value; 
        
        }
    }

    public float Z
    {
        get;
        set;
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

    public Color Color
    {
        get
        {
            return mSprites[(int)NineSliceSections.Center].Color;
        }
        set
        {
            mSprites[(int)NineSliceSections.TopLeft].Color = value;
            mSprites[(int)NineSliceSections.Top].Color = value;
            mSprites[(int)NineSliceSections.TopRight].Color = value;
            mSprites[(int)NineSliceSections.Right].Color = value;
            mSprites[(int)NineSliceSections.BottomRight].Color = value;
            mSprites[(int)NineSliceSections.Bottom].Color = value;
            mSprites[(int)NineSliceSections.BottomLeft].Color = value;
            mSprites[(int)NineSliceSections.Left].Color = value;
            mSprites[(int)NineSliceSections.Center].Color = value;
        }
    }

    public BlendState BlendState
    {
        get
        {
            return mSprites[(int)NineSliceSections.Center].BlendState;
        }
        set
        {
            mSprites[(int)NineSliceSections.TopLeft].BlendState = value;
            mSprites[(int)NineSliceSections.Top].BlendState = value;
            mSprites[(int)NineSliceSections.TopRight].BlendState = value;
            mSprites[(int)NineSliceSections.Right].BlendState = value;
            mSprites[(int)NineSliceSections.BottomRight].BlendState = value;
            mSprites[(int)NineSliceSections.Bottom].BlendState = value;
            mSprites[(int)NineSliceSections.BottomLeft].BlendState = value;
            mSprites[(int)NineSliceSections.Left].BlendState = value;
            mSprites[(int)NineSliceSections.Center].BlendState = value;
        }
    }

    public ObservableCollection<IRenderableIpso> Children
    {
        get { return mChildren; }
    }

    public float OutsideSpriteWidth
    {
        get { return mSprites[(int)NineSliceSections.TopLeft].Width; }
    }

    public float OutsideSpriteHeight
    {
        get { return mSprites[(int)NineSliceSections.TopLeft].Height; }
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

    float _borderScale = 1;
    public float BorderScale 
    {
        get => _borderScale;
        set
        {
            _borderScale = value;

        }
    }


    float IAspectRatio.AspectRatio
    {
        get
        {
            // EVentually we'll add this:
            //if (RenderTargetTextureSource != null)
            //{
            //    return (RenderTargetTextureSource.Width / (float)RenderTargetTextureSource.Height);
            //}
            //else 
            var texture = mSprites[(int)NineSliceSections.Center].Texture;
            if (texture != null)
            {
                return (texture.Width / (float)texture.Height);
            }
            else
            {
                return 1;
            }
        }
    }

    #endregion

    #region Methods

    [Obsolete("Do not use this, it's called automatically in rendering")]
    public void RefreshTextureCoordinatesAndSpriteSizes()
    {
        RefreshSourceRectangles();

        RefreshSpriteDimensions();
    }

    public override void Render(ISystemManagers managers)
    {
        //if (AbsoluteVisible && Width > 0 && Height > 0)
        // Why do we check absolute visible?
        // This seems to have problems:
        // 1. It's expensive
        // 2. The caller should be responsible for this
        // 3. This prevents render target rendering when the parent is invisible
        if (Width > 0 && Height > 0)
        {
            RefreshSourceRectangles();

            RefreshSpriteDimensions();

            var systemManagers = managers as SystemManagers;
            var spriteRenderer = systemManagers.Renderer.SpriteRenderer;

            float x = this.GetAbsoluteX();
            float y = this.GetAbsoluteY();
            var rotationInDegrees = this.GetAbsoluteRotation();

            if (IsOnlyRenderingCenterSprite)
            {
                var centerSprite = mSprites[(int)NineSliceSections.Center];

                centerSprite.X = x;
                centerSprite.Y = y;
                centerSprite.Rotation = rotationInDegrees;

                if (centerSprite.Height > 0 && centerSprite.Width > 0)
                {
                    Render(centerSprite, systemManagers, spriteRenderer);
                }
            }
            else 
            {
                float offsetX = 0;
                float offsetY = 0;

                Vector3 right;
                Vector3 up;


                // September 19, 2023
                // It is possible to have
                // rotations which are very 
                // close to 80, 180, or 270
                // degrees. In this situation,
                // we should hardcode the vectors
                // to avoid floating point errors. Otherwise
                // we can get small seams or overlaps in our nineslice
                // rendering

                var quarterRotations = rotationInDegrees / (float)90;
                var radiansFromPerfectRotation = System.Math.Abs(quarterRotations - MathFunctions.RoundToInt(quarterRotations));

                // 1/90 would be 1 degree. Let's go 1/10th of a degree
                const float errorToTolerate = .1f / 90f;

                if (radiansFromPerfectRotation < errorToTolerate)
                {
                    var quarterRotationsAsInt = MathFunctions.RoundToInt(quarterRotations) % 4;
                    if (quarterRotationsAsInt < 0)
                    {
                        quarterRotationsAsInt += 4;
                    }

                    // invert it to match how rotation works with the CreateRotationZ method:
                    quarterRotationsAsInt = 4 - quarterRotationsAsInt;

                    right = Vector3Extensions.Right;
                    up = Vector3Extensions.Up;

                    switch (quarterRotationsAsInt)
                    {
                        case 0:
                            right = Vector3Extensions.Right;
                            up = Vector3Extensions.Up;
                            break;
                        case 1:
                            right = Vector3Extensions.Up;
                            up = Vector3Extensions.Left;
                            break;
                        case 2:
                            right = Vector3Extensions.Left;
                            up = Vector3Extensions.Down;
                            break;

                        case 3:
                            right = Vector3Extensions.Down;
                            up = Vector3Extensions.Right;
                            break;
                    }
                }
                else
                {
                    var matrix = Matrix.CreateRotationZ(-MathHelper.ToRadians(rotationInDegrees));

                    right = matrix.Right();
                    up = matrix.Up();
                }

                mSprites[(int)NineSliceSections.TopLeft].X = x + offsetX * right.X + offsetY * up.X;
                mSprites[(int)NineSliceSections.TopLeft].Y = y + offsetX * right.Y + offsetY * up.Y;
                mSprites[(int)NineSliceSections.TopLeft].Rotation = rotationInDegrees;

                offsetX = mSprites[(int)NineSliceSections.TopLeft].Width;

                mSprites[(int)NineSliceSections.Top].X = x + offsetX * right.X + offsetY * up.X;
                mSprites[(int)NineSliceSections.Top].Y = y + offsetX * right.Y + offsetY * up.Y;
                mSprites[(int)NineSliceSections.Top].Rotation = rotationInDegrees;

                offsetX = mSprites[(int)NineSliceSections.TopLeft].Width + mSprites[(int)NineSliceSections.Top].Width;

                mSprites[(int)NineSliceSections.TopRight].X = x + offsetX * right.X + offsetY * up.X;
                mSprites[(int)NineSliceSections.TopRight].Y = y + offsetX * right.Y + offsetY * up.Y;
                mSprites[(int)NineSliceSections.TopRight].Rotation = rotationInDegrees;

                offsetX = 0;
                offsetY = mSprites[(int)NineSliceSections.TopLeft].Height;

                mSprites[(int)NineSliceSections.Left].X = x + offsetX * right.X + offsetY * up.X;
                mSprites[(int)NineSliceSections.Left].Y = y + offsetX * right.Y + offsetY * up.Y;
                mSprites[(int)NineSliceSections.Left].Rotation = rotationInDegrees;

                offsetX = mSprites[(int)NineSliceSections.Left].Width;

                mSprites[(int)NineSliceSections.Center].X = x + offsetX * right.X + offsetY * up.X;
                mSprites[(int)NineSliceSections.Center].Y = y + offsetX * right.Y + offsetY * up.Y;
                mSprites[(int)NineSliceSections.Center].Rotation = rotationInDegrees;

                offsetX = mSprites[(int)NineSliceSections.Left].Width + mSprites[(int)NineSliceSections.Center].Width;

                mSprites[(int)NineSliceSections.Right].X = x + offsetX * right.X + offsetY * up.X;
                mSprites[(int)NineSliceSections.Right].Y = y + offsetX * right.Y + offsetY * up.Y;
                mSprites[(int)NineSliceSections.Right].Rotation = rotationInDegrees;


                offsetX = 0;
                offsetY = mSprites[(int)NineSliceSections.TopLeft].Height + mSprites[(int)NineSliceSections.Left].Height;

                mSprites[(int)NineSliceSections.BottomLeft].X = x + offsetX * right.X + offsetY * up.X;
                mSprites[(int)NineSliceSections.BottomLeft].Y = y + offsetX * right.Y + offsetY * up.Y;
                mSprites[(int)NineSliceSections.BottomLeft].Rotation = rotationInDegrees;

                offsetX = mSprites[(int)NineSliceSections.BottomLeft].Width;

                mSprites[(int)NineSliceSections.Bottom].X = x + offsetX * right.X + offsetY * up.X;
                mSprites[(int)NineSliceSections.Bottom].Y = y + offsetX * right.Y + offsetY * up.Y;
                mSprites[(int)NineSliceSections.Bottom].Rotation = rotationInDegrees;

                offsetX = mSprites[(int)NineSliceSections.BottomLeft].Width + mSprites[(int)NineSliceSections.Bottom].Width;

                mSprites[(int)NineSliceSections.BottomRight].X = x + offsetX * right.X + offsetY * up.X;
                mSprites[(int)NineSliceSections.BottomRight].Y = y + offsetX * right.Y + offsetY * up.Y;
                mSprites[(int)NineSliceSections.BottomRight].Rotation = rotationInDegrees;
            




                Render(mSprites[(int)NineSliceSections.TopLeft], systemManagers, spriteRenderer);
                if (mSprites[(int)NineSliceSections.Center].Width > 0)
                {
                    Render(mSprites[(int)NineSliceSections.Top], systemManagers, spriteRenderer);
                    Render(mSprites[(int)NineSliceSections.Bottom], systemManagers, spriteRenderer);

                    if (mSprites[(int)NineSliceSections.Center].Height > 0)
                    {
                        Render(mSprites[(int)NineSliceSections.Center], systemManagers, spriteRenderer);
                    }

                }
                if (mSprites[(int)NineSliceSections.Center].Height > 0)
                {
                    Render(mSprites[(int)NineSliceSections.Left], systemManagers, spriteRenderer);
                    Render(mSprites[(int)NineSliceSections.Right], systemManagers, spriteRenderer);
                }

                Render(mSprites[(int)NineSliceSections.TopRight], systemManagers, spriteRenderer);
                Render(mSprites[(int)NineSliceSections.BottomLeft], systemManagers, spriteRenderer);
                Render(mSprites[(int)NineSliceSections.BottomRight], systemManagers, spriteRenderer);
            }
        }
    }

    private void RefreshSpriteDimensions()
    {
        bool usesMulti = mSprites[(int)NineSliceSections.TopLeft].Texture != mSprites[(int)NineSliceSections.Top].Texture;

        float desiredMiddleWidth = 0;
        float desiredMiddleHeight = 0;

        if (usesMulti == false)
        {
            if(IsOnlyRenderingCenterSprite)
            {
                // No need to update the non-center sprites because they aren't rendered anyway.
                desiredMiddleHeight = this.Height;
                desiredMiddleWidth = this.Width;

                mSprites[(int)NineSliceSections.TopLeft].Width =
                    mSprites[(int)NineSliceSections.Top].Width =
                    mSprites[(int)NineSliceSections.TopRight].Width =
                    mSprites[(int)NineSliceSections.Left].Width =
                    mSprites[(int)NineSliceSections.Right].Width =
                    mSprites[(int)NineSliceSections.BottomLeft].Width =
                    mSprites[(int)NineSliceSections.Bottom].Width =
                    mSprites[(int)NineSliceSections.BottomRight].Width = 0;

                mSprites[(int)NineSliceSections.TopLeft].Height =
                    mSprites[(int)NineSliceSections.Top].Height =
                    mSprites[(int)NineSliceSections.TopRight].Height =
                    mSprites[(int)NineSliceSections.Left].Height =
                    mSprites[(int)NineSliceSections.Right].Height =
                    mSprites[(int)NineSliceSections.BottomLeft].Height =
                    mSprites[(int)NineSliceSections.Bottom].Height =
                    mSprites[(int)NineSliceSections.BottomRight].Height = 0;
            }
            else
            {
                // single source file for each part of the NineSlice:
                var fullBorderWidth = mFullOutsideWidth * 2 * _borderScale;

                if (Width >= fullBorderWidth)
                {
                    desiredMiddleWidth = this.Width - fullBorderWidth;

                    mSprites[(int)NineSliceSections.TopLeft].Width = 
                        mSprites[(int)NineSliceSections.TopRight].Width = 
                        mSprites[(int)NineSliceSections.Left].Width = 
                        mSprites[(int)NineSliceSections.Right].Width =
                        mSprites[(int)NineSliceSections.BottomLeft].Width = 
                        mSprites[(int)NineSliceSections.BottomRight].Width = mFullOutsideWidth*_borderScale;
                }
                else
                {
                    desiredMiddleWidth = 0;
                    mSprites[(int)NineSliceSections.TopLeft].Width = 
                        mSprites[(int)NineSliceSections.TopRight].Width = 
                        mSprites[(int)NineSliceSections.Left].Width = 
                        mSprites[(int)NineSliceSections.Right].Width =
                        mSprites[(int)NineSliceSections.BottomLeft].Width = 
                        mSprites[(int)NineSliceSections.BottomRight].Width = Width / 2.0f;
                }

                float fullBorderHeight = mFullOutsideHeight * 2 * _borderScale;
                if (Height >= fullBorderHeight)
                {
                    desiredMiddleHeight = this.Height - fullBorderHeight;
                    mSprites[(int)NineSliceSections.TopLeft].Height = 
                        mSprites[(int)NineSliceSections.Top].Height =
                        mSprites[(int)NineSliceSections.TopRight].Height =
                        mSprites[(int)NineSliceSections.BottomLeft].Height =
                        mSprites[(int)NineSliceSections.Bottom].Height =
                        mSprites[(int)NineSliceSections.BottomRight].Height = mFullOutsideHeight * _borderScale;
                }
                else
                {
                    desiredMiddleHeight = 0;
                    mSprites[(int)NineSliceSections.TopLeft].Height =
                        mSprites[(int)NineSliceSections.Top].Height =
                        mSprites[(int)NineSliceSections.TopRight].Height =
                        mSprites[(int)NineSliceSections.BottomLeft].Height =
                        mSprites[(int)NineSliceSections.Bottom].Height =
                        mSprites[(int)NineSliceSections.BottomRight].Height = Height/2.0f;
                }
            }
        }
        else
        {
            desiredMiddleWidth = Width - mSprites[(int)NineSliceSections.TopLeft].Width - mSprites[(int)NineSliceSections.TopRight].Width;
            desiredMiddleHeight = Height - mSprites[(int)NineSliceSections.TopLeft].Height - this.mSprites[(int)NineSliceSections.BottomLeft].Height;
        }

        mSprites[(int)NineSliceSections.Top].Width = desiredMiddleWidth;
        mSprites[(int)NineSliceSections.Center].Width = desiredMiddleWidth;
        mSprites[(int)NineSliceSections.Bottom].Width = desiredMiddleWidth;

        mSprites[(int)NineSliceSections.Left].Height = desiredMiddleHeight;
        mSprites[(int)NineSliceSections.Center].Height = desiredMiddleHeight;
        mSprites[(int)NineSliceSections.Right].Height = desiredMiddleHeight;
    }

    private void RefreshSourceRectangles()
    {
        bool useMulti;

        useMulti = mSprites[(int)NineSliceSections.TopLeft].Texture != mSprites[(int)NineSliceSections.Top].Texture;

        if (useMulti)
        {
            if (mSprites[(int)NineSliceSections.TopLeft].Texture == null)
            {
                for (var sprite = 0; sprite < mSprites.Count(); sprite++)
                {
                    mSprites[sprite].SourceRectangle = null;
                }
            }
            else
            {
                for (var sprite = 0; sprite < mSprites.Count(); sprite++)
                {

                    mFullOutsideWidth = mSprites[(int)NineSliceSections.TopLeft].Texture.Width;
                    mFullInsideWidth = mSprites[(int)NineSliceSections.TopLeft].Texture.Width - (mFullOutsideWidth * 2);

                    mSprites[sprite].SourceRectangle = new Rectangle(0, 0, mSprites[sprite].Texture.Width, mSprites[sprite].Texture.Height);
                }

            }
        }
        else if ((mSprites[(int) NineSliceSections.TopLeft].Texture != null))
        {
            int leftCoordinate;
            int rightCoordinate;
            int topCoordinate;
            int bottomCoordinate;

            var texture = mSprites[(int)NineSliceSections.TopLeft].Texture;

            leftCoordinate = 0;
            rightCoordinate = texture.Width;
            topCoordinate = 0;
            bottomCoordinate = texture.Height;


            if (SourceRectangle.HasValue)
            {
                leftCoordinate = SourceRectangle.Value.Left;
                rightCoordinate = SourceRectangle.Value.Right;
                topCoordinate = SourceRectangle.Value.Top;
                bottomCoordinate = SourceRectangle.Value.Bottom;
            }

            if(IsOnlyRenderingCenterSprite)
            {
                mSprites[(int)NineSliceSections.Center].SourceRectangle = new Rectangle(
                    leftCoordinate,
                    topCoordinate,
                    rightCoordinate - leftCoordinate,
                    bottomCoordinate - topCoordinate);
            }
            else
            {

                int usedWidth = rightCoordinate - leftCoordinate;
                int usedHeight = bottomCoordinate - topCoordinate;

                if (CustomFrameTextureCoordinateWidth != null)
                {
                    mFullOutsideWidth = MathFunctions.RoundToInt(CustomFrameTextureCoordinateWidth.Value);
                    mFullOutsideHeight = mFullOutsideWidth;

                }
                else
                {
                    mFullOutsideWidth = (usedWidth + 1) / 3;
                    mFullOutsideHeight = (usedHeight + 1) / 3;
                }

                mFullInsideWidth = usedWidth - (mFullOutsideWidth * 2);
                mFullInsideHeight = usedHeight - (mFullOutsideHeight * 2);

                int outsideWidth = System.Math.Min(mFullOutsideWidth, RenderingLibrary.Math.MathFunctions.RoundToInt(Width / 2));
                int outsideHeight = System.Math.Min(mFullOutsideHeight, RenderingLibrary.Math.MathFunctions.RoundToInt(Height / 2));

                int topHeight = outsideHeight;
                int bottomHeight = outsideHeight;

                int insideWidth = mFullInsideWidth;
                int insideHeight = mFullInsideHeight;

                if (Height <= mFullOutsideHeight * 2 && Height % 2 == 1)
                {
                    // If this is an odd (not even) height
                    // and if the middle has 0 height, then one of the nineslices needs to be 1 pixel shorter
                    // We'll arbitrarily choose the bottom one
                    bottomHeight--;
                }

                mSprites[(int)NineSliceSections.TopLeft].SourceRectangle = new Rectangle(
                    leftCoordinate + 0,
                    topCoordinate + 0,
                    outsideWidth,
                    topHeight);
                mSprites[(int)NineSliceSections.Top].SourceRectangle = new Rectangle(
                    leftCoordinate + outsideWidth,
                    topCoordinate + 0,
                    insideWidth,
                    topHeight);
                mSprites[(int)NineSliceSections.TopRight].SourceRectangle = new Rectangle(
                    rightCoordinate - outsideWidth,
                    topCoordinate + 0,
                    outsideWidth,
                    topHeight);

                mSprites[(int)NineSliceSections.Left].SourceRectangle = new Rectangle(
                    leftCoordinate + 0,
                    topCoordinate + outsideHeight,
                    outsideWidth,
                    insideHeight);
                mSprites[(int)NineSliceSections.Center].SourceRectangle = new Rectangle(
                    leftCoordinate + outsideWidth,
                    topCoordinate + outsideHeight,
                    insideWidth,
                    insideHeight);
                mSprites[(int)NineSliceSections.Right].SourceRectangle = new Rectangle(
                    rightCoordinate - outsideWidth,
                    topCoordinate + outsideHeight,
                    outsideWidth,
                    insideHeight);

                mSprites[(int)NineSliceSections.BottomLeft].SourceRectangle = new Rectangle(
                    leftCoordinate + 0,
                    bottomCoordinate - bottomHeight,
                    outsideWidth,
                    bottomHeight);
                mSprites[(int)NineSliceSections.Bottom].SourceRectangle = new Rectangle(
                    leftCoordinate + outsideWidth,
                    bottomCoordinate - bottomHeight,
                    insideWidth,
                    bottomHeight);
                mSprites[(int)NineSliceSections.BottomRight].SourceRectangle = new Rectangle(
                    rightCoordinate - outsideWidth,
                    bottomCoordinate - bottomHeight,
                    outsideWidth,
                    bottomHeight);
            }            
        }
    }


    void Render(Sprite sprite, SystemManagers managers, SpriteRenderer spriteRenderer)
    {
        var texture = sprite.Texture;
        var sourceRectangle = sprite.EffectiveRectangle;

        // broken up to make debugging easier. Should have no impact on performance
        var color = sprite.Color;
        var flipVertical = sprite.FlipVertical;
        var rotation = sprite.Rotation;


        Sprite.Render(managers, spriteRenderer, sprite, texture, color, 
            sourceRectangle, flipVertical, rotation, treat0AsFullDimensions:false);
    }

    void IRenderableIpso.SetParentDirect(IRenderableIpso? parent)
    {
        mParent = parent;
    }


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
    IVisible? IVisible.Parent
    {
        get
        {
            return ((IRenderableIpso)this).Parent as IVisible;
        }
    }

    #endregion

    static NineSlice()
    {


    }

    public NineSlice()
    {
        Visible = true;

        mSprites = new Sprite[9];
        for (var sprite = 0; sprite < 9; sprite++)
        {
            mSprites[sprite] = new Sprite(null);
            mSprites[sprite].Name = "Unnamed nineslice Sprite" + sprite;
        }
    }

    /// <summary>
    /// Loads given texture(s) from atlas.
    /// </summary>
    /// <param name="valueAsString"></param>
    /// <param name="atlasedTexture"></param>
    public void LoadAtlasedTexture(string valueAsString, AtlasedTexture atlasedTexture)
    {
        //if made up of seperate textures
        if (NineSliceExtensions.GetIfShouldUsePattern(valueAsString))
        {
            SetTexturesUsingPattern(valueAsString, SystemManagers.Default, true);
        }
        else //single texture
        {
        }
    }

    public void SetSingleTexture(Texture2D? texture)
    {
        foreach (var sprite in mSprites)
        {
            sprite.Texture = texture;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="anyOf9Textures"></param>
    /// <param name="managers"></param>
    /// <param name="inAtlas">True if textures are atlased.</param>
    public void SetTexturesUsingPattern(string anyOf9Textures, SystemManagers managers, bool inAtlas)
    {
        string absoluteTexture = anyOf9Textures;

        if(FileManager.IsRelative(absoluteTexture))
        {
            absoluteTexture = FileManager.RelativeDirectory + absoluteTexture;

            absoluteTexture = FileManager.RemoveDotDotSlash(absoluteTexture);
        }

        string extension = FileManager.GetExtension(absoluteTexture);

        string bareTexture = NineSliceExtensions.GetBareTextureForNineSliceTexture(absoluteTexture);
        string error;
        if (!string.IsNullOrEmpty(bareTexture))
        {
            if (inAtlas)
            {
                //loop through all nine sprite names
                for (var sprite = 0; sprite < NineSliceExtensions.PossibleNineSliceEndings.Count(); sprite++)
                {

                }
            }
            else
            {
                for (var sprite = 0; sprite < NineSliceExtensions.PossibleNineSliceEndings.Count(); sprite++)
                {

                    var item = global::RenderingLibrary.Content.LoaderManager.Self.TryLoadContent<Texture2D>(bareTexture + NineSliceExtensions.PossibleNineSliceEndings[sprite] + "." + extension);

                    if(item == null)
                    {
                        item = Sprite.InvalidTexture;
                    }

                    mSprites[sprite].Texture = item;
                }
            }
        }
    }

    void IRenderable.PreRender() { }

    public NineSlice Clone()
    {
        var newInstance = (NineSlice)this.MemberwiseClone();
        newInstance.mParent = null;
        newInstance.mChildren = new ();

        return newInstance;
    }

    public void AnimationActivity(double currentTime)
    {
        if (Animate)
        {
            Animation.AnimationActivity(currentTime);

            SourceRectangle = Animation.SourceRectangle;
            SetSingleTexture(Animation.CurrentTexture);
            FlipHorizontal = Animation.FlipHorizontal;
            //FlipVertical = Animation.FlipVertical;

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
        //////////////////Early Out/////////////////////
        // Check mContainedObjectAsIVisible - if it's null, then this is a Screen and we should animate it
        if (Visible == false)
        {
            return false;
        }
        ////////////////End Early Out///////////////////

        var didChange = false;
        var shouldAnimateSelf = true;

        if (Animate == false || mCurrentChainIndex == -1 || mAnimationChains == null || mAnimationChains.Count == 0 || mAnimationChains[mCurrentChainIndex].Count == 0)
        {
            shouldAnimateSelf = false;
        }

        if (shouldAnimateSelf)
        {
            int frameBefore = mCurrentFrameIndex;

            // June 10, 2011
            // A negative animation speed should cause the animation to play in reverse
            //Removed the System.Math.Abs on the mAnimationSpeed variable to restore the correct behaviour.
            //double modifiedTimePassed = TimeManager.SecondDifference * System.Math.Abs(mAnimationSpeed);
            double modifiedTimePassed = secondDifference * mAnimationSpeed;

            mTimeIntoAnimation += modifiedTimePassed;

            AnimationChain animationChain = mAnimationChains[mCurrentChainIndex];

            mTimeIntoAnimation = MathFunctions.Loop(mTimeIntoAnimation, animationChain.TotalLength, out mJustCycled);

            UpdateFrameBasedOffOfTimeIntoAnimation();

            if (mCurrentFrameIndex != frameBefore)
            {
                didChange = UpdateToCurrentAnimationFrame();
                // Eventually we may need this? FRB uses it, but not sure if Gum needs it...
                //mJustChangedFrame = true;
            }
        }

        return didChange;
    }

    public bool UpdateToCurrentAnimationFrame()
    {
        var didChange = false;
        if (mAnimationChains != null &&
            mAnimationChains.Count > mCurrentChainIndex &&
            mCurrentChainIndex != -1 &&
            mCurrentFrameIndex > -1 &&
            mAnimationChains[mCurrentChainIndex].Count > 0
            // If we switch animations, we still want it to apply right away
            // so do a frame check:
            //mCurrentFrameIndex < mAnimationChains[mCurrentChainIndex].Count
            )
        {


            var index = mCurrentFrameIndex;
            if (index >= mAnimationChains[mCurrentChainIndex].Count)
            {
                index = 0;
            }
            var frame = mAnimationChains[mCurrentChainIndex][index];

            SetSingleTexture(frame.Texture);

            if(frame.Texture == null)
            {
                throw new InvalidOperationException($"The animation {mAnimationChains[mCurrentChainIndex].Name} has a frame with a null texture. Frames must have a texture");
            }
            var left = MathFunctions.RoundToInt(frame.LeftCoordinate * frame.Texture.Width);
            var width = MathFunctions.RoundToInt(frame.RightCoordinate * frame.Texture.Width) - left;

            var top = MathFunctions.RoundToInt(frame.TopCoordinate * frame.Texture.Height);
            var height = MathFunctions.RoundToInt(frame.BottomCoordinate * frame.Texture.Height) - top;

            SourceRectangle = new Rectangle(left, top, width, height);

            this.FlipHorizontal = frame.FlipHorizontal;
            //this.FlipVertical = frame.FlipVertical;

            didChange = true;
        }
        return didChange;
    }

    void UpdateFrameBasedOffOfTimeIntoAnimation()
    {
        double timeIntoAnimation = mTimeIntoAnimation;

        if (timeIntoAnimation < 0)
        {
            throw new ArgumentException("The timeIntoAnimation argument must be 0 or positive");
        }
        else if (CurrentChain != null &&
            // This used to check if the count was > 1, but that prevents 1-frame animations from applying, so we should do > 0
            //CurrentChain.Count > 1
            CurrentChain.Count > 0
            )
        {
            int frameIndex = 0;

            if(CurrentChain.TotalLength == 0)
            {
                // do nothing:
            }
            else
            {
                while (timeIntoAnimation >= 0)
                {
                    double frameTime = CurrentChain[frameIndex].FrameLength;

                    if (timeIntoAnimation < frameTime)
                    {
                        mCurrentFrameIndex = frameIndex;

                        break;
                    }
                    else
                    {
                        timeIntoAnimation -= frameTime;

                        frameIndex = (frameIndex + 1) % CurrentChain.Count;
                    }
                }
            }
        }
    }

    public void RefreshCurrentChainToDesiredName()
    {
        for (int i = 0; i < mAnimationChains.Count; i++)
        {
            if (mAnimationChains[i].Name == desiredCurrentChainName)
            {
                mCurrentChainIndex = i;
                break;
            }
        }
    }


    object ICloneable.Clone()
    {
        return Clone();
    }

    #endregion
}
