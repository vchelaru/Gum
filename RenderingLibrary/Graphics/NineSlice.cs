using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ToolsUtilities;
using RenderingLibrary.Content;

namespace RenderingLibrary.Graphics
{
    #region Enums

    public enum NineSliceSections
    {
        TopLeft,
        Top,
        TopRight,
        Left,
        Center,
        Right,
        BottomLeft,
        Bottom,
        BottomRight
    }

    #endregion

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

        int mFullOutsideWidth;
        int mFullInsideWidth;

        int mFullOutsideHeight;
        int mFullInsideHeight;

        public Rectangle? SourceRectangle;


        #endregion

        #region Properties

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

        public Color Color
        {
            get
            {
                return mCenterSprite.Color;
            }
            set
            {
                mTopLeftSprite.Color = value;
                mTopSprite.Color = value;
                mTopRightSprite.Color = value;
                mRightSprite.Color = value;
                mBottomRightSprite.Color = value;
                mBottomSprite.Color = value;
                mBottomLeftSprite.Color = value;
                mLeftSprite.Color = value;
                mCenterSprite.Color = value;
            }
        }

        public BlendState BlendState
        {
            get
            {
                return mCenterSprite.BlendState;
            }
            set
            {
                mTopLeftSprite.BlendState = value;
                mTopSprite.BlendState = value;
                mTopRightSprite.BlendState = value;
                mRightSprite.BlendState = value;
                mBottomRightSprite.BlendState = value;
                mBottomSprite.BlendState = value;
                mBottomLeftSprite.BlendState = value;
                mLeftSprite.BlendState = value;
                mCenterSprite.BlendState = value;
            }
        }



        public List<IPositionedSizedObject> Children
        {
            get { return mChildren; }
        }

        public static Dictionary<NineSliceSections, string> PossibleNineSliceEndings
        {
            get;
            private set;
        }

        public float OutsideSpriteWidth
        {
            get { return mTopLeftSprite.EffectiveWidth; }
        }

        public float OutsideSpriteHeight
        {
            get { return mTopLeftSprite.EffectiveHeight; }
        }

        #endregion


        #region Methods

        public void RefreshTextureCoordinatesAndSpriteSizes()
        {
            RefreshSourceRectangles();

            RefreshSpriteDimensions();
        }

        void IRenderable.Render(SpriteBatch spriteBatch, SystemManagers managers)
        {
            if (this.AbsoluteVisible && Width > 0 && Height > 0)
            {
                


                RefreshSourceRectangles();


                RefreshSpriteDimensions();

                float y = this.GetAbsoluteY();

                mTopLeftSprite.X = this.GetAbsoluteX() ;
                mTopLeftSprite.Y = y;

                mTopSprite.X = mTopLeftSprite.X + mTopLeftSprite.EffectiveWidth;
                mTopSprite.Y = y;

                mTopRightSprite.X = mTopSprite.X + mTopSprite.Width;
                mTopRightSprite.Y = y;

                y = mTopLeftSprite.Y + mTopLeftSprite.EffectiveHeight;

                mLeftSprite.X = this.GetAbsoluteX();
                mLeftSprite.Y = y;

                mCenterSprite.X = mLeftSprite.X + mLeftSprite.EffectiveWidth;
                mCenterSprite.Y = y;

                mRightSprite.X = mCenterSprite.X + mCenterSprite.Width;
                mRightSprite.Y = y;

                y = mLeftSprite.Y + mLeftSprite.Height;

                mBottomLeftSprite.X = this.GetAbsoluteX();
                mBottomLeftSprite.Y = y;

                mBottomSprite.X = mBottomLeftSprite.X + mBottomLeftSprite.EffectiveWidth;
                mBottomSprite.Y = y;

                mBottomRightSprite.X = mBottomSprite.X + mBottomSprite.Width;
                mBottomRightSprite.Y = y;

                Render(mTopLeftSprite, managers, spriteBatch);
                if (this.mCenterSprite.Width > 0)
                {
                    Render(mTopSprite, managers, spriteBatch);
                    Render(mBottomSprite, managers, spriteBatch);

                    if (this.mCenterSprite.Height > 0)
                    {
                        Render(mCenterSprite, managers, spriteBatch);
                    }

                }
                if (this.mCenterSprite.Height > 0)
                {
                    Render(mLeftSprite, managers, spriteBatch);
                    Render(mRightSprite, managers, spriteBatch);
                }


                Render(mTopRightSprite, managers, spriteBatch);
                Render(mBottomLeftSprite, managers, spriteBatch);
                Render(mBottomRightSprite, managers, spriteBatch);
            }
        }

        private void RefreshSpriteDimensions()
        {

            bool usesMulti = mTopLeftSprite.Texture != mTopSprite.Texture;

            float desiredMiddleWidth = 0;
            float desiredMiddleHeight = 0;


            if (usesMulti == false)
            {
                float fullWidth = mFullOutsideWidth * 2 + mFullInsideWidth;
                if (Width >= fullWidth)
                {
                    desiredMiddleWidth = this.Width - mTopLeftSprite.EffectiveWidth - mTopRightSprite.EffectiveWidth;

                    mTopLeftSprite.Width = mTopRightSprite.Width = mLeftSprite.Width = mRightSprite.Width =
                        mBottomLeftSprite.Width = mBottomRightSprite.Width = mFullOutsideWidth;
                }
                else if (Width >= mFullOutsideWidth * 2)
                {
                    desiredMiddleWidth = this.Width - mFullOutsideWidth * 2;

                    mTopLeftSprite.Width = mTopRightSprite.Width = mLeftSprite.Width = mRightSprite.Width =
                         mBottomLeftSprite.Width = mBottomRightSprite.Width = mFullOutsideWidth;
                }
                else
                {
                    desiredMiddleWidth = 0;
                    mTopLeftSprite.Width = mTopRightSprite.Width = mLeftSprite.Width = mRightSprite.Width =
                        mBottomLeftSprite.Width = mBottomRightSprite.Width = Width / 2.0f;
                }


                float fullHeight = mFullOutsideHeight * 2 + mFullInsideHeight;
                if (Height >= fullHeight)
                {
                    desiredMiddleHeight = this.Height - mTopLeftSprite.EffectiveHeight - mTopRightSprite.EffectiveHeight;
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
                desiredMiddleWidth = this.Width - mTopLeftSprite.EffectiveWidth - mTopRightSprite.EffectiveWidth;
                desiredMiddleHeight = this.Height - this.mTopLeftSprite.EffectiveHeight - this.mBottomLeftSprite.EffectiveHeight;
            }

            this.mTopSprite.Width = desiredMiddleWidth;
            this.mCenterSprite.Width = desiredMiddleWidth;
            this.mBottomSprite.Width = desiredMiddleWidth;

            this.mLeftSprite.Height = desiredMiddleHeight;
            this.mCenterSprite.Height = desiredMiddleHeight;
            this.mRightSprite.Height = desiredMiddleHeight;
        }

        private void RefreshSourceRectangles()
        {
            bool useMulti = mTopLeftSprite.Texture != mTopSprite.Texture;

            if (useMulti)
            {
                if (mTopLeftSprite.Texture == null)
                {
                    mTopLeftSprite.SourceRectangle = null;
                    mTopSprite.SourceRectangle = null;
                    mTopRightSprite.SourceRectangle = null;

                    mLeftSprite.SourceRectangle = null;
                    mCenterSprite.SourceRectangle = null;
                    mRightSprite.SourceRectangle = null;

                    mBottomLeftSprite.SourceRectangle = null;
                    mBottomSprite.SourceRectangle = null;
                    mBottomRightSprite.SourceRectangle = null;
                }
                else
                {
                    mFullOutsideWidth = mTopLeftSprite.Texture.Width;
                    mFullInsideWidth = mTopLeftSprite.Texture.Width - (mFullOutsideWidth * 2);

                    mTopLeftSprite.SourceRectangle = new Rectangle(0, 0, mTopLeftSprite.Texture.Width, mTopLeftSprite.Texture.Height);
                    mTopSprite.SourceRectangle = new Rectangle(0, 0, mTopSprite.Texture.Width, mTopSprite.Texture.Height);
                    mTopRightSprite.SourceRectangle = new Rectangle(0, 0, mTopRightSprite.Texture.Width, mTopRightSprite.Texture.Height);

                    mLeftSprite.SourceRectangle = new Rectangle(0, 0, mLeftSprite.Texture.Width, mLeftSprite.Texture.Height);
                    mCenterSprite.SourceRectangle = new Rectangle(0, 0, mCenterSprite.Texture.Width, mCenterSprite.Texture.Height);
                    mRightSprite.SourceRectangle = new Rectangle(0, 0, mRightSprite.Texture.Width, mRightSprite.Texture.Height);

                    mBottomLeftSprite.SourceRectangle = new Rectangle(0, 0, mBottomLeftSprite.Texture.Width, mBottomLeftSprite.Texture.Height);
                    mBottomSprite.SourceRectangle = new Rectangle(0, 0, mBottomSprite.Texture.Width, mBottomSprite.Texture.Height);
                    mBottomRightSprite.SourceRectangle = new Rectangle(0, 0, mBottomRightSprite.Texture.Width, mBottomRightSprite.Texture.Height);
                }
            }
            else if(mTopLeftSprite.Texture != null)
            {
                var texture = mTopLeftSprite.Texture;

                int leftCoordinate = 0;
                int rightCoordinate = texture.Width;
                int topCoordinate = 0;
                int bottomCoordinate = texture.Height;

                if(SourceRectangle.HasValue)
                {
                    leftCoordinate = SourceRectangle.Value.Left;
                    rightCoordinate = SourceRectangle.Value.Right;
                    topCoordinate = SourceRectangle.Value.Top;
                    bottomCoordinate = SourceRectangle.Value.Bottom;
                }

                int usedWidth = rightCoordinate - leftCoordinate;
                int usedHeight = bottomCoordinate - topCoordinate;

                mFullOutsideWidth = (usedWidth + 1) / 3;
                mFullInsideWidth = usedWidth - (mFullOutsideWidth * 2);

                mFullOutsideHeight = (usedHeight + 1) / 3;
                mFullInsideHeight = usedHeight - (mFullOutsideHeight * 2);

                int outsideWidth = System.Math.Min(mFullOutsideWidth, RenderingLibrary.Math.MathFunctions.RoundToInt( this.Width / 2)); ;
                int outsideHeight = System.Math.Min(mFullOutsideHeight, RenderingLibrary.Math.MathFunctions.RoundToInt(this.Height / 2));
                int insideWidth = mFullInsideWidth;
                int insideHeight = mFullInsideHeight;

                


                mTopLeftSprite.SourceRectangle = new Rectangle(
                    leftCoordinate + 0,
                    topCoordinate + 0,
                    outsideWidth,
                    outsideHeight);
                mTopSprite.SourceRectangle = new Rectangle(
                    leftCoordinate + outsideWidth,
                    topCoordinate + 0,
                    insideWidth,
                    outsideHeight);
                mTopRightSprite.SourceRectangle = new Rectangle(
                    leftCoordinate + insideWidth + outsideWidth,
                    topCoordinate + 0,
                    outsideWidth,
                    outsideHeight);

                mLeftSprite.SourceRectangle = new Rectangle(
                    leftCoordinate + 0,
                    topCoordinate + outsideHeight,
                    outsideWidth,
                    insideHeight);
                mCenterSprite.SourceRectangle = new Rectangle(
                    leftCoordinate + outsideWidth,
                    topCoordinate + outsideHeight,
                    insideWidth,
                    insideHeight);
                mRightSprite.SourceRectangle = new Rectangle(
                    leftCoordinate + outsideWidth + insideWidth,
                    topCoordinate + outsideHeight,
                    outsideWidth,
                    insideHeight);

                mBottomLeftSprite.SourceRectangle = new Rectangle(
                    leftCoordinate + 0,
                    topCoordinate + outsideHeight + insideHeight,
                    outsideWidth,
                    outsideHeight);
                mBottomSprite.SourceRectangle = new Rectangle(
                    leftCoordinate + outsideWidth,
                    topCoordinate + outsideHeight + insideHeight,
                    insideWidth,
                    outsideHeight);
                mBottomRightSprite.SourceRectangle = new Rectangle(
                    leftCoordinate + outsideWidth + insideWidth,
                    topCoordinate + outsideHeight + insideHeight,
                    outsideWidth,
                    outsideHeight);
            }

            if(mTopSprite.SourceRectangle.HasValue)
            {
                mTopSprite.Height = mTopSprite.SourceRectangle.Value.Height;
                mTopLeftSprite.Height = mTopSprite.Height;
                mTopRightSprite.Height = mTopSprite.Height;

            }
            if (mBottomSprite.SourceRectangle.HasValue)
            {
                mBottomSprite.Height = mBottomSprite.SourceRectangle.Value.Height;
                mBottomRightSprite.Height = mBottomSprite.Height;
                mBottomLeftSprite.Height = mBottomSprite.Height;
            }

            if (mLeftSprite.SourceRectangle.HasValue)
            {
                mLeftSprite.Width = mLeftSprite.SourceRectangle.Value.Width;
                mTopLeftSprite.Width = mLeftSprite.Width;
                mBottomLeftSprite.Width = mLeftSprite.Width;
            }

            if(mRightSprite.SourceRectangle.HasValue)
            {
                mRightSprite.Width = mRightSprite.SourceRectangle.Value.Width;
                mTopRightSprite.Width = mRightSprite.Width;
                mBottomRightSprite.Width = mRightSprite.Width;
            }
        }

        void Render(Sprite sprite, SystemManagers managers, SpriteBatch spriteBatch)
        {
            Sprite.Render(managers, spriteBatch, sprite, sprite.Texture, sprite.Color, 
                sprite.SourceRectangle, sprite.FlipHorizontal, sprite.FlipVertical, sprite.Rotation, treat0AsFullDimensions:false);

        }

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

        static NineSlice()
        {
            PossibleNineSliceEndings = new Dictionary<NineSliceSections, string>()
            {
                {NineSliceSections.Center, "_center"},
                {NineSliceSections.Left, "_left"},
                {NineSliceSections.Right, "_right"},
                {NineSliceSections.TopLeft, "_topLeft"},
                {NineSliceSections.Top, "_topCenter"},
                {NineSliceSections.TopRight, "_topRight"},
                {NineSliceSections.BottomLeft, "_bottomLeft"},
                {NineSliceSections.Bottom, "_bottomCenter"},
                {NineSliceSections.BottomRight, "_bottomRight"}
            };

        }

        public NineSlice()
        {
            Visible = true;
        }

        public void SetSingleTexture(Texture2D texture)
        {
            TopLeftTexture = texture;
            TopTexture = texture;
            TopRightTexture = texture;

            LeftTexture = texture;
            CenterTexture = texture;
            RightTexture = texture;

            BottomLeftTexture = texture;
            BottomTexture = texture;
            BottomRightTexture = texture;
        }

        public void SetTexturesUsingPattern(string anyOf9Textures, SystemManagers managers)
        {

            string absoluteTexture = anyOf9Textures;

            if(FileManager.IsRelative(absoluteTexture))
            {
                absoluteTexture = FileManager.RelativeDirectory + absoluteTexture;

                absoluteTexture = FileManager.RemoveDotDotSlash(absoluteTexture);
            }

            string extension = FileManager.GetExtension(absoluteTexture);

            string bareTexture = GetBareTextureForNineSliceTexture(absoluteTexture);
            string error;
            if (!string.IsNullOrEmpty(bareTexture))
            {
                TopLeftTexture = LoaderManager.Self.LoadOrInvalid(
                    bareTexture + NineSlice.PossibleNineSliceEndings[NineSliceSections.TopLeft] + "." + extension, managers, out error);
                TopTexture = LoaderManager.Self.LoadOrInvalid(
                    bareTexture + NineSlice.PossibleNineSliceEndings[NineSliceSections.Top] + "." + extension, managers, out error);
                TopRightTexture = LoaderManager.Self.LoadOrInvalid(
                    bareTexture + NineSlice.PossibleNineSliceEndings[NineSliceSections.TopRight] + "." + extension, managers, out error);

                LeftTexture = LoaderManager.Self.LoadOrInvalid(
                    bareTexture + NineSlice.PossibleNineSliceEndings[NineSliceSections.Left] + "." + extension, managers, out error);
                CenterTexture = LoaderManager.Self.LoadOrInvalid(
                    bareTexture + NineSlice.PossibleNineSliceEndings[NineSliceSections.Center] + "." + extension, managers, out error);
                RightTexture = LoaderManager.Self.LoadOrInvalid(
                    bareTexture + NineSlice.PossibleNineSliceEndings[NineSliceSections.Right] + "." + extension, managers, out error);

                BottomLeftTexture = LoaderManager.Self.LoadOrInvalid(
                    bareTexture + NineSlice.PossibleNineSliceEndings[NineSliceSections.BottomLeft] + "." + extension, managers, out error);
                BottomTexture = LoaderManager.Self.LoadOrInvalid(
                    bareTexture + NineSlice.PossibleNineSliceEndings[NineSliceSections.Bottom] + "." + extension, managers, out error);
                BottomRightTexture = LoaderManager.Self.LoadOrInvalid(
                    bareTexture + NineSlice.PossibleNineSliceEndings[NineSliceSections.BottomRight] + "." + extension, managers, out error);
            }


        }


        public static bool GetIfShouldUsePattern(string absoluteTexture)
        {
            bool usePattern = false;

            string withoutExtension = FileManager.RemoveExtension(absoluteTexture);
            foreach (var kvp in NineSlice.PossibleNineSliceEndings)
            {
                if (withoutExtension.EndsWith(kvp.Value, StringComparison.OrdinalIgnoreCase))
                {
                    usePattern = true;
                    break;
                }
            }
            return usePattern;
        }

        
        public static string GetBareTextureForNineSliceTexture(string absoluteTexture)
        {
            string extension = FileManager.GetExtension(absoluteTexture);

            string withoutExtension = FileManager.RemoveExtension(absoluteTexture);

            string toReturn = withoutExtension;

            foreach (var kvp in NineSlice.PossibleNineSliceEndings)
            {
                if (withoutExtension.EndsWith(kvp.Value, StringComparison.OrdinalIgnoreCase))
                {
                    toReturn = withoutExtension.Substring(0, withoutExtension.Length - kvp.Value.Length);
                    break;
                }
            }

            // No extensions, because we'll need to append that
            //toReturn += "." + extension;

            return toReturn;
        }

        #endregion


    }

}
