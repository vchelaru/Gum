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

namespace SkiaGum
{
    public class Text : IRenderableIpso, IVisible, IText
    {
        #region Fields/Properties

        public static decimal ScreenDensity = 1;

        //public SKTypeface Font { get; set; }
        public string FontName { get; set; } = "Arial";

        public int FontSize { get; set; } = 12;

        public float FontScale { get; set; }

        public float BoldWeight { get; set; } = 1;

        // I don't know if this should be a skia, XamForms, or XNA color...
        public SKColor Color
        {
            get;
            set;
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
            get; set;
        }

        public bool IsTruncatingWithEllipsisOnLastLine { get; set; }

        public bool IsItalic { get; set; }

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

        public float DescenderHeight
        {
            get
            {
                float toReturn = 0;
                var textBlock = GetTextBlock();
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
            get;
            set;
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

        public float LineHeightMultiplier { get; set; } = 1;

        public float WrappedTextHeight
        {
            get
            {
                var textBlock = GetTextBlock();
                return textBlock.MeasuredHeight;
            }
        }

        public float WrappedTextWidth
        {
            get
            {
                var textBlock = GetTextBlock();
                return textBlock.MeasuredWidth;
            }
        }

        // do nothing, this doesn't render to a local render target
        public void SetNeedsRefreshToTrue() { }

        // This could cache the prerendered for speed, but we currently don't do that...
        public void UpdatePreRenderDimensions() { }

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
                var textBlock = GetTextBlock();
                
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

#if DEBUG
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

        void IRenderableIpso.SetParentDirect(IRenderableIpso parent)
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
}
