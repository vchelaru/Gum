using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using SkiaGum.Graphics;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Topten.RichTextKit;
using Xamarin.Forms.PlatformConfiguration.TizenSpecific;

namespace SkiaGum
{
    public class Text : IRenderableIpso, IVisible
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
                this.Color = new SKColor(this.Color.Red, this.Color.Green, (byte)value);
            }
        }

        public int Green
        {
            get => Color.Green;
            set
            {
                this.Color = new SKColor(this.Color.Red, (byte)value, this.Color.Blue);
            }
        }

        public int Red
        {
            get => Color.Red;
            set
            {
                this.Color = new SKColor((byte)value, this.Color.Green, this.Color.Blue);
            }
        }

        public int? MaximumNumberOfLines
        {
            get; set;
        }


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

        public void Render(SKCanvas canvas)
        {
            if (AbsoluteVisible)
            {
                var textBlock = GetTextBlock();
                
                SKMatrix scaleMatrix = SKMatrix.MakeScale(1,1);
                //// Gum uses counter clockwise rotation, Skia uses clockwise, so invert:
                SKMatrix rotationMatrix = SKMatrix.MakeRotationDegrees(-Rotation);
                SKMatrix translateMatrix = SKMatrix.MakeTranslation(this.GetAbsoluteX(), this.GetAbsoluteY());
                SKMatrix result = SKMatrix.MakeIdentity();

                SKMatrix.Concat(
                    ref result, rotationMatrix, scaleMatrix);
                SKMatrix.Concat(
                    ref result, translateMatrix, result);

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
                FontWeight = (int)(400 * BoldWeight)
            };

            return style;
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
    }
}
