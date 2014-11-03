using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RenderingLibrary.Content;
using Microsoft.Xna.Framework;
using RenderingLibrary.Math.Geometry;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.ObjectModel;

namespace RenderingLibrary.Graphics
{
    #region HorizontalAlignment Enum

    public enum HorizontalAlignment
    {
        Left,
        Center,
        Right
    }

    #endregion

    #region VerticalAlignment Enum

    public enum VerticalAlignment
    {
        Top,
        Center,
        Bottom
    }

    #endregion

    public class Text : IPositionedSizedObject, IRenderable, IVisible
    {
        #region Fields

        /// <summary>
        /// Stores the width of the text object's texture before it has had a chance to render.
        /// </summary>
        /// <remarks>
        /// A Texture can have a width that depends on its texture.  However, its texture will not 
        /// be rendered created until the entire engine is rendered.  This will be used until that render
        /// occurs if not null.
        /// </remarks>
        int? mPreRenderWidth;
        /// <summary>
        /// Stores the height of the text object's texture before it has had a chance to render.
        /// </summary>
        /// <remarks>
        /// See mPreRenderWidth for more information about this member.
        /// </remarks>
        int? mPreRenderHeight;

        public Vector2 Position;

        public Color Color = Color.White;

        string mRawText;
        List<string> mWrappedText = new List<string>();
        float mWidth = 200;
        float mHeight = 200;
        LinePrimitive mBounds;

        BitmapFont mBitmapFont;
        Texture2D mTextureToRender;

        IPositionedSizedObject mParent;

        List<IPositionedSizedObject> mChildren;

        int mAlpha = 255;
        int mRed = 255;
        int mGreen = 255;
        int mBlue = 255;

        float mFontScale = 1;

        public bool mIsTextureCreationSuppressed;

        SystemManagers mManagers;

        bool mNeedsBitmapFontRefresh = false;

        #endregion

        #region Properties

        public static bool RenderBoundaryDefault
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public string RawText
        {
            get
            {
                return mRawText;
            }
            set
            {
                mRawText = value;
                UpdateWrappedText();
                mNeedsBitmapFontRefresh = true;

                //UpdateTextureToRender();
            }
        }

        public List<string> WrappedText
        {
            get
            {
                return mWrappedText;
            }
        }

        public float X
        {
            get
            {
                return Position.X;
            }
            set
            {
                Position.X = value;
            }
        }

        public float Y
        {
            get
            {
                return Position.Y;
            }
            set
            {
                Position.Y = value;
            }
        }

        public float Rotation { get; set; }

        public float Width
        {
            get
            {
                return mWidth;
            }
            set
            {
                mWidth = value;
                UpdateWrappedText();
                UpdateLinePrimitive();
            }
        }

        public float Height
        {
            get
            {
                return mHeight;
            }
            set
            {
                mHeight = value;
                UpdateLinePrimitive();
            }
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
                else if (mTextureToRender != null)
                {
                    if (mTextureToRender.Width == 0)
                    {
                        return 10;
                    }
                    else
                    {
                        return mTextureToRender.Width * mFontScale;
                    }
                }
                else if (mPreRenderWidth.HasValue)
                {
                    return mPreRenderWidth.Value;
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
                else if (mTextureToRender != null)
                {
                    if (mTextureToRender.Height == 0)
                    {
                        return 10;
                    }
                    else
                    {
                        return mTextureToRender.Height * mFontScale;
                    }
                }
                else if (mPreRenderHeight.HasValue)
                {
                    return mPreRenderHeight.Value;
                }
                else
                {
                    return 32;
                }
            }
        }

        public HorizontalAlignment HorizontalAlignment
        {
            get;
            set;
        }

        public VerticalAlignment VerticalAlignment
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

        public float Z
        {
            get;
            set;
        }

        public BitmapFont BitmapFont
        {
            get
            {
                return mBitmapFont;
            }
            set
            {
                mBitmapFont = value;

                UpdateWrappedText();

                mNeedsBitmapFontRefresh = true;
                //UpdateTextureToRender();
            }
        }

        public List<IPositionedSizedObject> Children
        {
            get { return mChildren; }
        }

        public int Alpha
        {
            get { return mAlpha; }
            set { mAlpha = value; }
        }

        public int Red
        {
            get { return mRed; }
            set { mRed = value; }
        }

        public int Green
        {
            get { return mGreen; }
            set { mGreen = value; }
        }

        public int Blue
        {
            get { return mBlue; }
            set { mBlue = value; }
        }

        public float FontScale
        {
            get { return mFontScale; }
            set
            {
                mFontScale = System.Math.Max(0, value);
                UpdateWrappedText();
                mNeedsBitmapFontRefresh = true;
            }
        }

        public object Tag { get; set; }

        public BlendState BlendState
        {
            get { return BlendState.NonPremultiplied; }
        }

        Renderer Renderer
        {
            get
            {
                if (mManagers == null)
                {
                    return Renderer.Self;
                }
                else
                {
                    return mManagers.Renderer;
                }
            }
        }

        public bool RenderBoundary
        {
            get;
            set;
        }

        public bool Wrap
        {
            get { return false; }
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

        #endregion

        #region Methods

        static Text()
        {
            RenderBoundaryDefault = true;
        }

        public Text(SystemManagers managers, string text = "Hello")
        {
            Visible = true;
            RenderBoundary = RenderBoundaryDefault;

            mManagers = managers;
            mChildren = new List<IPositionedSizedObject>();

            mRawText = text;
            mNeedsBitmapFontRefresh = true;
            mBounds = new LinePrimitive(this.Renderer.SinglePixelTexture);
            mBounds.Color = Color.LightGreen;

            mBounds.Add(0, 0);
            mBounds.Add(0, 0);
            mBounds.Add(0, 0);
            mBounds.Add(0, 0);
            mBounds.Add(0, 0);
            HorizontalAlignment = Graphics.HorizontalAlignment.Left;
            VerticalAlignment = Graphics.VerticalAlignment.Top;

#if !TEST
            if (LoaderManager.Self.DefaultBitmapFont != null)
            {
                this.BitmapFont = LoaderManager.Self.DefaultBitmapFont;
            }
#endif
            UpdateLinePrimitive();
        }

        char[] whatToSplitOn = new char[] { ' ' };
        private void UpdateWrappedText()
        {
            ///////////EARLY OUT/////////////
            if (this.BitmapFont == null)
            {
                return;
            }
            /////////END EARLY OUT///////////

            mWrappedText.Clear();

            float wrappingWidth = mWidth / mFontScale;
            if (mWidth == 0)
            {
                wrappingWidth = float.PositiveInfinity;
            }

            // This allocates like crazy but we're
            // on the PC and prob won't be calling this
            // very frequently so let's 
            String line = String.Empty;
            String returnString = String.Empty;

            // The user may have entered "\n" in the string, which would 
            // be written as "\\n".  Let's replace that, shall we?
            string stringToUse = null;
            List<string> wordArray = new List<string>();

            if (mRawText != null)
            {
                stringToUse = mRawText.Replace("\\n", "\n");
                wordArray.AddRange(stringToUse.Split(whatToSplitOn));
            }


            while (wordArray.Count != 0)
            {
                string wordUnmodified = wordArray[0];

                string word = wordUnmodified;

                bool containsNewline = false;

                if (word.Contains('\n'))
                {
                    word = word.Substring(0, word.IndexOf('\n'));
                    containsNewline = true;
                }

                string whatToMeasure = line + word;

                float lineWidth = MeasureString(whatToMeasure);

                if (lineWidth > wrappingWidth)
                {
                    while (line.EndsWith(" "))
                    {
                        line = line.Substring(0, line.Length - 1);
                    }
                    if (!string.IsNullOrEmpty(line))
                    {
                        mWrappedText.Add(line);
                    }

                    //returnString = returnString + line + '\n';
                    line = String.Empty;
                }

                // If it's the first word and it's empty, don't add anything
                if (!string.IsNullOrEmpty(word) || !string.IsNullOrEmpty(line))
                {
                    line = line + word + ' ';
                }

                wordArray.RemoveAt(0);

                if (containsNewline)
                {
                    mWrappedText.Add(line);
                    line = string.Empty;
                    int indexOfNewline = wordUnmodified.IndexOf('\n');
                    wordArray.Insert(0, wordUnmodified.Substring(indexOfNewline + 1, wordUnmodified.Length - (indexOfNewline + 1)));
                }
            }
            while (line.EndsWith(" "))
            {
                line = line.Substring(0, line.Length - 1);
            }
            mWrappedText.Add(line);


            //if (mManagers == null || mManagers.IsCurrentThreadPrimary)
            //{
            //    UpdateTextureToRender();
            //}
            //else
            {
                mNeedsBitmapFontRefresh = true;
            }
        }

        private float MeasureString(string whatToMeasure)
        {
            if (this.BitmapFont != null)
            {
                return BitmapFont.MeasureString(whatToMeasure);
            }
            else if (LoaderManager.Self.DefaultBitmapFont != null)
            {
                return LoaderManager.Self.DefaultBitmapFont.MeasureString(whatToMeasure);
            }
            else
            {
#if TEST
                return 0;
#else
                float wordWidth = LoaderManager.Self.DefaultFont.MeasureString(whatToMeasure).X;
                return wordWidth;
#endif
            }
        }

        // made public so that objects that need to position based off of the texture can force call this
        public void TryUpdateTextureToRender()
        {
            if (mNeedsBitmapFontRefresh)
            {
                UpdateTextureToRender();
            }
        }

        static int reuse = 0;
        static int isNew = 0;

        public void UpdateTextureToRender()
        {
            if (!mIsTextureCreationSuppressed)
            {
                BitmapFont fontToUse = mBitmapFont;
                if (mBitmapFont == null)
                {
                    fontToUse = LoaderManager.Self.DefaultBitmapFont;
                }


                if (fontToUse != null)
                {
                    //if (mTextureToRender != null)
                    //{
                    //    mTextureToRender.Dispose();
                    //    mTextureToRender = null;
                    //}

                    var returnedRenderTarget = fontToUse.RenderToTexture2D(WrappedText, this.HorizontalAlignment, mManagers, mTextureToRender);
                    bool isNewInstance = returnedRenderTarget != mTextureToRender;

                    if (isNewInstance)
                    {
                        isNew++;

                    }
                    else
                    {
                        reuse++;
                    }

                    Console.WriteLine("New: " + isNew + ", Reuse:" + reuse);

                    if (isNewInstance && mTextureToRender != null)
                    {
                        mTextureToRender.Dispose();

                        if (mTextureToRender is RenderTarget2D)
                        {
                            (mTextureToRender as RenderTarget2D).ContentLost -= SetNeedsRefresh;
                        }
                        mTextureToRender = null;
                    }
                    mTextureToRender = returnedRenderTarget;

                    if (isNewInstance && mTextureToRender is RenderTarget2D)
                    {
                        (mTextureToRender as RenderTarget2D).ContentLost += SetNeedsRefresh;
                    }
                }
                else if (mBitmapFont == null)
                {
                    if (mTextureToRender != null)
                    {
                        mTextureToRender.Dispose();
                        mTextureToRender = null;
                    }
                }

                mPreRenderWidth = null;
                mPreRenderHeight = null;

                mNeedsBitmapFontRefresh = false;
            }
        }

        void SetNeedsRefresh(object sender, EventArgs args)
        {
            mNeedsBitmapFontRefresh = true;
        }

        void UpdateLinePrimitive()
        {
            LineRectangle.UpdateLinePrimitive(mBounds, this);

        }


        public void Render(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, SystemManagers managers)
        {
            if (AbsoluteVisible)
            {
                // Moved this out of here - it's manually called by the TextManager
                // This is required because we can't update in the draw call now that
                // we're using RenderTargets
                //if (mNeedsBitmapFontRefresh)
                //{
                //    UpdateTextureToRender();
                //}
                if (RenderBoundary)
                {
                    LineRectangle.RenderLinePrimitive(mBounds, spriteBatch, this, managers, false);
                }

                if (mTextureToRender == null)
                {
                    RenderUsingSpriteFont(spriteBatch);
                }
                else
                {
                    RenderUsingBitmapFont(spriteBatch, managers);
                }
            }
        }

        private void RenderUsingBitmapFont(SpriteBatch spriteBatch, SystemManagers managers)
        {
            if (mTempForRendering == null)
            {
                mTempForRendering = new LineRectangle(managers);
            }

            mTempForRendering.X = this.X;
            mTempForRendering.Y = this.Y;
            mTempForRendering.Width = this.mTextureToRender.Width * mFontScale;
            mTempForRendering.Height = this.mTextureToRender.Height * mFontScale;
            mTempForRendering.Parent = this.Parent;

            float widthDifference = this.EffectiveWidth - mTempForRendering.Width;

            if (this.HorizontalAlignment == Graphics.HorizontalAlignment.Center)
            {
                mTempForRendering.X += widthDifference / 2.0f;
            }
            else if (this.HorizontalAlignment == Graphics.HorizontalAlignment.Right)
            {
                mTempForRendering.X += widthDifference;
            }

            if (this.VerticalAlignment == Graphics.VerticalAlignment.Center)
            {
                mTempForRendering.Y += (this.EffectiveHeight - mTextureToRender.Height) / 2.0f;
            }
            else if (this.VerticalAlignment == Graphics.VerticalAlignment.Bottom)
            {
                mTempForRendering.Y += this.EffectiveHeight - mTempForRendering.Height;
            }

            Sprite.Render(managers, spriteBatch, mTempForRendering, mTextureToRender,
                new Color(mRed, mGreen, mBlue, mAlpha), null, false, false, Rotation, treat0AsFullDimensions: false);
        }

        IPositionedSizedObject mTempForRendering;

        private void RenderUsingSpriteFont(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
        {

            Vector2 offset = new Vector2(this.Renderer.Camera.RenderingXOffset, Renderer.Camera.RenderingYOffset);

            float leftSide = offset.X + this.GetAbsoluteX();
            float topSide = offset.Y + this.GetAbsoluteY();

            SpriteFont font = LoaderManager.Self.DefaultFont;
            // Maybe this hasn't been loaded yet?
            if (font != null)
            {
                switch (this.VerticalAlignment)
                {
                    case Graphics.VerticalAlignment.Top:
                        offset.Y = topSide;
                        break;
                    case Graphics.VerticalAlignment.Bottom:
                        {
                            float requiredHeight = (this.mWrappedText.Count) * font.LineSpacing;

                            offset.Y = topSide + (this.Height - requiredHeight);

                            break;
                        }
                    case Graphics.VerticalAlignment.Center:
                        {
                            float requiredHeight = (this.mWrappedText.Count) * font.LineSpacing;

                            offset.Y = topSide + (this.Height - requiredHeight) / 2.0f;
                            break;
                        }
                }



                float offsetY = offset.Y;

                for (int i = 0; i < mWrappedText.Count; i++)
                {
                    offset.X = leftSide;
                    offset.Y = (int)offsetY;

                    string line = mWrappedText[i];

                    if (HorizontalAlignment == Graphics.HorizontalAlignment.Right)
                    {
                        offset.X = leftSide + (Width - font.MeasureString(line).X);
                    }
                    else if (HorizontalAlignment == Graphics.HorizontalAlignment.Center)
                    {
                        offset.X = leftSide + (Width - font.MeasureString(line).X) / 2.0f;
                    }

                    offset.X = (int)offset.X; // so we don't have half-pixels that render weird

                    spriteBatch.DrawString(font, line, offset, Color);
                    offsetY += LoaderManager.Self.DefaultFont.LineSpacing;
                }
            }
        }

        public override string ToString()
        {
            return this.Name;
        }

        public void SuppressTextureCreation()
        {
            mIsTextureCreationSuppressed = true;
        }

        public void EnableTextureCreation()
        {
            mIsTextureCreationSuppressed = false;
            mNeedsBitmapFontRefresh = true;
            //UpdateTextureToRender();
        }

        public void SetNeedsRefreshToTrue()
        {
            mNeedsBitmapFontRefresh = true;
        }

        public void UpdatePreRenderDimensions()
        {

            if (this.mBitmapFont != null)
            {
                int requiredWidth;
                int requiredHeight;

                string[] lines = this.mRawText.Split('\n');

                mBitmapFont.GetRequiredWithAndHeight(lines, out requiredWidth, out requiredHeight);

                mPreRenderWidth = requiredWidth;
                mPreRenderHeight = requiredHeight;
            }
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
