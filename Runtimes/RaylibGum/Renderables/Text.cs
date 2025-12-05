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
using static Raylib_cs.Raylib;

namespace Gum.Renderables;
public class Text : IVisible, IRenderableIpso,
    IWrappedText
{
    Font _font = Raylib.GetFontDefault();
    bool IWrappedText.IsMidWordLineBreakEnabled => true;

    //static Font defaultFont = Raylib.GetFontDefault();

    public Vector2 Position;

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
        set => _font = value;
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

    public float FontScale => 1;

    public object Tag { get; set; }


    public BlendState BlendState { get; set; } = BlendState.NonPremultiplied;

    public int FontSize { get; set; } = 12;

    public float WrappedTextWidth =>  MeasureTextEx(Font, RawText, FontSize, 1).X;

    public float WrappedTextHeight => MeasureTextEx(Font, RawText, FontSize, 1).Y;

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
            //else if (mPreRenderWidth.HasValue)
            //{
            //    return mPreRenderWidth.Value * mFontScale;
            //}
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
            //else if (mPreRenderHeight.HasValue)
            //{
            //    return mPreRenderHeight.Value * mFontScale;
            //}
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
    public int LineHeightInPixels => Font.BaseSize;

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

    public float MeasureString(string whatToMeasure)
    {
        return MeasureTextEx(Font, whatToMeasure, FontSize, 1).X;
    }

    public virtual void PreRender() { }

    public void Render(ISystemManagers managers)
    {
        if (!Visible) return;

        var fontValue = Font;

        var position = new Vector2(
            this.GetAbsoluteLeft(),
            this.GetAbsoluteTop());
        var origin = new Vector2(
            0, // todo - handle horizontal alignment
            0); // todo - handle vertical alignment


        if(HorizontalAlignment == HorizontalAlignment.Center)
        {
            position.X += this.Width??32 / 2;
            origin.X = MeasureTextEx(fontValue, RawText, FontSize, 1).X/2;
        }
        else if (HorizontalAlignment == HorizontalAlignment.Right)
        {
            position.X += this.Width??32;
            origin.X = MeasureTextEx(fontValue, RawText, FontSize, 1).X;
        }

        if (VerticalAlignment == VerticalAlignment.Center)
        {
            position.Y += this.Height / 2;
            origin.Y = MeasureTextEx(fontValue, RawText, FontSize, 1).Y / 2;
        }
        if (VerticalAlignment == VerticalAlignment.Bottom)
        {
            position.Y += this.Height;
            origin.Y = MeasureTextEx(fontValue, RawText, FontSize, 1).Y;
        }

        // todo - handle alignment
        //DrawText(RawText, x, y, 20, Color.DarkGray);
        //DrawTextEx(Font, RawText, position, FontSize, 0, Color);
        const float spacing = 1;
        DrawTextPro(fontValue, RawText, position, origin, 0, FontSize, spacing, Color);

    }

    public void SetNeedsRefreshToTrue()
    {
    }

    public void UpdatePreRenderDimensions()
    {
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
