using Gum.GueDeriving;
using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using RenderingLibrary.Math;
using ToolsUtilitiesStandard.Helpers;
using static Raylib_cs.Raylib;

namespace Gum.Renderables;

/// <summary>
/// This enum defines the ways the renderer can align
/// text, which can affect clarity - especially with
/// small fonts when drawing with point sampling
/// </summary>
public enum TextRenderingPositionMode
{
    /// <summary>
    /// In this mode, the renderer will ensure text renders at
    /// a whole pixel which can avoid artifacting on small fonts
    /// </summary>
    SnapToPixel,
    
    /// <summary>
    /// In this mode the renderer will render text at its
    /// specified position, even if that's a fractional or
    /// subpixel.
    /// </summary>
    FreeFloating,
}

/// <summary>
/// This enum defines the way the renderer can round text
/// rendering to the nearest pixel. This only applies if
/// TextRenderingPositionMode is set to SnapToPixel
/// </summary>
public enum TextPositionRoundingMode
{
    /// <summary>
    /// This mode does special integer rounding to the nearest
    /// pixel and will round midpoints away from zero.
    /// </summary>
    RoundToInt,
    
    /// <summary>
    /// This mode always rounds to floor, which may reduce
    /// render jittering affecting the spacing between text.
    /// Use this if your default rendering behavior rounds
    /// to floor for subpixels.
    /// </summary>
    Floor,
    
    /// <summary>
    /// This mode always rounds to ceiling, which may reduce
    /// render jittering affecting the spacing between text.
    /// Use this if your default rendering behavior rounds
    /// to ceiling for subpixels.
    /// </summary>
    Ceiling,
}


public class Text : IVisible, IRenderableIpso,
    IWrappedText
{
    /// <summary>
    /// The line height as defined by the font, ignoring FontScale.
    /// </summary>
    private int _lineHeightInPixels;

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

    bool IWrappedText.IsMidWordLineBreakEnabled => true;

    //static Font defaultFont = Raylib.GetFontDefault();

    public Vector2 Position;
    
    /// <summary>
    /// Whether the renderer should snap text rendering to whole pixels or not. Default
    /// behavior is to snap as this prevents baseline misalignment and artifacts for
    /// small fonts.
    /// </summary>
    public static TextRenderingPositionMode TextRenderingPositionMode = TextRenderingPositionMode.SnapToPixel;
    
    /// <summary>
    /// How the renderer should round text rendering to whole pixels. Only applies if
    /// TextRenderingPositionMode is SnapToPixel. Default is to use special integer rounding.
    /// </summary>
    public static TextPositionRoundingMode TextPositionRoundingMode = TextPositionRoundingMode.RoundToInt;

    List<string> mWrappedText = new List<string>();
    float? mWidth = 200;
    float mHeight = 200;

    IRenderableIpso? mParent;

    ObservableCollectionNoReset<IRenderableIpso> mChildren;

    ColorOperation IRenderableIpso.ColorOperation => ColorOperation.Modulate;

    bool IRenderableIpso.ClipsChildren
    {
        get
        {
            return false;
        }
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

    public float Z
    {
        get;
        set;
    }

    public string FontFamily
    {
        get; set;
    }

    Font _font;
    public Font Font
    {
        get
        {
            if (_font.BaseSize == 0)
            {
                _font = GetFontDefault();
            }

            return _font;
        }
        set
        {
            _font = value;

            // cache this to make checking this faster
            UpdateLineHeightInPixels();
        }
    }

    private void UpdateLineHeightInPixels()
    {
        _lineHeightInPixels = (int)(MeasureTextEx(_font, "M", _font.BaseSize, 0).Y + .5);
    }

    public ObservableCollection<IRenderableIpso> Children
    {
        get { return mChildren; }
    }

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
                Color = new Color(Color.R, Color.G, Color.B, (byte)value);
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
                Color = new Color((byte)value, Color.G, Color.B, Color.A);
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
                Color = new Color(Color.R, (byte)value, Color.B, Color.A);
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
                Color = new Color(Color.R, Color.G, (byte)value, Color.A);
            }
        }
    }

    public Color Color
    {
        get; set;
    } = Color.DarkGray;


    public bool IsTruncatingWithEllipsisOnLastLine { get; set; }
        // temp:
        = true;

    public string Name
    {
        get;
        set;
    }

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

    public float DescenderHeight => 2;

    public float FontScale { get; set; } = 1;

    public object Tag { get; set; }


    public BlendState BlendState { get; set; } = BlendState.NonPremultiplied;

    public int FontSize
    {
        get => _fontSize;
        set
        {
            _fontSize = value; UpdateLineHeightInPixels();
        }
    }

    public float WrappedTextWidth
    {
        get
        {
            if (mPreRenderWidth != null)
            {
                return mPreRenderWidth.Value * FontScale;
            }
            //else if (mTextureToRender?.Width > 0)
            //{
            //    return mTextureToRender.Width * mFontScale;
            //}
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
                return mPreRenderHeight.Value * FontScale;
            }
            //else if (mTextureToRender?.Height > 0)
            //{
            //    return mTextureToRender.Height * mFontScale;
            //}
            else
            {
                return 0;
            }
        }
    }

    int? maxNumberOfLines;
    private int _fontSize = 12;

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

    public TextOverflowVerticalMode TextOverflowVerticalMode { get; set; }
    


    public HorizontalAlignment HorizontalAlignment
    {
        get; set;
    }

    public VerticalAlignment VerticalAlignment
    {
        get; set;
    }

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
                //UpdateLinePrimitive();
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

                //UpdateLinePrimitive();

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
                return mPreRenderWidth.Value * FontScale;
            }
            //else if (mTextureToRender != null)
            //{
            //    if (mTextureToRender.Width == 0)
            //    {
            //        return 10;
            //    }
            //    else
            //    {
            //        return mTextureToRender.Width * mFontScale;
            //    }
            //}
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
                return mPreRenderHeight.Value * FontScale;
            }
            //else if (mTextureToRender != null)
            //{
            //    if (mTextureToRender.Height == 0)
            //    {
            //        return 10;
            //    }
            //    else
            //    {
            //        return mTextureToRender.Height * mFontScale;
            //    }
            //}
            else
            {
                return 32;
            }
        }
    }

    public string? StoredMarkupText => null;


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

    // not sure if basesize is correct here...
    public int LineHeightInPixels => _lineHeightInPixels;

    bool IRenderable.Wrap => false;


    bool IRenderableIpso.IsRenderTarget => false;


    static Text()
    {
        GraphicalUiElement.UpdateFontFromProperties += HandleUpdateFontValues;
    }

    public Text() : this(null)
    {
    }

    public Text(ISystemManagers? managers)
    {
        Font = GetFontDefault();
        mChildren = new();
        Visible = true;
    }

    private static void HandleUpdateFontValues(IText text, GraphicalUiElement element)
    {
        var asText = text as Text;
        if(element is TextRuntime asTextRuntime)
        {
            asText.FontSize = asTextRuntime.FontSize;

        }
    }

    private void UpdateWrappedText()
    {
        mWrappedText.Clear();
        this.UpdateLines(mWrappedText);
    }

    /// <summary>
    /// Returns the size of the string, ignoring font scale, but considering the bitmap font.
    /// </summary>
    /// <param name="whatToMeasure"></param>
    /// <returns></returns>
    public float MeasureString(string whatToMeasure)
    {
        return MeasureTextEx(Font, whatToMeasure, _font.BaseSize, 0).X;
    }

    public virtual void PreRender() { }

    public void Render(ISystemManagers managers)
    {
        if (!Visible) return;

        var fontValue = Font;

        var absoluteLeft = this.GetAbsoluteLeft();
        var position = new Vector2(
            absoluteLeft,
            this.GetAbsoluteTop());
        var origin = new Vector2(
            0,
            0);


        if (VerticalAlignment == VerticalAlignment.Center)
        {
            position.Y += this.Height / 2;
            origin.Y = FontScale * mPreRenderHeight / 2 ?? 0;
        }
        if (VerticalAlignment == VerticalAlignment.Bottom)
        {
            position.Y += this.Height;
            origin.Y = FontScale * mPreRenderHeight ?? 0;
        }

        for(int i = 0; i < WrappedText.Count; i++)
        {
            var line = WrappedText[i];
            origin.X = 0;
            position.X = absoluteLeft;

            if(HorizontalAlignment == HorizontalAlignment.Center)
            {
                position.X += (this.Width??32) / 2;
                origin.X = MeasureTextEx(fontValue, line, fontValue.BaseSize * FontScale, 0).X/2;
            }
            else if (HorizontalAlignment == HorizontalAlignment.Right)
            {
                position.X += this.Width??32;
                origin.X = MeasureTextEx(fontValue, line, fontValue.BaseSize * FontScale, 0).X;
            }
            var linePosition = position;
            linePosition.Y += i * LineHeightInPixels;

            if (TextRenderingPositionMode == TextRenderingPositionMode.SnapToPixel)
            {
                // 2025-12 JUSTIN: Changes to vertical alignment resulted fractional
                // origin values which cause baseline misalignment and broken text.
                // Applied the same rounding used for position to origin, which fixes
                // the problem but may cause weird artifacts or "sizzle" for really
                // small pixel fonts
                
                switch (TextPositionRoundingMode)
                {
                    case TextPositionRoundingMode.Floor:
                        linePosition = new Vector2(
                            (int)Math.Floor(linePosition.X),
                            (int)Math.Floor(linePosition.Y));
                        origin = new Vector2(
                            (int)Math.Floor(origin.X),
                            (int)Math.Floor(origin.Y)
                        );
                        break;
                    case TextPositionRoundingMode.Ceiling:
                        linePosition = new Vector2(
                            (int)Math.Ceiling(linePosition.X),
                            (int)Math.Ceiling(linePosition.Y));
                        origin = new Vector2(
                            (int)Math.Ceiling(origin.X),
                            (int)Math.Ceiling(origin.Y)
                        );
                        break;
                    default:
                        linePosition = new Vector2(
                            MathFunctions.RoundToInt(linePosition.X),
                            MathFunctions.RoundToInt(linePosition.Y));
                        origin = new Vector2(
                            MathFunctions.RoundToInt(origin.X),
                            MathFunctions.RoundToInt(origin.Y)
                        );
                        break;
                }
            }

            Raylib.SetTextureFilter(fontValue.Texture, TextureFilter.Point);
            DrawTextPro(fontValue, line, linePosition, origin, 0, fontValue.BaseSize * FontScale, 0, Color);
        }

        // todo - handle alignment
        //DrawText(RawText, x, y, 20, Color.DarkGray);
        //DrawTextEx(Font, RawText, position, FontSize, 0, Color);
        //const float spacing = 1;
        //DrawTextPro(fontValue, RawText, position, origin, 0, FontSize, spacing, Color);

    }

    public void SetNeedsRefreshToTrue()
    {
    }

    public void UpdatePreRenderDimensions()
    {
        int requiredWidth = 0;
        int requiredHeight = 0;

        if (this.mRawText != null)
        {
            GetRequiredWidthAndHeight(WrappedText, out requiredWidth, out requiredHeight, null);
        }

        mPreRenderWidth = (int)(requiredWidth + .5f);
        //mPreRenderHeight = (int)(requiredHeight * LineHeightMultiplier + .5f);
        mPreRenderHeight = (int)(requiredHeight * 1 + .5f);
    }

    public void GetRequiredWidthAndHeight(IEnumerable<string> lines, out int requiredWidth, out int requiredHeight, List<float>? widths)
    {

        float maxWidth = 0;
        float maxHeight = 0;

        foreach (string line in lines)
        {
            maxHeight += LineHeightInPixels;
            float lineWidth = 0;

            lineWidth = (int)Math.Ceiling(this.MeasureString(line));
            if (widths != null)
            {
                widths.Add(lineWidth);
            }
            maxWidth = System.Math.Max(lineWidth, maxWidth);
        }

        const int MaxWidthAndHeight = 4096; // change this later?
        requiredWidth = System.Math.Min((int)(maxWidth +.5f), MaxWidthAndHeight);
        requiredHeight = System.Math.Min((int)(maxHeight + .5f), MaxWidthAndHeight);
        //if (requiredWidth != 0 && mOutlineThickness != 0)
        //{
        //    requiredWidth += mOutlineThickness * 2;
        //}
    }

    void IRenderableIpso.SetParentDirect(IRenderableIpso? parent)
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
}

public static class StringExtensions
{
    public static string SubstringEnd(this string value, int lettersToRemove)
    {
        if (value.Length <= lettersToRemove)
        {
            return string.Empty;
        }
        else
        {
            return value.Substring(0, value.Length - lettersToRemove);
        }
    }
}
