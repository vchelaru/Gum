using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ToolsUtilities;
using RenderingLibrary.Content;
using System.Collections.ObjectModel;
using RenderingLibrary.Math;

namespace RenderingLibrary.Graphics
{
    public class NineSlice : IRenderableIpso, IVisible
    {
        #region Fields


        ObservableCollection<IRenderableIpso> mChildren = new ObservableCollection<IRenderableIpso>();

        //Sprites which make up NineSlice indexed by NineSliceSections enum
        private Sprite[] mSprites;

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

        #endregion

        #region Properties

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
                    Color = new Color(Color.R, Color.G, Color.B, value);
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
                    Color = new Color(value, Color.G, Color.B, Color.A);
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
                    Color = new Color(Color.R, value, Color.B, Color.A);
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
                    Color = new Color(Color.R, Color.G, value, Color.A);
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

        public float EffectiveWidth
        {
            get
            {
                // I think we want to treat these individually so a 
                // width could be set but height could be default

                return Width;

            }
        }

        public float EffectiveHeight
        {
            get
            {
                return Height;
            }
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

        public float? CustomFrameTextureCoordinateWidth
        {
            get;
            set;
        }

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
#if DEBUG
                if(float.IsNaN(value))
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
#if DEBUG
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
            get { return mSprites[(int)NineSliceSections.TopLeft].EffectiveWidth; }
        }

        public float OutsideSpriteHeight
        {
            get { return mSprites[(int)NineSliceSections.TopLeft].EffectiveHeight; }
        }

        #endregion

        #region Methods

        public void RefreshTextureCoordinatesAndSpriteSizes()
        {
            RefreshSourceRectangles();

            RefreshSpriteDimensions();
        }

        void IRenderable.Render(SpriteRenderer spriteRenderer, SystemManagers managers)
        {
            if (AbsoluteVisible && Width > 0 && Height > 0)
            {
                RefreshSourceRectangles();

                RefreshSpriteDimensions();

                float x = this.GetAbsoluteX();
                float y = this.GetAbsoluteY();
                float offsetX = 0;
                float offsetY = 0;

                Vector3 right;
                Vector3 up;

                var rotationInDegrees = this.GetAbsoluteRotation();

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

                if(radiansFromPerfectRotation < errorToTolerate)
                {
                    var quarterRotationsAsInt = MathFunctions.RoundToInt(quarterRotations) % 4;
                    if(quarterRotationsAsInt < 0)
                    {
                        quarterRotationsAsInt += 4;
                    }

                    // invert it to match how rotation works with the CreateRotationZ method:
                    quarterRotationsAsInt = 4 - quarterRotationsAsInt;

                    right = Vector3.Right;
                    up = Vector3.Up;

                    switch (quarterRotationsAsInt)
                    {
                        case 0:
                            right = Vector3.Right;
                            up = Vector3.Up;
                            break;
                        case 1:
                            right = Vector3.Up;
                            up = Vector3.Left;
                            break;
                        case 2:
                            right = Vector3.Left;
                            up = Vector3.Down;
                            break;

                        case 3:
                            right = Vector3.Down;
                            up = Vector3.Right;
                            break;
                    }


                }
                else
                {
                    var matrix = Matrix.CreateRotationZ(-MathHelper.ToRadians(rotationInDegrees));

                    right = matrix.Right;
                    up = matrix.Up;
                }

                mSprites[(int)NineSliceSections.TopLeft].X = x + offsetX * right.X + offsetY * up.X;
                mSprites[(int)NineSliceSections.TopLeft].Y = y + offsetX * right.Y + offsetY * up.Y; 
                mSprites[(int)NineSliceSections.TopLeft].Rotation = rotationInDegrees;

                offsetX = mSprites[(int)NineSliceSections.TopLeft].EffectiveWidth;

                mSprites[(int)NineSliceSections.Top].X = x + offsetX * right.X + offsetY * up.X;
                mSprites[(int)NineSliceSections.Top].Y = y + offsetX * right.Y + offsetY * up.Y;
                mSprites[(int)NineSliceSections.Top].Rotation = rotationInDegrees;

                offsetX = mSprites[(int)NineSliceSections.TopLeft].EffectiveWidth + mSprites[(int)NineSliceSections.Top].Width;

                mSprites[(int)NineSliceSections.TopRight].X = x + offsetX * right.X + offsetY * up.X;
                mSprites[(int)NineSliceSections.TopRight].Y = y + offsetX * right.Y + offsetY * up.Y; 
                mSprites[(int)NineSliceSections.TopRight].Rotation = rotationInDegrees;

                offsetX = 0;
                offsetY = mSprites[(int)NineSliceSections.TopLeft].EffectiveHeight;

                mSprites[(int)NineSliceSections.Left].X = x + offsetX * right.X + offsetY * up.X;
                mSprites[(int)NineSliceSections.Left].Y = y + offsetX * right.Y + offsetY * up.Y; 
                mSprites[(int)NineSliceSections.Left].Rotation = rotationInDegrees;

                offsetX = mSprites[(int)NineSliceSections.Left].EffectiveWidth;

                mSprites[(int)NineSliceSections.Center].X = x + offsetX * right.X + offsetY * up.X;
                mSprites[(int)NineSliceSections.Center].Y = y + offsetX * right.Y + offsetY * up.Y; 
                mSprites[(int)NineSliceSections.Center].Rotation = rotationInDegrees;

                offsetX = mSprites[(int)NineSliceSections.Left].EffectiveWidth + mSprites[(int)NineSliceSections.Center].Width;

                mSprites[(int)NineSliceSections.Right].X = x + offsetX * right.X + offsetY * up.X;
                mSprites[(int)NineSliceSections.Right].Y = y + offsetX * right.Y + offsetY * up.Y; 
                mSprites[(int)NineSliceSections.Right].Rotation = rotationInDegrees;


                offsetX = 0;
                offsetY = mSprites[(int)NineSliceSections.TopLeft].EffectiveHeight + mSprites[(int)NineSliceSections.Left].Height;

                mSprites[(int)NineSliceSections.BottomLeft].X = x + offsetX * right.X + offsetY * up.X;
                mSprites[(int)NineSliceSections.BottomLeft].Y = y + offsetX * right.Y + offsetY * up.Y; 
                mSprites[(int)NineSliceSections.BottomLeft].Rotation = rotationInDegrees;

                offsetX = mSprites[(int)NineSliceSections.BottomLeft].EffectiveWidth;

                mSprites[(int)NineSliceSections.Bottom].X = x + offsetX * right.X + offsetY * up.X;
                mSprites[(int)NineSliceSections.Bottom].Y = y + offsetX * right.Y + offsetY * up.Y; 
                mSprites[(int)NineSliceSections.Bottom].Rotation = rotationInDegrees;

                offsetX = mSprites[(int)NineSliceSections.BottomLeft].EffectiveWidth + mSprites[(int)NineSliceSections.Bottom].Width;

                mSprites[(int)NineSliceSections.BottomRight].X = x + offsetX * right.X + offsetY * up.X;
                mSprites[(int)NineSliceSections.BottomRight].Y = y + offsetX * right.Y + offsetY * up.Y; 
                mSprites[(int)NineSliceSections.BottomRight].Rotation = rotationInDegrees;

                Render(mSprites[(int)NineSliceSections.TopLeft], managers, spriteRenderer);
                if (mSprites[(int)NineSliceSections.Center].Width > 0)
                {
                    Render(mSprites[(int)NineSliceSections.Top], managers, spriteRenderer);
                    Render(mSprites[(int)NineSliceSections.Bottom], managers, spriteRenderer);

                    if (mSprites[(int)NineSliceSections.Center].Height > 0)
                    {
                        Render(mSprites[(int)NineSliceSections.Center], managers, spriteRenderer);
                    }

                }
                if (mSprites[(int)NineSliceSections.Center].Height > 0)
                {
                    Render(mSprites[(int)NineSliceSections.Left], managers, spriteRenderer);
                    Render(mSprites[(int)NineSliceSections.Right], managers, spriteRenderer);
                }

                Render(mSprites[(int)NineSliceSections.TopRight], managers, spriteRenderer);
                Render(mSprites[(int)NineSliceSections.BottomLeft], managers, spriteRenderer);
                Render(mSprites[(int)NineSliceSections.BottomRight], managers, spriteRenderer);
            }
        }

        private void RefreshSpriteDimensions()
        {
            bool usesMulti = mSprites[(int)NineSliceSections.TopLeft].Texture != mSprites[(int)NineSliceSections.Top].Texture;

            float desiredMiddleWidth = 0;
            float desiredMiddleHeight = 0;

            if (usesMulti == false)
            {
                float fullWidth = mFullOutsideWidth * 2 + mFullInsideWidth;
                if (Width >= fullWidth)
                {
                    desiredMiddleWidth = Width - mSprites[(int)NineSliceSections.TopLeft].EffectiveWidth - mSprites[(int)NineSliceSections.TopRight].EffectiveWidth;

                    mSprites[(int)NineSliceSections.TopLeft].Width = mSprites[(int)NineSliceSections.TopRight].Width = mSprites[(int)NineSliceSections.Left].Width = mSprites[(int)NineSliceSections.Right].Width =
                        mSprites[(int)NineSliceSections.BottomLeft].Width = mSprites[(int)NineSliceSections.BottomRight].Width = mFullOutsideWidth;
                }
                else if (Width >= mFullOutsideWidth * 2)
                {
                    desiredMiddleWidth = this.Width - mFullOutsideWidth * 2;

                    mSprites[(int)NineSliceSections.TopLeft].Width = mSprites[(int)NineSliceSections.TopRight].Width = mSprites[(int)NineSliceSections.Left].Width = mSprites[(int)NineSliceSections.Right].Width =
                         mSprites[(int)NineSliceSections.BottomLeft].Width = mSprites[(int)NineSliceSections.BottomRight].Width = mFullOutsideWidth;
                }
                else
                {
                    desiredMiddleWidth = 0;
                    mSprites[(int)NineSliceSections.TopLeft].Width = mSprites[(int)NineSliceSections.TopRight].Width = 
                        mSprites[(int)NineSliceSections.Left].Width = mSprites[(int)NineSliceSections.Right].Width =
                        mSprites[(int)NineSliceSections.BottomLeft].Width = mSprites[(int)NineSliceSections.BottomRight].Width = Width / 2.0f;
                }

                float fullHeight = mFullOutsideHeight * 2 + mFullInsideHeight;
                if (Height >= fullHeight)
                {
                    desiredMiddleHeight = this.Height - mSprites[(int)NineSliceSections.TopLeft].EffectiveHeight - mSprites[(int)NineSliceSections.TopRight].EffectiveHeight;
                }
                else if (Height >= mFullOutsideHeight * 2)
                {
                    desiredMiddleHeight = this.Height - mFullOutsideHeight * 2;
                }
                else
                {
                    desiredMiddleHeight = 0;
                }
            }
            else
            {
                desiredMiddleWidth = Width - mSprites[(int)NineSliceSections.TopLeft].EffectiveWidth - mSprites[(int)NineSliceSections.TopRight].EffectiveWidth;
                desiredMiddleHeight = Height - mSprites[(int)NineSliceSections.TopLeft].EffectiveHeight - this.mSprites[(int)NineSliceSections.BottomLeft].EffectiveHeight;
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
            var useAtlas = mSprites[(int) NineSliceSections.TopLeft].AtlasedTexture != null;

            if (useAtlas)
            {
                useMulti = mSprites[(int) NineSliceSections.TopLeft].AtlasedTexture.Name !=
                           mSprites[(int) NineSliceSections.Top].AtlasedTexture.Name;
            }
            else //not using atlas
            {
                useMulti = mSprites[(int)NineSliceSections.TopLeft].Texture != mSprites[(int)NineSliceSections.Top].Texture;
            }

            if (useMulti)
            {
                if ((!useAtlas && mSprites[(int)NineSliceSections.TopLeft].Texture == null) ||
                    (useAtlas && mSprites[(int)NineSliceSections.TopLeft].AtlasedTexture == null))
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
                        if (useAtlas)
                        {
                            if (sprite == (int) NineSliceSections.TopLeft)
                            {
                                mFullOutsideWidth = mSprites[sprite].AtlasedTexture.SourceRectangle.Width;
                                mFullInsideWidth = mSprites[sprite].AtlasedTexture.SourceRectangle.Width - (mFullOutsideWidth * 2);
                            }
                        }
                        else
                        {
                            mFullOutsideWidth = mSprites[(int)NineSliceSections.TopLeft].Texture.Width;
                            mFullInsideWidth = mSprites[(int)NineSliceSections.TopLeft].Texture.Width - (mFullOutsideWidth * 2);

                            mSprites[sprite].SourceRectangle = new Rectangle(0, 0, mSprites[sprite].Texture.Width, mSprites[sprite].Texture.Height);
                        }
                    }

                }
            }
            else if ((!useAtlas && mSprites[(int) NineSliceSections.TopLeft].Texture != null) ||
                     (useAtlas && mSprites[(int) NineSliceSections.TopLeft].AtlasedTexture != null))
            {
                int leftCoordinate;
                int rightCoordinate;
                int topCoordinate;
                int bottomCoordinate;

                if (useAtlas)
                {
                    var atlasedTexture = mSprites[(int) NineSliceSections.TopLeft].AtlasedTexture;

                    leftCoordinate = atlasedTexture.SourceRectangle.Left;
                    rightCoordinate = atlasedTexture.SourceRectangle.Right;
                    topCoordinate = atlasedTexture.SourceRectangle.Top;
                    bottomCoordinate = atlasedTexture.SourceRectangle.Bottom;
                }
                else
                {
                    var texture = mSprites[(int)NineSliceSections.TopLeft].Texture;

                    leftCoordinate = 0;
                    rightCoordinate = texture.Width;
                    topCoordinate = 0;
                    bottomCoordinate = texture.Height;
                }


                if (SourceRectangle.HasValue)
                {
                    leftCoordinate = SourceRectangle.Value.Left;
                    rightCoordinate = SourceRectangle.Value.Right;
                    topCoordinate = SourceRectangle.Value.Top;
                    bottomCoordinate = SourceRectangle.Value.Bottom;
                }

                int usedWidth = rightCoordinate - leftCoordinate;
                int usedHeight = bottomCoordinate - topCoordinate;

                if(CustomFrameTextureCoordinateWidth != null)
                {
                    mFullOutsideWidth = MathFunctions.RoundToInt( CustomFrameTextureCoordinateWidth.Value);
                    mFullOutsideHeight = mFullOutsideWidth;

                }
                else
                {
                    mFullOutsideWidth = (usedWidth + 1) / 3;
                    mFullOutsideHeight = (usedHeight + 1) / 3;
                }

                mFullInsideWidth = usedWidth - (mFullOutsideWidth * 2);
                mFullInsideHeight = usedHeight - (mFullOutsideHeight * 2);

                int outsideWidth = System.Math.Min(mFullOutsideWidth, RenderingLibrary.Math.MathFunctions.RoundToInt(Width / 2)); ;
                int outsideHeight = System.Math.Min(mFullOutsideHeight, RenderingLibrary.Math.MathFunctions.RoundToInt(Height / 2));
                int insideWidth = mFullInsideWidth;
                int insideHeight = mFullInsideHeight;

                mSprites[(int)NineSliceSections.TopLeft].SourceRectangle = new Rectangle(
                    leftCoordinate + 0,
                    topCoordinate + 0,
                    outsideWidth,
                    outsideHeight);
                mSprites[(int)NineSliceSections.Top].SourceRectangle = new Rectangle(
                    leftCoordinate + outsideWidth,
                    topCoordinate + 0,
                    insideWidth,
                    outsideHeight);
                mSprites[(int)NineSliceSections.TopRight].SourceRectangle = new Rectangle(
                    rightCoordinate - outsideWidth,
                    topCoordinate + 0,
                    outsideWidth,
                    outsideHeight);

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
                    bottomCoordinate - outsideHeight,
                    outsideWidth,
                    outsideHeight);
                mSprites[(int)NineSliceSections.Bottom].SourceRectangle = new Rectangle(
                    leftCoordinate + outsideWidth,
                    bottomCoordinate - outsideHeight,
                    insideWidth,
                    outsideHeight);
                mSprites[(int)NineSliceSections.BottomRight].SourceRectangle = new Rectangle(
                    rightCoordinate - outsideWidth,
                    bottomCoordinate - outsideHeight,
                    outsideWidth,
                    outsideHeight);
            }

            //top
            var tempRect = mSprites[(int) NineSliceSections.Top].SourceRectangle;
            if (!mSprites[(int)NineSliceSections.Top].SourceRectangle.HasValue && useAtlas)
                tempRect = mSprites[(int) NineSliceSections.Top].AtlasedTexture.SourceRectangle;

            if (tempRect.HasValue)
            {
                mSprites[(int)NineSliceSections.Top].Height = tempRect.Value.Height;
                mSprites[(int)NineSliceSections.TopLeft].Height = mSprites[(int)NineSliceSections.Top].Height;
                mSprites[(int)NineSliceSections.TopRight].Height = mSprites[(int)NineSliceSections.Top].Height;
            }

            //bottom
            tempRect = mSprites[(int)NineSliceSections.Bottom].SourceRectangle;
            if (!mSprites[(int)NineSliceSections.Bottom].SourceRectangle.HasValue && useAtlas)
                tempRect = mSprites[(int)NineSliceSections.Bottom].AtlasedTexture.SourceRectangle;

            if (tempRect.HasValue)
            {
                mSprites[(int)NineSliceSections.Bottom].Height = tempRect.Value.Height;
                mSprites[(int)NineSliceSections.BottomRight].Height = mSprites[(int)NineSliceSections.Bottom].Height;
                mSprites[(int)NineSliceSections.BottomLeft].Height = mSprites[(int)NineSliceSections.Bottom].Height;
            }

            //left
            tempRect = mSprites[(int)NineSliceSections.Left].SourceRectangle;
            if (!mSprites[(int)NineSliceSections.Left].SourceRectangle.HasValue && useAtlas)
                tempRect = mSprites[(int)NineSliceSections.Left].AtlasedTexture.SourceRectangle;

            if (tempRect.HasValue)
            {
                mSprites[(int)NineSliceSections.Left].Width = tempRect.Value.Width;
                mSprites[(int)NineSliceSections.TopLeft].Width = mSprites[(int)NineSliceSections.Left].Width;
                mSprites[(int)NineSliceSections.BottomLeft].Width = mSprites[(int)NineSliceSections.Left].Width;
            }

            //right
            tempRect = mSprites[(int)NineSliceSections.Right].SourceRectangle;
            if (!mSprites[(int)NineSliceSections.Right].SourceRectangle.HasValue && useAtlas)
                tempRect = mSprites[(int)NineSliceSections.Right].AtlasedTexture.SourceRectangle;

            if (tempRect.HasValue)
            {
                mSprites[(int)NineSliceSections.Right].Width = tempRect.Value.Width;
                mSprites[(int)NineSliceSections.TopRight].Width = mSprites[(int)NineSliceSections.Right].Width;
                mSprites[(int)NineSliceSections.BottomRight].Width = mSprites[(int)NineSliceSections.Right].Width;
            }
        }


        void Render(Sprite sprite, SystemManagers managers, SpriteRenderer spriteRenderer)
        {
            var texture = sprite.Texture;
            var sourceRectangle = sprite.EffectiveRectangle;
            if (sprite.AtlasedTexture != null) texture = sprite.AtlasedTexture.Texture;

            // broken up to make debugging easier. Should have no impact on performance
            var color = sprite.Color;
            var flipVertical = sprite.FlipVertical;
            var rotation = sprite.Rotation;


            Sprite.Render(managers, spriteRenderer, sprite, texture, color, 
                sourceRectangle, flipVertical, rotation, treat0AsFullDimensions:false);
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
                foreach (var sprite in mSprites)
                {
                    sprite.AtlasedTexture = atlasedTexture;
                }   
            }
        }

        public void SetSingleTexture(Texture2D texture)
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
                        var atlasedTexture = LoaderManager.Self.TryLoadContent<AtlasedTexture>
                            (bareTexture + NineSliceExtensions.PossibleNineSliceEndings[sprite] + "." + extension);

                        if (atlasedTexture != null) mSprites[sprite].AtlasedTexture = atlasedTexture;
                    }
                }
                else
                {
                    for (var sprite = 0; sprite < NineSliceExtensions.PossibleNineSliceEndings.Count(); sprite++)
                    {
                        mSprites[sprite].Texture = LoaderManager.Self.LoadOrInvalid(
                            bareTexture + NineSliceExtensions.PossibleNineSliceEndings[sprite] + "." + extension, managers, out error);
                    }
                }
            }
        }



        void IRenderable.PreRender() { }
        #endregion
    }
}
