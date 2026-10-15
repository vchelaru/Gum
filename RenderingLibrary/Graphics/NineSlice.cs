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
using Gum.Content.AnimationChain;
using Gum.Graphics.Animation;
using RenderingLibrary.Graphics.Animation;
using System.Runtime.CompilerServices;

namespace RenderingLibrary.Graphics;

public class NineSlice : SpriteBatchRenderableBase, 
    IRenderableIpso, 
    IVisible, 
    IAspectRatio,
    ITextureCoordinate, 
    IAnimatable, IAnimatingRenderable, ICloneable
{
    #region Fields


    ObservableCollectionNoReset<IRenderableIpso> mChildren = new ();

    //Sprites which make up NineSlice indexed by NineSliceSections enum
    private Sprite[] mSprites;


    /// <summary>
    /// Shared AnimationChain playback state. Exposed so consumers can subscribe
    /// to <see cref="AnimationChainLogic.AnimationChainCycled"/> or tweak
    /// playback (speed, looping) directly. The property shortcuts below
    /// (<see cref="AnimationChains"/>, <see cref="CurrentChainName"/>,
    /// <see cref="Animate"/>, ...) forward through this object.
    /// </summary>
    public AnimationChainLogic AnimationLogic { get; private set; }

    public int CurrentFrameIndex
    {
        get => AnimationLogic.CurrentFrameIndex;
        set => AnimationLogic.CurrentFrameIndex = value;
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

    IRenderableIpso? mParent;

//      Sprite mTopLeftSprite = new Sprite(null);
//      Sprite mTopSprite = new Sprite(null);
//      Sprite mTopRightSprite = new Sprite(null);
//      Sprite mRightSprite = new Sprite(null);
//      Sprite mBottomRightSprite = new Sprite(null);
//      Sprite mBottomSprite = new Sprite(null);
//      Sprite mBottomLeftSprite = new Sprite(null);
//      Sprite mLeftSprite = new Sprite(null);
//      Sprite mCenterSprite = new Sprite(null);

    int _fullOutsideTexturePixelWidth;
    int _fullInsideTexturePixelWidth;

    int _fullOutsideTexturePixelHeight;
    int _fullInsideTexturePixelHeight;

    public Rectangle? SourceRectangle;

    bool _isTilingMiddleSections;

    bool ITextureCoordinate.Wrap
    {
        get => false;
        set { } // NineSlice does not use texture wrapping; use IsTilingMiddleSections instead
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
    ColorOperation IRenderableIpso.ColorOperation => ColorOperation;

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

    /// <summary>
    /// Whether the whole nine-slice is mirrored left to right inside its own bounds: the left and
    /// right pieces swap places and each piece is drawn mirrored.
    /// </summary>
    public bool FlipHorizontal { get; set; }

    /// <summary>
    /// Whether the whole nine-slice is mirrored top to bottom inside its own bounds.
    /// </summary>
    public bool FlipVertical { get; set; }

    /// <summary>
    /// Whether the whole nine-slice is reflected across its main diagonal (a transpose: the
    /// top-left and bottom-right corners stay put while the top-right and bottom-left swap).
    /// Applied after <see cref="FlipHorizontal"/> and <see cref="FlipVertical"/>, so a flipped
    /// nine-slice is flipped first and then transposed. The nine-slice keeps its own footprint, so
    /// a non-square one stretches the transposed image to fit.
    /// </summary>
    public bool FlipDiagonal { get; set; }

    public string? Name
    {
        get;
        set;
    }

    public object? Tag { get; set; }

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

    public Texture2D? TopLeftTexture 
    {
        get { return mSprites[(int)NineSliceSections.TopLeft].Texture; }
        set { mSprites[(int) NineSliceSections.TopLeft].Texture = value; }
    }
    public Texture2D? TopTexture 
    {
        get { return mSprites[(int)NineSliceSections.Top].Texture; }
        set { mSprites[(int)NineSliceSections.Top].Texture = value; }
    }
    public Texture2D? TopRightTexture 
    {
        get { return mSprites[(int)NineSliceSections.TopRight].Texture; }
        set { mSprites[(int)NineSliceSections.TopRight].Texture = value; }
    }
    public Texture2D? RightTexture 
    {
        get { return mSprites[(int)NineSliceSections.Right].Texture; }
        set { mSprites[(int)NineSliceSections.Right].Texture = value; }
    }
    public Texture2D? BottomRightTexture 
    {
        get { return mSprites[(int)NineSliceSections.BottomRight].Texture; }
        set { mSprites[(int)NineSliceSections.BottomRight].Texture = value; }
    }
    public Texture2D? BottomTexture 
    {
        get { return mSprites[(int)NineSliceSections.Bottom].Texture; }
        set { mSprites[(int)NineSliceSections.Bottom].Texture = value; }
    }
    public Texture2D? BottomLeftTexture
    {
        get { return mSprites[(int)NineSliceSections.BottomLeft].Texture; }
        set { mSprites[(int)NineSliceSections.BottomLeft].Texture = value; }
    }
    public Texture2D? LeftTexture
    {
        get { return mSprites[(int)NineSliceSections.Left].Texture; }
        set { mSprites[(int)NineSliceSections.Left].Texture = value; }
    }
    public Texture2D? CenterTexture
    {
        get { return mSprites[(int)NineSliceSections.Center].Texture; }
        set { mSprites[(int)NineSliceSections.Center].Texture = value; }
    }

    // The 9 slices normally share one texture (a sheet carved into regions), so the center slice
    // is a representative key for the whole NineSlice. A NineSlice built with genuinely different
    // textures per corner degrades to a slightly less optimal grouping, not a correctness bug -
    // the 9 slices already collapse into one GPU draw call among themselves regardless (they're
    // consecutive same-texture SpriteBatch.Draw calls), this only affects grouping across siblings.
    public override object? BatchSortKey => CenterTexture;



    /// <summary>
    /// Whether to tile (repeat) the middle sections instead of stretching them.
    /// When true, the Top, Bottom, Left, Right, and Center sections are rendered
    /// as repeating tiles at their natural texture size scaled by BorderScale.
    /// </summary>
    public bool IsTilingMiddleSections
    {
        get { return _isTilingMiddleSections; }
        set { _isTilingMiddleSections = value; }
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

    /// <summary>
    /// How <see cref="Color"/> combines with the texture. Forwarded to all 9 internal sprites the
    /// same way <see cref="Color"/> is.
    /// </summary>
    public ColorOperation ColorOperation
    {
        get
        {
            return mSprites[(int)NineSliceSections.Center].ColorOperation;
        }
        set
        {
            mSprites[(int)NineSliceSections.TopLeft].ColorOperation = value;
            mSprites[(int)NineSliceSections.Top].ColorOperation = value;
            mSprites[(int)NineSliceSections.TopRight].ColorOperation = value;
            mSprites[(int)NineSliceSections.Right].ColorOperation = value;
            mSprites[(int)NineSliceSections.BottomRight].ColorOperation = value;
            mSprites[(int)NineSliceSections.Bottom].ColorOperation = value;
            mSprites[(int)NineSliceSections.BottomLeft].ColorOperation = value;
            mSprites[(int)NineSliceSections.Left].ColorOperation = value;
            mSprites[(int)NineSliceSections.Center].ColorOperation = value;
        }
    }

    public new BlendState? BlendState
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

    /// <summary>
    /// A legacy hand-assigned animation, applied by <see cref="AnimationActivity"/>, which
    /// <c>SpriteManager</c> does not call for nine-slices; animation chains use
    /// <see cref="AnimationLogic"/> instead. Cloning copies it only when it implements
    /// <see cref="ICloneable"/>; otherwise the clone shares this instance.
    /// </summary>
    public IAnimation? Animation
    {
        get;
        set;
    }

    public bool Animate
    {
        get => AnimationLogic.Animate;
        set => AnimationLogic.Animate = value;
    }

    bool IAnimatingRenderable.IsAnimating => Animate && CurrentChain?.Count > 1;

    public event Action AnimationChainCycled
    {
        add => AnimationLogic.AnimationChainCycled += value;
        remove => AnimationLogic.AnimationChainCycled -= value;
    }

    /// <summary>
    /// Raised once when a non-looping animation chain reaches its end and Animate becomes false.
    /// </summary>
    public event Action AnimationChainFinished
    {
        add => AnimationLogic.AnimationChainFinished += value;
        remove => AnimationLogic.AnimationChainFinished -= value;
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

            var systemManagers = (SystemManagers)managers;
            var spriteRenderer = systemManagers.Renderer.SpriteRenderer;

            float x = this.GetAbsoluteX();
            float y = this.GetAbsoluteY();
            var rotationInDegrees = this.GetAbsoluteRotation();

            // Evaluated every render so an offset glide moves the nine-slice between frame changes.
            var renderOffset = AnimationLogic.GetRenderOffset(rotationInDegrees);
            x += renderOffset.X;
            y += renderOffset.Y;

            if (IsOnlyRenderingCenterSprite)
            {
                var centerSprite = mSprites[(int)NineSliceSections.Center];

                if (centerSprite.Height > 0 && centerSprite.Width > 0)
                {
                    var centerLayout = new SliceLayout(
                        x, y, Vector3Extensions.Right, Vector3Extensions.Up, rotationInDegrees,
                        Width, Height, FlipHorizontal, FlipVertical, FlipDiagonal);
                    RenderSection(centerSprite, centerLayout, systemManagers, spriteRenderer, 0, 0);
                }
            }
            else 
            {
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

                var layout = new SliceLayout(
                    x, y, right, up, rotationInDegrees,
                    Width, Height, FlipHorizontal, FlipVertical, FlipDiagonal);

                // Each piece's top-left corner in the nine-slice's own (unflipped) space. The
                // layout turns that into the piece's final place once any flips are applied.
                var topLeft = mSprites[(int)NineSliceSections.TopLeft];
                var top = mSprites[(int)NineSliceSections.Top];
                var left = mSprites[(int)NineSliceSections.Left];
                var center = mSprites[(int)NineSliceSections.Center];
                var bottomLeft = mSprites[(int)NineSliceSections.BottomLeft];
                var bottom = mSprites[(int)NineSliceSections.Bottom];

                float topX = topLeft.Width;
                float topRightX = topLeft.Width + top.Width;
                float middleY = topLeft.Height;
                float centerX = left.Width;
                float rightX = left.Width + center.Width;
                float bottomY = topLeft.Height + left.Height;
                float bottomX = bottomLeft.Width;
                float bottomRightX = bottomLeft.Width + bottom.Width;

                RenderSection(topLeft, layout, systemManagers, spriteRenderer, 0, 0);
                if (center.Width > 0)
                {
                    if (_isTilingMiddleSections)
                    {
                        RenderTiled(top, systemManagers, spriteRenderer, layout, topX, 0,
                            center.Width,
                            top.Height,
                            tileHorizontally: true, tileVertically: false);
                        RenderTiled(bottom, systemManagers, spriteRenderer, layout, bottomX, bottomY,
                            center.Width,
                            bottom.Height,
                            tileHorizontally: true, tileVertically: false);
                    }
                    else
                    {
                        RenderSection(top, layout, systemManagers, spriteRenderer, topX, 0);
                        RenderSection(bottom, layout, systemManagers, spriteRenderer, bottomX, bottomY);
                    }

                    if (center.Height > 0)
                    {
                        if (_isTilingMiddleSections)
                        {
                            RenderTiled(center, systemManagers, spriteRenderer, layout, centerX, middleY,
                                center.Width,
                                center.Height,
                                tileHorizontally: true, tileVertically: true);
                        }
                        else
                        {
                            RenderSection(center, layout, systemManagers, spriteRenderer, centerX, middleY);
                        }
                    }

                }
                if (center.Height > 0)
                {
                    if (_isTilingMiddleSections)
                    {
                        RenderTiled(left, systemManagers, spriteRenderer, layout, 0, middleY,
                            left.Width,
                            center.Height,
                            tileHorizontally: false, tileVertically: true);
                        RenderTiled(mSprites[(int)NineSliceSections.Right], systemManagers, spriteRenderer, layout, rightX, middleY,
                            mSprites[(int)NineSliceSections.Right].Width,
                            center.Height,
                            tileHorizontally: false, tileVertically: true);
                    }
                    else
                    {
                        RenderSection(left, layout, systemManagers, spriteRenderer, 0, middleY);
                        RenderSection(mSprites[(int)NineSliceSections.Right], layout, systemManagers, spriteRenderer, rightX, middleY);
                    }
                }

                RenderSection(mSprites[(int)NineSliceSections.TopRight], layout, systemManagers, spriteRenderer, topRightX, 0);
                RenderSection(bottomLeft, layout, systemManagers, spriteRenderer, 0, bottomY);
                RenderSection(mSprites[(int)NineSliceSections.BottomRight], layout, systemManagers, spriteRenderer, bottomRightX, bottomY);
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
                var fullBorderWidth = _fullOutsideTexturePixelWidth * 2 * _borderScale;

                if (Width >= fullBorderWidth)
                {
                    desiredMiddleWidth = this.Width - fullBorderWidth;

                    mSprites[(int)NineSliceSections.TopLeft].Width = 
                        mSprites[(int)NineSliceSections.TopRight].Width = 
                        mSprites[(int)NineSliceSections.Left].Width = 
                        mSprites[(int)NineSliceSections.Right].Width =
                        mSprites[(int)NineSliceSections.BottomLeft].Width = 
                        mSprites[(int)NineSliceSections.BottomRight].Width = _fullOutsideTexturePixelWidth*_borderScale;
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

                float fullBorderHeight = _fullOutsideTexturePixelHeight * 2 * _borderScale;
                if (Height >= fullBorderHeight)
                {
                    desiredMiddleHeight = this.Height - fullBorderHeight;
                    mSprites[(int)NineSliceSections.TopLeft].Height = 
                        mSprites[(int)NineSliceSections.Top].Height =
                        mSprites[(int)NineSliceSections.TopRight].Height =
                        mSprites[(int)NineSliceSections.BottomLeft].Height =
                        mSprites[(int)NineSliceSections.Bottom].Height =
                        mSprites[(int)NineSliceSections.BottomRight].Height = _fullOutsideTexturePixelHeight * _borderScale;
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
            var topLeftTexture = mSprites[(int)NineSliceSections.TopLeft].Texture;
            if (topLeftTexture == null)
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

                    _fullOutsideTexturePixelWidth = topLeftTexture.Width;
                    _fullInsideTexturePixelWidth = topLeftTexture.Width - (_fullOutsideTexturePixelWidth * 2);

                    var spriteTexture = mSprites[sprite].Texture;
                    // A piece can be null when its texture was assigned individually.
                    mSprites[sprite].SourceRectangle = spriteTexture == null
                        ? null
                        : new Rectangle(0, 0, spriteTexture.Width, spriteTexture.Height);
                }

            }
        }
        else if (mSprites[(int) NineSliceSections.TopLeft].Texture is Texture2D texture)
        {
            int leftCoordinate;
            int rightCoordinate;
            int topCoordinate;
            int bottomCoordinate;


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
                    _fullOutsideTexturePixelWidth = MathFunctions.RoundToInt(CustomFrameTextureCoordinateWidth.Value);
                    _fullOutsideTexturePixelHeight = _fullOutsideTexturePixelWidth;

                }
                else
                {
                    _fullOutsideTexturePixelWidth = (usedWidth + 1) / 3;
                    _fullOutsideTexturePixelHeight = (usedHeight + 1) / 3;
                }

                _fullInsideTexturePixelWidth = usedWidth - (_fullOutsideTexturePixelWidth * 2);
                _fullInsideTexturePixelHeight = usedHeight - (_fullOutsideTexturePixelHeight * 2);

                int outsideTexturePixelWidth = System.Math.Min(_fullOutsideTexturePixelWidth, 
                    RenderingLibrary.Math.MathFunctions.RoundToInt(Width / (BorderScale * 2)));
                int outsideTexturePixelHeight = System.Math.Min(_fullOutsideTexturePixelHeight,
                    RenderingLibrary.Math.MathFunctions.RoundToInt(Height / (BorderScale * 2)));

                int topHeight = outsideTexturePixelHeight;
                int bottomHeight = outsideTexturePixelHeight;

                int insideWidth = _fullInsideTexturePixelWidth;
                int insideHeight = _fullInsideTexturePixelHeight;

                if (Height <= _fullOutsideTexturePixelHeight * 2 && Height % 2 == 1)
                {
                    // If this is an odd (not even) height
                    // and if the middle has 0 height, then one of the nineslices needs to be 1 pixel shorter
                    // We'll arbitrarily choose the bottom one
                    bottomHeight--;
                }

                mSprites[(int)NineSliceSections.TopLeft].SourceRectangle = new Rectangle(
                    leftCoordinate + 0,
                    topCoordinate + 0,
                    outsideTexturePixelWidth,
                    topHeight);
                mSprites[(int)NineSliceSections.Top].SourceRectangle = new Rectangle(
                    leftCoordinate + outsideTexturePixelWidth,
                    topCoordinate + 0,
                    insideWidth,
                    topHeight);
                mSprites[(int)NineSliceSections.TopRight].SourceRectangle = new Rectangle(
                    rightCoordinate - outsideTexturePixelWidth,
                    topCoordinate + 0,
                    outsideTexturePixelWidth,
                    topHeight);

                mSprites[(int)NineSliceSections.Left].SourceRectangle = new Rectangle(
                    leftCoordinate + 0,
                    topCoordinate + outsideTexturePixelHeight,
                    outsideTexturePixelWidth,
                    insideHeight);
                mSprites[(int)NineSliceSections.Center].SourceRectangle = new Rectangle(
                    leftCoordinate + outsideTexturePixelWidth,
                    topCoordinate + outsideTexturePixelHeight,
                    insideWidth,
                    insideHeight);
                mSprites[(int)NineSliceSections.Right].SourceRectangle = new Rectangle(
                    rightCoordinate - outsideTexturePixelWidth,
                    topCoordinate + outsideTexturePixelHeight,
                    outsideTexturePixelWidth,
                    insideHeight);

                mSprites[(int)NineSliceSections.BottomLeft].SourceRectangle = new Rectangle(
                    leftCoordinate + 0,
                    bottomCoordinate - bottomHeight,
                    outsideTexturePixelWidth,
                    bottomHeight);
                mSprites[(int)NineSliceSections.Bottom].SourceRectangle = new Rectangle(
                    leftCoordinate + outsideTexturePixelWidth,
                    bottomCoordinate - bottomHeight,
                    insideWidth,
                    bottomHeight);
                mSprites[(int)NineSliceSections.BottomRight].SourceRectangle = new Rectangle(
                    rightCoordinate - outsideTexturePixelWidth,
                    bottomCoordinate - bottomHeight,
                    outsideTexturePixelWidth,
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


        // Add draws untinted and then adds Color in a second pass, matching Sprite.Render.
        bool isAdd = sprite.ColorOperation == ColorOperation.Add;

        Sprite.Render(managers, spriteRenderer, sprite, texture,
            isAdd ? Color.FromArgb(color.A, 255, 255, 255) : color,
            sourceRectangle, flipVertical, rotation, treat0AsFullDimensions:false);

        if (isAdd)
        {
            managers.Renderer.DrawAdditiveColorOverlay(managers, sprite, texture, color,
                sourceRectangle, flipVertical, rotation, flipDiagonal: false);
        }
    }

    /// <summary>
    /// Where the pieces of one nine-slice draw: its top-left corner, its axes, its rotation, and
    /// the flips applied to the whole nine-slice. Flips are applied before the transpose, matching
    /// the Skia and raylib nine-slices.
    /// </summary>
    private readonly struct SliceLayout
    {
        private readonly float _originX;
        private readonly float _originY;
        private readonly Vector3 _right;
        private readonly Vector3 _up;
        private readonly float _rotation;
        private readonly float _width;
        private readonly float _height;
        private readonly bool _flipHorizontal;
        private readonly bool _flipVertical;
        private readonly bool _flipDiagonal;

        public SliceLayout(float originX, float originY, Vector3 right, Vector3 up, float rotation,
            float width, float height, bool flipHorizontal, bool flipVertical, bool flipDiagonal)
        {
            _originX = originX;
            _originY = originY;
            _right = right;
            _up = up;
            _rotation = rotation;
            _width = width;
            _height = height;
            _flipHorizontal = flipHorizontal;
            _flipVertical = flipVertical;
            _flipDiagonal = flipDiagonal && width > 0 && height > 0;
        }

        /// <summary>
        /// Positions, sizes, rotates and flips <paramref name="sprite"/> so it draws the piece
        /// whose unflipped rectangle inside the nine-slice is the given one. The sprite's width and
        /// height are left swapped for a transposed piece; callers restore them.
        /// </summary>
        public void Place(Sprite sprite, float localX, float localY, float width, float height)
        {
            if (_flipHorizontal)
            {
                localX = _width - localX - width;
            }

            if (_flipVertical)
            {
                localY = _height - localY - height;
            }

            float rotation = _rotation;
            bool spriteFlipHorizontal = _flipHorizontal;

            if (_flipDiagonal)
            {
                // Transposing stretches the piece into the nine-slice's own footprint, so a
                // non-square nine-slice scales each rectangle by the ratio of its sides.
                float widthOverHeight = _width / _height;
                float heightOverWidth = _height / _width;
                (localX, localY, width, height) = (
                    localY * widthOverHeight,
                    localX * heightOverWidth,
                    height * widthOverHeight,
                    width * heightOverWidth);

                // MonoGame has no diagonal SpriteEffects, but a transpose is a quarter turn plus a
                // horizontal mirror. Turned about its bottom-left corner the sprite's own width runs
                // up the screen and its height runs across it, so it is sized swapped.
                sprite.Width = height;
                sprite.Height = width;
                localY += height;
                rotation += 90;
                spriteFlipHorizontal = !spriteFlipHorizontal;
            }
            else
            {
                sprite.Width = width;
                sprite.Height = height;
            }

            sprite.X = _originX + localX * _right.X + localY * _up.X;
            sprite.Y = _originY + localX * _right.Y + localY * _up.Y;
            sprite.Rotation = rotation;
            sprite.FlipHorizontal = spriteFlipHorizontal;
            sprite.FlipVertical = _flipVertical;
        }
    }

    void RenderSection(Sprite sprite, in SliceLayout layout, SystemManagers systemManagers,
        SpriteRenderer spriteRenderer, float localX, float localY)
    {
        float originalWidth = sprite.Width;
        float originalHeight = sprite.Height;

        layout.Place(sprite, localX, localY, originalWidth, originalHeight);
        Render(sprite, systemManagers, spriteRenderer);

        sprite.Width = originalWidth;
        sprite.Height = originalHeight;
    }

    void RenderTiled(
        Sprite sprite,
        SystemManagers systemManagers,
        SpriteRenderer spriteRenderer,
        in SliceLayout layout,
        float localX,
        float localY,
        float fillWidth,
        float fillHeight,
        bool tileHorizontally,
        bool tileVertically)
    {
        if (sprite.Texture == null || sprite.SourceRectangle == null)
        {
            return;
        }

        var originalSourceRect = sprite.SourceRectangle.Value;
        float originalX = sprite.X;
        float originalY = sprite.Y;
        float originalWidth = sprite.Width;
        float originalHeight = sprite.Height;

        float tileRenderWidth = tileHorizontally
            ? originalSourceRect.Width * _borderScale
            : fillWidth;
        float tileRenderHeight = tileVertically
            ? originalSourceRect.Height * _borderScale
            : fillHeight;

        if (tileRenderWidth <= 0 || tileRenderHeight <= 0)
        {
            return;
        }

        float currentY = 0;
        while (currentY < fillHeight)
        {
            float remainingHeight = fillHeight - currentY;
            float thisHeight = tileVertically
                ? System.Math.Min(tileRenderHeight, remainingHeight)
                : fillHeight;

            float currentX = 0;
            while (currentX < fillWidth)
            {
                float remainingWidth = fillWidth - currentX;
                float thisWidth = tileHorizontally
                    ? System.Math.Min(tileRenderWidth, remainingWidth)
                    : fillWidth;

                // For partial tiles, reduce the source rectangle proportionally.
                // Use Math.Max(1, ...) so the last tile always renders at least 1 source
                // pixel, preventing sub-pixel gaps when the remaining space is very small.
                int srcW = tileHorizontally && thisWidth < tileRenderWidth
                    ? System.Math.Max(1, MathFunctions.RoundToInt(originalSourceRect.Width * (thisWidth / tileRenderWidth)))
                    : originalSourceRect.Width;
                int srcH = tileVertically && thisHeight < tileRenderHeight
                    ? System.Math.Max(1, MathFunctions.RoundToInt(originalSourceRect.Height * (thisHeight / tileRenderHeight)))
                    : originalSourceRect.Height;

                if (srcW > 0 && srcH > 0)
                {
                    sprite.SourceRectangle = new Rectangle(
                        originalSourceRect.X, originalSourceRect.Y, srcW, srcH);
                    layout.Place(sprite, localX + currentX, localY + currentY, thisWidth, thisHeight);

                    Render(sprite, systemManagers, spriteRenderer);
                }

                if (!tileHorizontally)
                {
                    break;
                }
                currentX += thisWidth;
            }

            if (!tileVertically)
            {
                break;
            }
            currentY += thisHeight;
        }

        // Restore sprite state
        sprite.X = originalX;
        sprite.Y = originalY;
        sprite.Width = originalWidth;
        sprite.Height = originalHeight;
        sprite.SourceRectangle = originalSourceRect;
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

        AnimationLogic = new AnimationChainLogic();
        AnimationLogic.ApplyFrame = ApplyAnimationFrame;
        AnimationLogic.ApplyKeyframeColor = ApplyKeyframeColor;
    }

    void ApplyAnimationFrame(AnimationFrame frame)
    {
        if (frame.Texture == null)
        {
            throw new InvalidOperationException(
                $"The animation {AnimationLogic.CurrentChain?.Name} has a frame with a null texture. Frames must have a texture");
        }

        SetSingleTexture(frame.Texture);

        var left = MathFunctions.RoundToInt(frame.LeftCoordinate * frame.Texture.Width);
        var width = MathFunctions.RoundToInt(frame.RightCoordinate * frame.Texture.Width) - left;
        var top = MathFunctions.RoundToInt(frame.TopCoordinate * frame.Texture.Height);
        var height = MathFunctions.RoundToInt(frame.BottomCoordinate * frame.Texture.Height) - top;
        SourceRectangle = new Rectangle(left, top, width, height);

        FlipHorizontal = frame.FlipHorizontal;
        FlipVertical = frame.FlipVertical;
        FlipDiagonal = frame.FlipDiagonal;

        // A chain with color keyframes gets its color from ApplyKeyframeColor instead, which also
        // ignores a skipped frame's own channels.
        if (!AnimationLogic.IsColorDrivenByKeyframes)
        {
            ApplyColor(frame.Alpha, frame.Red, frame.Green, frame.Blue, frame.ColorOperation);
        }
    }

    void ApplyKeyframeColor(KeyframeColor color) =>
        ApplyColor(color.Alpha, color.Red, color.Green, color.Blue, color.Operation);

    void ApplyColor(int? alpha, int? red, int? green, int? blue, AnimationFrameColorOperation? colorOperation)
    {
        if (alpha.HasValue)
        {
            Alpha = alpha.Value;
        }

        if (colorOperation == AnimationFrameColorOperation.Multiply)
        {
            Red = red ?? 255;
            Green = green ?? 255;
            Blue = blue ?? 255;
            ColorOperation = ColorOperation.Modulate;
        }
        else if (colorOperation == AnimationFrameColorOperation.Add)
        {
            // Black (0) is Add's identity, so an unset channel contributes nothing - unlike
            // Multiply's 255 identity above. Mirrors RenderingLibrary.Graphics.Sprite.
            Red = red ?? 0;
            Green = green ?? 0;
            Blue = blue ?? 0;
            ColorOperation = ColorOperation.Add;
        }
        else if (ColorOperation == ColorOperation.Add)
        {
            ColorOperation = ColorOperation.Modulate;
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
    public void SetTexturesUsingPattern(string anyOf9Textures, SystemManagers managers)
    {
        string absoluteTexture = anyOf9Textures;

        if(FileManager.IsRelative(absoluteTexture))
        {
            absoluteTexture = FileManager.RelativeDirectory + absoluteTexture;

            absoluteTexture = FileManager.RemoveDotDotSlash(absoluteTexture);
        }

        string extension = FileManager.GetExtension(absoluteTexture);

        string bareTexture = NineSliceExtensions.GetBareTextureForNineSliceTexture(absoluteTexture);
        if (!string.IsNullOrEmpty(bareTexture))
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

    void IRenderable.PreRender() { }

    public NineSlice Clone()
    {
        var newInstance = (NineSlice)this.MemberwiseClone();
        newInstance.mParent = null;
        newInstance.mChildren = new ();
        // The section sprites hold this nine-slice's textures and colors, so the clone needs its
        // own; otherwise its texture or color changes would show on the source.
        newInstance.mSprites = new Sprite[mSprites.Length];
        for (int i = 0; i < mSprites.Length; i++)
        {
            newInstance.mSprites[i] = mSprites[i].Clone();
        }
        newInstance.AnimationLogic = AnimationLogic.Clone(newInstance.ApplyAnimationFrame, newInstance.ApplyKeyframeColor);
        // IAnimation has no clone contract: copy an implementation that opts in through
        // ICloneable, and share any other one, since it may hold state that cannot be copied safely.
        if (Animation is ICloneable cloneableAnimation)
        {
            newInstance.Animation = (IAnimation)cloneableAnimation.Clone();
        }

        return newInstance;
    }

    public void AnimationActivity(double currentTime)
    {
        // SpriteManager.Activity does not call this for nine-slices; the caller must.
        if (Animate && Animation != null)
        {
            Animation.AnimationActivity(currentTime);

            SourceRectangle = Animation.SourceRectangle;
            SetSingleTexture(Animation.CurrentTexture);
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
        // Check mContainedObjectAsIVisible - if it's null, then this is a Screen and we should animate it
        if (Visible == false)
        {
            return false;
        }

        return AnimationLogic.AnimateSelf(secondDifference);
    }

    public bool UpdateToCurrentAnimationFrame() => AnimationLogic.UpdateToCurrentAnimationFrame();

    public void RefreshCurrentChainToDesiredName() => AnimationLogic.RefreshCurrentChainToDesiredName();


    object ICloneable.Clone()
    {
        return Clone();
    }

    #endregion
}
