using System;
using System.Collections.Generic;
using RenderingLibrary.Content;
using RenderingLibrary.Math.Geometry;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.ObjectModel;
using BlendState = Gum.BlendState;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Color = System.Drawing.Color;
using Matrix = System.Numerics.Matrix4x4;
using System.Linq;
using ToolsUtilitiesStandard.Helpers;
using System.Drawing;
using System.Text;
using RenderingLibrary.Math;
using Gum.Graphics;

namespace RenderingLibrary.Graphics;

#region TextRenderingMode Enum

public enum TextRenderingMode
{
    RenderTarget,
    CharacterByCharacter
}

#endregion

#region TextRenderingPositionMode

public enum TextRenderingPositionMode
{
    SnapToPixel,
    FreeFloating
}

#endregion

#region InlineVariable

public class InlineVariable
{
    /// <summary>
    /// Variable name, such as "Font". This translates to the left-side of the assignment in the tag. For example
    /// [Font=Arial] would have a VariableName of "Font".
    /// </summary>
    public string VariableName;

    /// <summary>
    /// The start index of the tag in the "stripped" text (after all tags have been removed).
    /// </summary>
    public int StartIndex;

    /// <summary>
    /// The number of characters covered by this inline variable. This is the character count on the "stripped" text.
    /// </summary>
    public int CharacterCount;
    public object Value;

    public override string ToString()
    {
        return $"{VariableName} = {Value} at [{StartIndex}] for {CharacterCount} characters";
    }
}

#endregion


#region LetterCustomization
public struct LetterCustomization
{
    public float XOffset;
    public float YOffset;
    public Color? Color;
    public float ScaleX;
    public HorizontalAlignment ScaleXOrigin = HorizontalAlignment.Center;
    public float ScaleY;
    public VerticalAlignment ScaleYOrigin = VerticalAlignment.Center;
    public float RotationDegrees;
    public char? ReplacementCharacter;

    public LetterCustomization()
    {
        ScaleX = 1;
        ScaleY = 1;
    }
}
#endregion

#region ParameterizedLetterCustomizationCall

public class ParameterizedLetterCustomizationCall
{
    public string FunctionName { get; set; } = string.Empty;
    public Func<int, string, LetterCustomization>? Function
    {
        get
        {
            if(!string.IsNullOrEmpty(FunctionName) && Text.Customizations.TryGetValue(FunctionName, out var func))
            {
                return func;
            }
            return null;
        }
    }

    public int CharacterIndex { get; set; }

    public string TextBlock { get; set; }
}

#endregion

public class Text : SpriteBatchRenderableBase, IRenderableIpso, IVisible, IWrappedText, ICloneable
{
    #region Fields

    public static SpriteFont DefaultFont
    {
        get;
        set;
    }

    /// <summary>
    /// The default BitmapFont to use if a Text instance is referencing a null font.
    /// </summary>
    public static BitmapFont DefaultBitmapFont
    {
        get;
        set;
    }

    /// <summary>
    /// Stores the width of the text object's texture before it has had a chance to render, not including
    /// the FontScale.
    /// </summary>
    /// <remarks>
    /// A text object may need to be positioned according to its dimensions. Normally this would
    /// use a text's render target texture. In some situations (before the render pass has occurred,
    /// or when using character-by-character rendering), the text may not have a render target texture.
    /// Therefore, the pre-rendered values provide size information.
    /// </remarks>
    int? mPreRenderWidth;
    /// <summary>
    /// Stores the height of the text object's texture before it has had a chance to render, not including
    /// the FontScale.
    /// </summary>
    /// <remarks>
    /// See mPreRenderWidth for more information about this member.
    /// </remarks>
    int? mPreRenderHeight;

    public Vector2 Position;

    public Color Color
    {
        get
        {
            return Color.FromArgb(mAlpha, mRed, mGreen, mBlue);
        }
        set
        {
            mRed = value.R;
            mGreen = value.G;
            mBlue = value.B;
            mAlpha = value.A;
        }
    }

    
    List<string> mWrappedText = new List<string>();
    float? mWidth = 200;
    float mHeight = 200;
    LinePrimitive mBounds;

    public List<InlineVariable> InlineVariables { get; private set; } = new List<InlineVariable>();

    BitmapFont mBitmapFont;
    Texture2D mTextureToRender;

    IRenderableIpso? mParent;

    ObservableCollectionNoReset<IRenderableIpso> mChildren;

    int mAlpha = 255;
    int mRed = 255;
    int mGreen = 255;
    int mBlue = 255;

    float mFontScale = 1;

    public bool mIsTextureCreationSuppressed;

    bool IWrappedText.IsMidWordLineBreakEnabled => IsMidWordLineBreakEnabled;

    // Defaulting to false, will be turned on in future version
    // Updating to true on August 3, 2025 for:
    // https://github.com/vchelaru/Gum/issues/1254
    // It's now turned on, and we have more unit tests to cover this.
    public static bool IsMidWordLineBreakEnabled = true;

    SystemManagers mManagers;

    bool mNeedsBitmapFontRefresh = true;

    // For now this is going to be app-wide, but...maybe we want to make this instance-based?  I'm not sure, but
    // I don't want to inflate each text object to support something that may not be used, so we'll start with a static.
    // It'll break code but it won't be hard to respond to.
    // As of 0.8.7, CharacterByCharacter is the standard in Gum tool
    public static TextRenderingMode TextRenderingMode = TextRenderingMode.CharacterByCharacter;

    public static TextRenderingPositionMode TextRenderingPositionMode = TextRenderingPositionMode.SnapToPixel;

    public TextRenderingPositionMode? OverrideTextRenderingPositionMode = null;

    #endregion

    #region Properties

    ColorOperation IRenderableIpso.ColorOperation => ColorOperation.Modulate;

    /// <summary>
    /// The width needed to display the wrapped text. 
    /// </summary>
    public float WrappedTextWidth
    {
        get
        {
            if (mPreRenderWidth != null)
            {
                return mPreRenderWidth.Value * mFontScale;
            }
            else if (mTextureToRender?.Width > 0)
            {
                return mTextureToRender.Width * mFontScale;
            }
            else
            {
                return 0;
            }
        }
    }

    public float WrappedTextHeight
    {
        get
        {
            if (mPreRenderHeight != null)
            {
                return mPreRenderHeight.Value * mFontScale;
            }
            else if (mTextureToRender?.Height > 0)
            {
                return mTextureToRender.Height * mFontScale;
            }
            else
            {
                return 0;
            }
        }
    }

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

    int? maxLettersToShow;
    /// <summary>
    /// The maximum letters to display. This can be used to 
    /// create an effect where the text prints out letter-by-letter.
    /// </summary>
    public int? MaxLettersToShow
    {
        get => maxLettersToShow;
        set
        {
            if (maxLettersToShow != value)
            {
                maxLettersToShow = value;

                mNeedsBitmapFontRefresh = true;
            }
        }
    }

    int? maxNumberOfLines;
    /// <summary>
    /// The maximum number of lines to display. This can be used to 
    /// limit how many lines of text are displayed at one time.
    /// </summary>
    public int? MaxNumberOfLines
    {
        get => maxNumberOfLines;
        set
        {
            if (maxNumberOfLines != value)
            {
                maxNumberOfLines = value;
                UpdateWrappedText();

                UpdatePreRenderDimensions();
            }
        }
    }

    public bool IsTruncatingWithEllipsisOnLastLine { get; set; }
        // temp:
        = true;

    string? mRawText;
    public string? RawText
    {
        get => mRawText;
        set
        {
            if (mRawText != value)
            {
                mRawText = value;
                UpdateWrappedText();

                UpdatePreRenderDimensions();
            }
        }
    }

    /// <summary>
    /// Stores the markup text including BBCode. This should not be
    /// directly set outside of custom property assignments since setting
    /// it directly does not update the RawText, WrappedText, or InlineVariables.
    /// This only exists to make it easier for the code that creates InlineVariables
    /// to use this.
    /// </summary>
    public string StoredMarkupText { get; set; }

    public List<string> WrappedText => mWrappedText;

    public float X
    {
        get => Position.X;
        set => Position.X = value;
    }

    public float Y
    {
        get => Position.Y;
        set => Position.Y = value;
    }

    public bool FlipHorizontal { get; set; }

    public float Rotation { get; set; }

    public float? Width
    {
        get
        {
            return mWidth;
        }
        set
        {
            if (mWidth != value)
            {
                mWidth = value;
                UpdateWrappedText();
                UpdateLinePrimitive();
                UpdatePreRenderDimensions();
            }

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
            if (mHeight != value)
            {
                mHeight = value;

                if (TextOverflowVerticalMode != TextOverflowVerticalMode.SpillOver)
                {
                    UpdateWrappedText();
                }

                UpdateLinePrimitive();

                if (TextOverflowVerticalMode != TextOverflowVerticalMode.SpillOver)
                {
                    UpdatePreRenderDimensions();
                }

            }
        }
    }

    public float EffectiveWidth
    {
        get
        {
            // I think we want to treat these individually so a 
            // width could be set but height could be default
            if (Width != null)
            {
                return Width.Value;
            }
            // If there is a prerendered width/height, then that means that
            // the width/height has updated but it hasn't yet made its way to the
            // texture. This could happen when the text already has a texture, so give
            // priority to the prerendered values as they may be more up-to-date.
            else if (mPreRenderWidth.HasValue)
            {
                return mPreRenderWidth.Value * mFontScale;
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
            else
            {
                // This causes problems when the text object has no text:
                //return 32;
                return 0;
            }
        }
    }

    public float EffectiveHeight
    {
        get
        {
            // December 2, 2024
            // Width now treats 0 width as a proper 0 width. Do we want to do the same for height? Not sure at this point...
            if (Height != 0)
            {
                return Height;
            }
            // See EffectiveWidth for an explanation of why the prerendered values need to come first
            else if (mPreRenderHeight.HasValue)
            {
                return mPreRenderHeight.Value * mFontScale;
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
            else
            {
                return 32;
            }
        }
    }


    bool IRenderableIpso.ClipsChildren => false;

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

    TextOverflowVerticalMode textOverflowVerticalMode;
    public TextOverflowVerticalMode TextOverflowVerticalMode
    {
        get => textOverflowVerticalMode;
        set
        {
            if (textOverflowVerticalMode != value)
            {
                textOverflowVerticalMode = value;
                UpdateWrappedText();
                UpdatePreRenderDimensions();
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
            if (mBitmapFont != value)
            {
                mBitmapFont = value;

                UpdateWrappedText();
                UpdatePreRenderDimensions();

                mNeedsBitmapFontRefresh = true;
            }
            //UpdateTextureToRender();
        }
    }

    public ObservableCollection<IRenderableIpso> Children
    {
        get { return mChildren; }
    }

    public int Alpha
    {
        get => mAlpha;
        set
        {
            mAlpha = System.Math.Max(0, System.Math.Min(value, 255));
        }
    }

    public int Red
    {
        get => mRed;
        set
        {

            mRed = System.Math.Max(0, System.Math.Min(value, 255));
        }
    }

    public int Green
    {
        get => mGreen;

        set
        {
            mGreen = System.Math.Max(0, System.Math.Min(value, 255));
        }
    }

    public int Blue
    {
        get => mBlue;
        set
        {
            mBlue = System.Math.Max(0, System.Math.Min(value, 255));
        }
    }

    public float FontScale
    {
        get { return mFontScale; }
        set
        {
#if FULL_DIAGNOSTICS
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentException($"Invalid value: {value}. FontScale cannot be NaN.");
            }
#endif

            var newValue = System.Math.Max(0, value);

            if (newValue != mFontScale)
            {
                mFontScale = newValue;
                UpdateWrappedText();
                mNeedsBitmapFontRefresh = true;
                UpdatePreRenderDimensions();
            }
        }
    }

    public object Tag { get; set; }

    public BlendState BlendState { get; set; }

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

    public bool Wrap =>false; 

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

    /// <summary>
    /// DescenderHeight in pixels as defined by the BitmapFont, ignoring FontScale.
    /// </summary>
    public float DescenderHeight => BitmapFont?.DescenderHeight ?? 0;

    /// <summary>
    /// Line height in pixels as defined by the BitmapFont, ignoring FontScale
    /// </summary>
    public int LineHeightInPixels => BitmapFont?.LineHeightInPixels ?? 32;

    public float LineHeightMultiplier { get; set; } = 1;

    bool IRenderableIpso.IsRenderTarget => false;

    /// <summary>
    /// Customization functions, where the key is the name of the custom function in bbcode, and the Func returns
    /// customization per letter.
    /// </summary>
    /// <example>
    /// LetterCustomization SineWave(int index, string textInBlock)
    /// {
    ///   // Index is the letter, it will get called for each character in the block, starting at 0
    ///   // textInBlock is the entire block, in case the function needs to reference it.
    ///   return new LetterCustomization
    ///   {
    ///     OffsetY = MathF.Sin(DateTime.Now.TotalSeconds + index/5);
    ///   };
    /// }
    /// // This would be used as follows:
    /// Text.Customizations["SineWave"] = SineWave;
    /// </example>
    public static Dictionary<string, Func<int, string, LetterCustomization>> Customizations { get; private set; }
        = new ();

    public OverlapDirection OverlapDirection { get; set; } = OverlapDirection.RightOnTop;

    #endregion

    #region Methods

    static Text()
    {
        RenderBoundaryDefault = true;
    }

    public Text()
    {
        Initialize(SystemManagers.Default, "Hello");
    }

    public Text(SystemManagers managers, string text = "Hello")
    {
        Initialize(managers, text);
    }

    private void Initialize(SystemManagers managers, string text)
    {
        Visible = true;
        RenderBoundary = RenderBoundaryDefault;

        mManagers = managers;
        mChildren = new ();

        mRawText = text;
        mNeedsBitmapFontRefresh = true;

        HorizontalAlignment = Graphics.HorizontalAlignment.Left;
        VerticalAlignment = Graphics.VerticalAlignment.Top;

        if (DefaultBitmapFont != null)
        {
            this.BitmapFont = DefaultBitmapFont;
        }

        UpdateLinePrimitive();
    }

    private void CreateBounds(SystemManagers managers)
    {
        mBounds = new LinePrimitive(managers.Renderer?.TryGetSinglePixelTexture());
        mBounds.Color = Color.LightGreen;

        mBounds.Add(0, 0);
        mBounds.Add(0, 0);
        mBounds.Add(0, 0);
        mBounds.Add(0, 0);
        mBounds.Add(0, 0);
    }

    char[] whatToSplitOn = new char[] { ' ' };

    static char[] preservedNewlinableCharacters = new char[] { ',', '-', ':', '.', '?', '!', '&', 
        // 
        ')' };
    private void UpdateWrappedText()
    {
        ///////////EARLY OUT/////////////
        if (this.BitmapFont == null && DefaultBitmapFont == null)
        {
            return;
        }

        mWrappedText.Clear();
        UpdateLines(mWrappedText);

    }

    const string ellipsis = "...";
    public void UpdateLines(List<string> lines)
    {
        ((IWrappedText)this).UpdateLines(lines);

        mNeedsBitmapFontRefresh = true;
    }

    /// <summary>
    /// Returns the size of the string, ignoring font scale, but considering the bitmap font.
    /// </summary>
    /// <param name="whatToMeasure"></param>
    /// <returns></returns>
    public float MeasureString(string whatToMeasure)
    {
        if (this.BitmapFont != null)
        {
            return BitmapFont.MeasureString(whatToMeasure);
        }
        else if (DefaultBitmapFont != null)
        {
            return DefaultBitmapFont.MeasureString(whatToMeasure);
        }
        else
        {
#if TEST
            return 0;
#else
            float wordWidth = DefaultFont.MeasureString(whatToMeasure).X;
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

    void IRenderable.PreRender()
    {
        TryUpdateTextureToRender();
    }

    public void UpdateTextureToRender()
    {
        if (!mIsTextureCreationSuppressed && TextRenderingMode == TextRenderingMode.RenderTarget)
        {
            BitmapFont fontToUse = mBitmapFont;
            if (mBitmapFont == null)
            {
                fontToUse = DefaultBitmapFont;
            }


            if (fontToUse != null)
            {
                //if (mTextureToRender != null)
                //{
                //    mTextureToRender.Dispose();
                //    mTextureToRender = null;
                //}

                var returnedRenderTarget = fontToUse.RenderToTexture2D(WrappedText, this.HorizontalAlignment,
                    mManagers, mTextureToRender, this, MaxLettersToShow);
                bool isNewInstance = returnedRenderTarget != mTextureToRender;

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
                    mTextureToRender.Name = "Render Target for Text " + this.Name;

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
        if(RenderBoundary)
        {
            if(mBounds == null)
            {
                CreateBounds(this.mManagers);
            }
            LineRectangle.UpdateLinePrimitive(mBounds, this);
        }

    }


    public override void Render(ISystemManagers managers)
    {
        // See NineSlice for explanation of this Visible check
        if (!string.IsNullOrEmpty(RawText))
        {
            var systemManagers = (SystemManagers)managers;
            var spriteRenderer = systemManagers.Renderer.SpriteRenderer;
            // Moved this out of here - it's manually called by the TextManager
            // This is required because we can't update in the draw call now that
            // we're using RenderTargets
            //if (mNeedsBitmapFontRefresh)
            //{
            //    UpdateTextureToRender();
            //}
            if (RenderBoundary)
            {
                LineRectangle.RenderLinePrimitive(mBounds, spriteRenderer, this, systemManagers, false);
            }

            if (TextRenderingMode == TextRenderingMode.CharacterByCharacter)
            {
                RenderCharacterByCharacter(spriteRenderer);
            }
            else // RenderTarget
            {
                if (mTextureToRender == null)
                {
                    RenderUsingSpriteFont(spriteRenderer);
                }
                else
                {
                    RenderUsingBitmapFont(spriteRenderer, systemManagers);
                }
            }
        }

    }

    // todo: reduce allocs by using a static here (static is prob okay since it can't be multithreaded)
    static List<int> widths = new List<int>();
    private void RenderCharacterByCharacter(SpriteRenderer spriteRenderer)
    {
        BitmapFont fontToUse = mBitmapFont;
        if (mBitmapFont == null)
        {
            fontToUse = DefaultBitmapFont;
        }


        if (fontToUse != null)
        {
            widths.Clear();
            int requiredWidth;
            fontToUse.GetRequiredWidthAndHeight(WrappedText, out requiredWidth, out int _, widths);
            UpdateIpsoForRendering();


            if (InlineVariables.Count > 0)
            {
                DrawWithInlineVariables(fontToUse, requiredWidth, spriteRenderer);
            }
            else
            {
                var absoluteLeft = mTempForRendering.GetAbsoluteLeft();
                var absoluteTop = mTempForRendering.GetAbsoluteTop();

                var sourceRectangle = new Rectangle(0, 0, 32, 32);

                if (fontToUse.Texture == null)
                {
                    if (Sprite.InvalidTexture != null)
                    {
                        spriteRenderer.Draw(Sprite.InvalidTexture,
                            new Rectangle((int)absoluteLeft, (int)absoluteTop, 16, 16),
                            sourceRectangle,
                            Color.White,
                            this);
                    }
                }
                else
                {
                    fontToUse.DrawTextLines(WrappedText, 
                        HorizontalAlignment,
                        this,
                        requiredWidth, widths, spriteRenderer, Color,
                        absoluteLeft,
                        absoluteTop,
                        this.GetAbsoluteRotation(), 
                        mFontScale, mFontScale, maxLettersToShow, 
                        OverrideTextRenderingPositionMode, lineHeightMultiplier: LineHeightMultiplier,
                        overlapDirection: OverlapDirection);
                }
            }

        }
    }

    List<string> lineByLineList = new List<string>() { "" };

    public class StyledSubstring
    {
        public List<InlineVariable> Variables = new List<InlineVariable>();
        public string Substring;
        public int StartIndex;

        public override string ToString()
        {
            var toReturn = Substring ?? "<null>";

            foreach (var variable in Variables)
            {
                toReturn += $" {variable.VariableName} = {variable.Value}";
            }
            return toReturn;
        }
    }

    // When drawing line-by-line, we only pass a single 
    List<int> individualLineWidth = new List<int>() { 0 };
    private void DrawWithInlineVariables(BitmapFont fontToUse, int requiredWidth, SpriteRenderer spriteRenderer)
    {
        var absoluteTop = mTempForRendering.GetAbsoluteTop();

        int startOfLineIndex = 0;

        var rotation = this.GetAbsoluteRotation();
        float topOfLine = absoluteTop;
        var lettersLeft = maxLettersToShow;
        for (int i = 0; i < WrappedText.Count; i++)
        {
            if (lettersLeft <= 0)
            {
                break;
            }
            var absoluteLeft = mTempForRendering.GetAbsoluteLeft();
            var lineOfText = WrappedText[i];

            var color = Color;

            var substrings = GetStyledSubstrings(startOfLineIndex, lineOfText, color);

            if (substrings.Count == 0)
            {
                lineByLineList[0] = lineOfText;
                fontToUse.DrawTextLines(lineByLineList, HorizontalAlignment,
                    this,
                    requiredWidth, widths, spriteRenderer, color,
                    absoluteLeft,
                    topOfLine,
                    this.GetAbsoluteRotation(), mFontScale, 
                    mFontScale, lettersLeft, OverrideTextRenderingPositionMode, lineHeightMultiplier: LineHeightMultiplier,
                    overlapDirection: OverlapDirection);

                topOfLine += fontToUse.EffectiveLineHeight(mFontScale, mFontScale);
                maxLettersToShow -= lineOfText.Length;
            }
            else
            {
                individualLineWidth[0] = widths[i];
                var lineHeight = fontToUse.EffectiveLineHeight(mFontScale, 1);
                var defaultBaseline = fontToUse.BaselineY;

                float currentFontScale = FontScale;
                float maxFontScale = 1;
                BitmapFont currentFont = fontToUse;

                float maxBaseline = currentFontScale * currentFont.BaselineY;

                foreach (var substring in substrings)
                {
                    for (int variableIndex = 0; variableIndex < substring.Variables.Count; variableIndex++)
                    {
                        var variable = substring.Variables[variableIndex];
                        if (variable.VariableName == nameof(FontScale))
                        {
                            currentFontScale = (float)variable.Value;
                            lineHeight = System.Math.Max(lineHeight, currentFont.EffectiveLineHeight(currentFontScale, 1));
                            maxFontScale = System.Math.Max(maxFontScale, currentFontScale);
                            maxBaseline = System.Math.Max(maxBaseline, currentFontScale * currentFont.BaselineY);
                        }
                        else if (variable.VariableName == nameof(BitmapFont))
                        {
                            currentFont = (BitmapFont)variable.Value;
                            lineHeight = System.Math.Max(lineHeight, currentFont.EffectiveLineHeight(currentFontScale, 1));
                            maxBaseline = System.Math.Max(maxBaseline, currentFontScale * currentFont.BaselineY);

                        }
                    }
                }

                for(int substringIndex = 0; substringIndex < substrings.Count; substringIndex++)
                {

                    if (lettersLeft <= 0)
                    {
                        break;
                    }

                    var substring = substrings[substringIndex];

                    lineByLineList[0] = substring.Substring;
                    color = Color;
                    var fontScale = mFontScale;
                    var effectiveFont = fontToUse;
                    var effectiveTopOfLine = topOfLine;
                    float yOffset = 0;
                    float xOffset = 0;
                    float scaleX = 1;
                    float scaleY = 1;
                    float rotationOffset = 0;
                    for (int variableIndex = 0; variableIndex < substring.Variables.Count; variableIndex++)
                    {
                        var variable = substring.Variables[variableIndex];
                        if (variable.VariableName == nameof(Color))
                        {
                            color = (System.Drawing.Color)variable.Value;
                        }
                        else if (variable.VariableName == nameof(FontScale))
                        {
                            fontScale = (float)variable.Value;
                        }
                        else if (variable.VariableName == nameof(BitmapFont))
                        {
                            effectiveFont = (BitmapFont)variable.Value;
                        }
                        else if (variable.VariableName == nameof(Red))
                        {
                            color = color.WithRed((byte)variable.Value);
                        }
                        else if (variable.VariableName == nameof(Green))
                        {
                            color = color.WithGreen((byte)variable.Value);
                        }
                        else if (variable.VariableName == nameof(Blue))
                        {
                            color = color.WithBlue((byte)variable.Value);
                        }
                        else if(variable.VariableName == nameof(Y))
                        {
                            yOffset = (float)variable.Value;
                        }
                        else if(variable.VariableName == "Custom")
                        {
                            var function = variable.Value as ParameterizedLetterCustomizationCall;

                            if(function != null)
                            {
                                var response = function.Function(function.CharacterIndex, function.TextBlock);

                                xOffset = response.XOffset;
                                yOffset = response.YOffset;
                                if(response.ReplacementCharacter != null)
                                {
                                    lineByLineList[0] = response.ReplacementCharacter.ToString()!;
                                }
                                if(response.Color != null)
                                {
                                    color = response.Color.Value;
                                }
                                scaleX = response.ScaleX;
                                scaleY = response.ScaleY;

                                if(scaleX != 1)
                                {
                                    switch(response.ScaleXOrigin)
                                    {
                                        case HorizontalAlignment.Left:
                                            // do nothing
                                            break;
                                        case HorizontalAlignment.Center:
                                            xOffset -= (fontScale * effectiveFont.MeasureString(lineByLineList[0]) * (scaleX - 1)) / 2.0f;
                                            break;
                                        case HorizontalAlignment.Right:
                                            xOffset -= fontScale * effectiveFont.MeasureString(lineByLineList[0]) * (scaleX - 1);
                                            break;
                                    }
                                }
                                if(scaleY != 1)
                                {
                                    switch(response.ScaleYOrigin)
                                    {
                                        case VerticalAlignment.Top:
                                            // do nothing
                                            break;
                                        case VerticalAlignment.Center:
                                            yOffset -= (fontScale * effectiveFont.LineHeightInPixels * (scaleY - 1)) / 2.0f;
                                            break;
                                        case VerticalAlignment.Bottom:
                                            yOffset -= fontScale * effectiveFont.LineHeightInPixels * (scaleY - 1);
                                            break;
                                        case VerticalAlignment.TextBaseline:
                                            yOffset -= fontScale * effectiveFont.BaselineY * (scaleY - 1);
                                            break;
                                    }
                                }

                                rotationOffset = response.RotationDegrees;
                            }
                        }
                    }


                    var baselineDifference = maxBaseline - (fontScale * effectiveFont.BaselineY);
                    effectiveTopOfLine += baselineDifference;

                    var rect = effectiveFont.DrawTextLines(lineByLineList, HorizontalAlignment,
                        this,
                        requiredWidth, 
                        individualLineWidth,
                        spriteRenderer, 
                        color,
                        absoluteLeft + xOffset,
                        effectiveTopOfLine + yOffset,
                        rotation + rotationOffset, 
                        fontScale * scaleX, 
                        fontScale * scaleY, 
                        lettersLeft, 
                        OverrideTextRenderingPositionMode, 
                        lineHeightMultiplier: LineHeightMultiplier,
                        shiftForOutline:substringIndex == 0,
                        overlapDirection: OverlapDirection);

                    if (lettersLeft != null)
                    {
                        lettersLeft -= substring.Substring.Length;
                    }

                    absoluteLeft += rect.Width / scaleX;

                }

                topOfLine += lineHeight;
            }
            startOfLineIndex += lineOfText.Length;
        }
    }

    // made public for auto tests:
    public List<StyledSubstring> GetStyledSubstrings(int startOfLineIndex, string lineOfText, Color color)
    {
        List<StyledSubstring> substrings = new ();
        int currentSubstringStart = 0;

        List<InlineVariable> currentlyActiveInlines = new ();
        List<InlineVariable> inlinesForThisCharacter = new ();

        int relativeLetterIndex = 0;
        for (; relativeLetterIndex < lineOfText.Length; relativeLetterIndex++)
        {
            inlinesForThisCharacter.Clear();
            var absoluteIndex = startOfLineIndex + relativeLetterIndex;

            var startNewRun = relativeLetterIndex == 0;
            var endLastRun = false;
            foreach (var variable in InlineVariables)
            {

                if (absoluteIndex >= variable.StartIndex && absoluteIndex < variable.StartIndex + variable.CharacterCount)
                {
                    if (currentlyActiveInlines.Contains(variable) == false)
                    {
                        startNewRun = true;
                        endLastRun = true;
                    }
                    inlinesForThisCharacter.Add(variable);
                }
            }

            foreach (var variable in currentlyActiveInlines)
            {
                if (absoluteIndex >= variable.StartIndex + variable.CharacterCount)
                {
                    startNewRun = true;
                    endLastRun = true;
                }
            }

            if (endLastRun && substrings.Count > 0)
            {
                var lastSubstring = substrings.Last();
                lastSubstring.Substring = lineOfText.Substring(currentSubstringStart, relativeLetterIndex - currentSubstringStart);
            }

            if (startNewRun)
            {
                currentSubstringStart = relativeLetterIndex;

                var styledSubstring = new StyledSubstring();
                foreach(var item in inlinesForThisCharacter)
                {
                    var existing = styledSubstring.Variables.FirstOrDefault(x => x.VariableName == item.VariableName);
                    if(existing != null)
                    {
                        // This allows new variables to replace old ones:
                        styledSubstring.Variables.Remove(existing);
                    }
                    styledSubstring.Variables.Add(item);
                }
                styledSubstring.StartIndex = relativeLetterIndex;

                if (relativeLetterIndex == lineOfText.Length - 1)
                {
                    styledSubstring.Substring = lineOfText.Substring(currentSubstringStart);
                }

                substrings.Add(styledSubstring);

                currentlyActiveInlines.Clear();
                currentlyActiveInlines.AddRange(inlinesForThisCharacter);
            }
        }

        var endSubstring = substrings.LastOrDefault();
        if (endSubstring != null)
        {
            endSubstring.Substring = lineOfText.Substring(currentSubstringStart, relativeLetterIndex - currentSubstringStart);
        }

        //if (lastSubstring == null && substrings.Count == 0 )
        //{
        //    var styledSubstring = new StyledSubstring();
        //    // no styles
        //    styledSubstring.Substring = lineOfText.Substring(0, letter);
        //    styledSubstring.StartIndex = startOfLineIndex;
        //    substrings.Add(styledSubstring);
        //}

        return substrings;
    }

    private void RenderUsingBitmapFont(SpriteRenderer spriteRenderer, SystemManagers managers)
    {
        UpdateIpsoForRendering();

        if (mBitmapFont?.AtlasedTexture != null)
        {
            mBitmapFont.RenderAtlasedTextureToScreen(WrappedText, this.HorizontalAlignment, mTextureToRender.Height,
                Color.FromArgb(mAlpha, mRed, mGreen, mBlue), Rotation, mFontScale, managers, spriteRenderer, this);
        }
        else
        {
            Sprite.Render(managers, spriteRenderer, mTempForRendering, mTextureToRender,
                Color.FromArgb(mAlpha, mRed, mGreen, mBlue), null, false, Rotation,
                treat0AsFullDimensions: false,
                objectCausingRendering: this);

        }
    }

    private void UpdateIpsoForRendering()
    {
        if (mTempForRendering == null)
        {
            // Why do we need managers?
            //mTempForRendering = new LineRectangle(managers);
            // And why do we even need a line rectangle?
            mTempForRendering = new InvisibleRenderable();
        }

        mTempForRendering.X = this.X;
        mTempForRendering.Y = this.Y;

        if (mPreRenderWidth.HasValue)
        {
            mTempForRendering.Width = this.mPreRenderWidth.Value * mFontScale;
            mTempForRendering.Height = this.mPreRenderHeight.Value * mFontScale;
        }
        else
        {
            mTempForRendering.Width = this.mTextureToRender.Width * mFontScale;
            mTempForRendering.Height = this.mTextureToRender.Height * mFontScale;
        }
        //mTempForRendering.Parent = this.Parent;

        float widthDifference = this.EffectiveWidth - mTempForRendering.Width;

        Vector3 alignmentOffset = Vector3.Zero;

        if (this.HorizontalAlignment == Graphics.HorizontalAlignment.Center)
        {
            alignmentOffset.X = widthDifference / 2.0f;
        }
        else if (this.HorizontalAlignment == Graphics.HorizontalAlignment.Right)
        {
            alignmentOffset.X = widthDifference;
        }

        if (this.VerticalAlignment == Graphics.VerticalAlignment.Center)
        {
            alignmentOffset.Y = (this.EffectiveHeight - mTempForRendering.Height) / 2.0f;
        }
        else if (this.VerticalAlignment == Graphics.VerticalAlignment.Bottom)
        {
            alignmentOffset.Y = this.EffectiveHeight - mTempForRendering.Height;
        }

        var absoluteRotation = this.GetAbsoluteRotation();
        if (absoluteRotation != 0)
        {
            var matrix = Matrix.CreateRotationZ(-MathHelper.ToRadians(absoluteRotation));

            alignmentOffset = Vector3.Transform(alignmentOffset, matrix);
        }

        mTempForRendering.X += alignmentOffset.X;
        mTempForRendering.Y += alignmentOffset.Y;

        if (this.Parent != null)
        {
            mTempForRendering.X += Parent.GetAbsoluteX();
            mTempForRendering.Y += Parent.GetAbsoluteY();

        }
    }

    IRenderableIpso mTempForRendering;

    private void RenderUsingSpriteFont(SpriteRenderer spriteRenderer)
    {

        Vector2 offset = new Vector2(this.Renderer.Camera.RenderingXOffset, Renderer.Camera.RenderingYOffset);

        float leftSide = offset.X + this.GetAbsoluteX();
        float topSide = offset.Y + this.GetAbsoluteY();

        SpriteFont font = DefaultFont;
        // Maybe this hasn't been loaded yet?
        if (font != null)
        {
            var lineCount = mWrappedText.Count;
            switch (this.VerticalAlignment)
            {
                case Graphics.VerticalAlignment.Top:
                    offset.Y = topSide;
                    break;
                case Graphics.VerticalAlignment.Bottom:
                    {
                        float requiredHeight = (lineCount) * font.LineSpacing;

                        offset.Y = topSide + (this.Height - requiredHeight);

                        break;
                    }
                case Graphics.VerticalAlignment.Center:
                    {
                        float requiredHeight = (lineCount) * font.LineSpacing;

                        offset.Y = topSide + (this.Height - requiredHeight) / 2.0f;
                        break;
                    }
            }



            float offsetY = offset.Y;

            for (int i = 0; i < lineCount; i++)
            {
                offset.X = leftSide;
                offset.Y = (int)offsetY;

                string line = mWrappedText[i];

                if (HorizontalAlignment == Graphics.HorizontalAlignment.Right)
                {
                    offset.X = leftSide + (EffectiveWidth - font.MeasureString(line).X);
                }
                else if (HorizontalAlignment == Graphics.HorizontalAlignment.Center)
                {
                    offset.X = leftSide + (EffectiveWidth - font.MeasureString(line).X) / 2.0f;
                }

                offset.X = (int)offset.X; // so we don't have half-pixels that render weird

                spriteRenderer.DrawString(font, line, offset, Color, this);
                offsetY += DefaultFont.LineSpacing;
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
            int requiredWidth = 0;
            int requiredHeight = 0;

            if (this.mRawText != null)
            {
                mBitmapFont.GetRequiredWidthAndHeight(WrappedText, out requiredWidth, out requiredHeight);
            }

            mPreRenderWidth = requiredWidth;
            mPreRenderHeight = (int)(requiredHeight * LineHeightMultiplier + .5f);
        }
    }
    #endregion

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
    IVisible? IVisible.Parent => ((IRenderableIpso)this).Parent as IVisible;

    #endregion

    public Text Clone()
    {
        var newInstance = (Text)this.MemberwiseClone();
        newInstance.mParent = null;
        newInstance.mChildren = new ();

        return newInstance;
    }

    object ICloneable.Clone()
    {
        return Clone();
    }
}
