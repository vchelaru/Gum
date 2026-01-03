using RenderingLibrary;
using RenderingLibrary.Graphics;
using SkiaGum.GueDeriving;
using SkiaSharp;
using Svg.Skia;
using System.Collections.ObjectModel;
using BlendState = Gum.BlendState;
using Vector2 = System.Numerics.Vector2;
using Matrix = System.Numerics.Matrix4x4;
using System.Drawing;

namespace SkiaGum;

public class VectorSprite : IRenderableIpso, IVisible, IAspectRatio, ITextureCoordinate
{
    #region Fields/Properties

    public SKColor Color
    {
        get; set;
    } = SKColors.White;

    Vector2 Position;
    IRenderableIpso mParent;

    public bool IsRenderTarget => false;

    public SKSvg Texture
    {
        get; set;
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

    public object Tag { get; set; }


    ObservableCollectionNoReset<IRenderableIpso> mChildren;
    public ObservableCollection<IRenderableIpso> Children
    {
        get { return mChildren; }
    }

    bool ITextureCoordinate.Wrap
    {
        get => false;
        set { } // not used currently
    }

    Rectangle? ITextureCoordinate.SourceRectangle
    {
        get => null;
        set { } // not used currently
    }

    public float? TextureWidth => Texture?.Picture.CullRect.Width;
    public float? TextureHeight => Texture?.Picture.CullRect.Height;

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
    public string Name
    {
        get;
        set;
    }
    public float Rotation { get; set; }

    public bool Wrap => false;

    public int Alpha
    {
        get => Color.Alpha;
        set
        {
            this.Color = new SKColor(this.Color.Red, this.Color.Green, this.Color.Blue, (byte)value);
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

    public ColorOperation ColorOperation { get; set; } = ColorOperation.Modulate;

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

    public float AspectRatio
    {
        get
        {
            if (Texture?.Picture != null)
            {
                return Texture.Picture.CullRect.Width / (float)Texture.Picture.CullRect.Height;
            }
            else
            {
                return 1;
            }
        }
    }


    #endregion

    public VectorSprite()
    {
        Width = 32;
        Height = 32;
        this.Visible = true;
        mChildren = new ();

    }

#if SKIA
    public void Render(ISystemManagers managers)
    {
        var canvas = (managers as SystemManagers).Canvas;
        if (AbsoluteVisible && Texture != null)
        {
            var textureBox = Texture.Picture.CullRect;
            var textureWidth = textureBox.Width;
            var textureHeight = textureBox.Height;

            var scaleX = this.Width / textureWidth;
            var scaleY = this.Height / textureHeight;

            SKMatrix scaleMatrix = SKMatrix.CreateScale(scaleX, scaleY);
            // Gum uses counter clockwise rotation, Skia uses clockwise, so invert:
            SKMatrix rotationMatrix = SKMatrix.CreateRotationDegrees(-Rotation);
            SKMatrix translateMatrix = SKMatrix.CreateTranslation(this.GetAbsoluteX(), this.GetAbsoluteY());
            SKMatrix result = SKMatrix.CreateIdentity();

            SKMatrix.Concat(
                ref result, rotationMatrix, scaleMatrix);
            SKMatrix.Concat(
                ref result, translateMatrix, result);


            // This code can be used to draw an orange border around a SVG, for debugging purposes
            //var placeholder = new SKRect(this.GetAbsoluteX(), this.GetAbsoluteY(), 
            //    this.GetAbsoluteX() + textureBox.Width * scaleX,
            //    this.GetAbsoluteY() + textureBox.Height * scaleY);

            //canvas.DrawRect(placeholder, new SKPaint() { Color = SKColors.Orange });

            // April 21, 2023
            // SVGs like the google fit SVG do not render correctly. Not sure why...
            // going to put a clip:
            // July 7, 2023
            // If we clip, then rotation won't work. We'd have to do a rotated rectangle
            // I think the SVG rendering in April 21 was using the old SVG rendering (maybe?)
            // so going to check if rotation is not 0
            var shouldClip = Rotation == 0;
            if (shouldClip)
            {
                canvas.Save();
                var clipRect = new SKRect(this.GetAbsoluteX(), this.GetAbsoluteY(),
                    this.GetAbsoluteX() + textureBox.Width * scaleX,
                    this.GetAbsoluteY() + textureBox.Height * scaleY);
                canvas.ClipRect(clipRect);

            }
            {
                // Currently this supports "multiply". Other color operations could be supported...
                if (Color.Red != 255 || Color.Green != 255 || Color.Blue != 255 || Color.Alpha != 255)
                {
                    var paint = new SKPaint() { Color = Color };
                    var redRatio = Color.Red / 255.0f;
                    var greenRatio = Color.Green / 255.0f;
                    var blueRatio = Color.Blue / 255.0f;

                    paint.ColorFilter =
                        SKColorFilter.CreateColorMatrix(new float[]
                        {
                        redRatio   , 0            , 0        , 0, 0,
                        0,           greenRatio   , 0        , 0, 0,
                        0,           0            , blueRatio, 0, 0,
                        0,           0            , 0        , 1, 0
                        });

                    using (paint)
                    {
                        canvas.DrawPicture(Texture.Picture, ref result, paint);
                    }
                }
                else
                {
                    canvas.DrawPicture(Texture.Picture, ref result);
                }
            }
            if (shouldClip)
            {
                canvas.Restore();
            }
        }
    }
#else
    public void Render(SpriteRenderer spriteRenderer, SystemManagers managers)
    {

    }
#endif
    public BlendState BlendState => BlendState.AlphaBlend; 

    public bool ClipsChildren { get; set; }

    public void PreRender() { }

    void IRenderableIpso.SetParentDirect(IRenderableIpso? parent) => mParent = parent;

    public void StartBatch(ISystemManagers systemManagers)
    {
    }

    public void EndBatch(ISystemManagers systemManagers)
    {
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
    IVisible? IVisible.Parent => ((IRenderableIpso)this).Parent as IVisible;

    public string BatchKey => string.Empty;


    #endregion
}
