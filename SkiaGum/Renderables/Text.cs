using RenderingLibrary;
using RenderingLibrary.Graphics;
using SkiaGum.GueDeriving;
using SkiaSharp;
using System;
using System.Collections.ObjectModel;
using Topten.RichTextKit;
using Vector2 = System.Numerics.Vector2;
using Matrix = System.Numerics.Matrix4x4;
using Gum;
using Gum.DataTypes;

namespace SkiaGum;

public class Text : IRenderableIpso, IVisible, IText
{
    #region Fields/Properties

    public static decimal ScreenDensity = 1;

    public bool IsRenderTarget => false;

    float? mWidth = 200;

    //public SKTypeface Font { get; set; }
    public string FontName
    {
        get => _fontName; 
        set
        {
            _fontName = value;
            _cachedTextBlock = null;

        }
    }
    public int FontSize
    {
        get => _fontSize; 
        set
        {
            _fontSize = value;
            _cachedTextBlock = null;
        }
    }
    public float FontScale
    {
        get => _fontScale; 
        set
        {
            _fontScale = value;
            _cachedTextBlock = null;
        }
    }

    public float BoldWeight
    {
        get => _boldWeight; 
        set
        {
            _boldWeight = value;
            _cachedTextBlock = null;
        }
    }
    // I don't know if this should be a skia, XamForms, or XNA color...
    public SKColor Color
    {
        get => _color;
        set
        {
            _color = value;
            _cachedTextBlock = null;
        }
    }

    public int Blue
    {
        get => Color.Blue;
        set
        {
            this.Color = new SKColor(this.Color.Red, this.Color.Green, (byte)value, this.Color.Alpha);
        }
    }

    public int Green
    {
        get => Color.Green;
        set
        {
            this.Color = new SKColor(this.Color.Red, (byte)value, this.Color.Blue, this.Color.Alpha);
        }
    }

    public int Red
    {
        get => Color.Red;
        set
        {
            this.Color = new SKColor((byte)value, this.Color.Green, this.Color.Blue, this.Color.Alpha);
        }
    }

    public int Alpha
    {
        get => Color.Alpha;
        set
        {
            this.Color = new SKColor(this.Color.Red, this.Color.Green, this.Color.Blue, (byte)value);

        }
    }

    public int? MaximumNumberOfLines
    {
        get => _maximumNumberOfLines; set
        {
            _maximumNumberOfLines = value;
            _cachedTextBlock = null;
        }
    }

    public bool IsTruncatingWithEllipsisOnLastLine { get; set; }

    public bool IsItalic
    {
        get => _isItalic; set
        {
            _isItalic = value;
            _cachedTextBlock = null;
        }
    }

    Vector2 Position;
    IRenderableIpso mParent;

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

    ObservableCollection<IRenderableIpso> mChildren;
    public ObservableCollection<IRenderableIpso> Children
    {
        get { return mChildren; }
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

    public float? Width
    {
        get;
        set;
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

    public float EffectiveWidth
    {
        get
        {
            return Width ?? 0;
        }
    }

    public float Height
    {
        get;
        set;
    }

    public float DescenderHeight
    {
        get
        {
            float toReturn = 0;
            var textBlock = GetCachedTextBlock();
            if (textBlock.Lines.Count > 0)
            {
                toReturn = textBlock.Lines[textBlock.Lines.Count - 1].MaxDescent;
            }
            return toReturn;
        }
    }

    public bool Wrap => false;

    public HorizontalAlignment HorizontalAlignment
    {
        get => _horizontalAlignment;
        set
        {
            _horizontalAlignment = value;
            _cachedTextBlock = null;
        }
    }

    // todo - currently this does nothing except satisfy the Gum text object interface
    public VerticalAlignment VerticalAlignment
    {
        get; set;
    }

    public string Name
    {
        get;
        set;
    }

    public float Rotation { get; set; }

    string mRawText;
    public string RawText
    {
        get
        {
            return mRawText;
        }
        set
        {
            if (mRawText != value)
            {
                mRawText = value;
                _cachedTextBlock = null;

                //UpdateWrappedText();

                //UpdatePreRenderDimensions();
            }
        }
    }

    public ColorOperation ColorOperation { get; set; } = ColorOperation.Modulate;

    public object Tag { get; set; }

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

    public float LineHeightMultiplier
    {
        get => _lineHeightMultiplier;
        set
        {
            _lineHeightMultiplier = value;
            _cachedTextBlock = null;
        }
    }
    public float WrappedTextHeight
    {
        get
        {
            var textBlock = GetCachedTextBlock();
            return textBlock.MeasuredHeight;
        }
    }

    public float WrappedTextWidth
    {
        get
        {
            var textBlock = GetCachedTextBlock();
            return textBlock.MeasuredWidth;
        }
    }

    // do nothing, this doesn't render to a local render target
    public void SetNeedsRefreshToTrue() { }

    // This could cache the prerendered for speed, but we currently don't do that...
    public void UpdatePreRenderDimensions() { }

    // todo - need to make this actually functional:
    public TextOverflowVerticalMode TextOverflowVerticalMode { get; set; } 

    #endregion

    public Text()
    {
        FontScale = 1;
        Width = 32;
        Height = 32;

        this.Visible = true;
        Color = SKColors.Black;
        mChildren = new ObservableCollection<IRenderableIpso>();
    }

    public void Render(ISystemManagers managers)
    {
        var canvas = (managers as SystemManagers).Canvas;

        if (AbsoluteVisible)
        {
            var textBlock = GetCachedTextBlock();
            
            //// Gum uses counter clockwise rotation, Skia uses clockwise, so invert:
            SKMatrix rotationMatrix = SKMatrix.CreateRotationDegrees(-Rotation);
            var absoluteX = this.GetAbsoluteX();
            var absoluteY = this.GetAbsoluteY();

            if(this.VerticalAlignment == VerticalAlignment.Center)
            {
                // compare the bound height with the actual height, and adjust the offset
                var textBlockHeight = textBlock.MeasuredHeight;
                var boundsHeight = this.Height;

                absoluteY += (boundsHeight - textBlockHeight)/2.0f;
            }

            if(this.VerticalAlignment == VerticalAlignment.Bottom)
            {
                // compare the bound height with the actual height, and adjust the offset
                var textBlockHeight = textBlock.MeasuredHeight;
                var boundsHeight = this.Height;

                absoluteY += boundsHeight - textBlockHeight;
            }

            SKMatrix translateMatrix = SKMatrix.CreateTranslation(absoluteX, absoluteY);
            // Continue to apply the previou matrix in case there is scaling
            // for device density
            SKMatrix result = rotationMatrix;

            SKMatrix.Concat(
                ref result, translateMatrix, result);
            SKMatrix.Concat(
                ref result, canvas.TotalMatrix, result);

            canvas.Save();

            // set the clip rect *after* save so it gets undone and restored
            var clipRect = this.GetEffectiveClipRect();
            if(clipRect != null)
            {
                canvas.ClipRect(clipRect.Value);
            }

            canvas.SetMatrix(result);

            textBlock.Paint(canvas, new SKPoint(0, 0));
            canvas.Restore();
        }
    }
    public BlendState BlendState => BlendState.AlphaBlend;


    public bool ClipsChildren { get; set; }

    TextBlock? _cachedTextBlock;
    float? _lastEffectiveWidth;
    decimal _lastScreenDensity;
    private string _fontName = "Arial";
    private int _fontSize = 12;
    private float _fontScale;
    private float _boldWeight = 1;
    private SKColor _color;
    private bool _isItalic;
    private float _lineHeightMultiplier = 1;
    private HorizontalAlignment _horizontalAlignment;
    private int? _maximumNumberOfLines;

    public TextBlock GetCachedTextBlock(float? forcedWidth = null)
    {
        var effectiveWidth = forcedWidth ?? this.Width;

        if(effectiveWidth != _lastEffectiveWidth || _lastScreenDensity != ScreenDensity)
        {
            _cachedTextBlock = null;
            _lastEffectiveWidth = effectiveWidth;
            _lastScreenDensity = ScreenDensity;
        }

        if(_cachedTextBlock == null)
        {
            _cachedTextBlock = GetTextBlock(effectiveWidth);
        }
        return _cachedTextBlock;
    }

    public TextBlock GetTextBlock(float? forcedWidth = null)
    {
        var textBlock = new TextBlock();
        try
        {
            textBlock.MaxWidth = forcedWidth ?? this.Width;
            textBlock.AddText(mRawText, GetStyle());
            switch (HorizontalAlignment)
            {
                case HorizontalAlignment.Left:
                    textBlock.Alignment = TextAlignment.Left;
                    break;
                case HorizontalAlignment.Center:
                    textBlock.Alignment = TextAlignment.Center;
                    break;
                case HorizontalAlignment.Right:
                    textBlock.Alignment = TextAlignment.Right;
                    break;
            }

            textBlock.MaxLines = MaximumNumberOfLines;
        }
        catch(Exception e)
        {

#if FULL_DIAGNOSTICS
            throw new InvalidOperationException($"An internal exception has occurred: {e.ToString()} with the following information:" +
                $"forcedWidth {forcedWidth}\n" +
                $"FontName {FontName}\n" +
                $"FontSize {FontSize * (float)ScreenDensity * FontScale}\n" +
                $"FontWeight {400*BoldWeight}");
            
#else
            // I guess do nothing?
#endif
        }


        return textBlock;
    }


    private Style GetStyle()
    {
        var style = new Style()
        {
            FontFamily = FontName,
            FontSize = FontSize * (float)ScreenDensity * FontScale,
            TextColor = this.Color,
            FontItalic = this.IsItalic,
            FontWeight = (int)(400 * BoldWeight),
            LineHeight = LineHeightMultiplier
        };

        return style;
    }

    void IRenderableIpso.SetParentDirect(IRenderableIpso? parent)
    {
        mParent = parent;
    }

    public void PreRender() { }

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
