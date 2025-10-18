using RenderingLibrary;
using RenderingLibrary.Graphics;
using SkiaGum.GueDeriving;
using SkiaSharp;
using System;
using System.Collections.ObjectModel;
using Vector2 = System.Numerics.Vector2;
using Matrix = System.Numerics.Matrix4x4;
using Gum;

namespace SkiaGum.Renderables;

internal class LottieAnimation : IRenderableIpso, IVisible
{
    public SkiaSharp.Skottie.Animation Animation
    {
        get; set;
    }

    public bool IsRenderTarget => false;

    public object Tag { get; set; }


    ObservableCollection<IRenderableIpso> mChildren;
    public ObservableCollection<IRenderableIpso> Children
    {
        get { return mChildren; }
    }

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

    Vector2 Position;

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

    public DateTime TimeAnimationStarted
    {
        get;
        set;
    }
    public bool Loops
    {
        get; set;
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

    void IRenderableIpso.SetParentDirect(IRenderableIpso? parent) => mParent = parent;

    public LottieAnimation()
    {
        mChildren = new ObservableCollection<IRenderableIpso>();
        Loops = true;
        TimeAnimationStarted = DateTime.Now;
    }

    public void PreRender() { }

#if SKIA
    public void Render(ISystemManagers managers)
    {
        if (AbsoluteVisible && Animation != null)
        {
            //var textureBox = Animation.Size;
            //var textureWidth = textureBox.Width;
            //var textureHeight = textureBox.Height;

            //var scaleX = this.Width / textureWidth;
            //var scaleY = this.Height / textureHeight;

            //SKMatrix scaleMatrix = SKMatrix.MakeScale(scaleX, scaleY);
            //// Gum uses counter clockwise rotation, Skia uses clockwise, so invert:
            //SKMatrix rotationMatrix = SKMatrix.MakeRotationDegrees(-Rotation);
            //SKMatrix translateMatrix = SKMatrix.MakeTranslation(this.GetAbsoluteX(), this.GetAbsoluteY());
            //SKMatrix result = SKMatrix.MakeIdentity();

            //SKMatrix.Concat(
            //    ref result, rotationMatrix, scaleMatrix);
            //SKMatrix.Concat(
            //    ref result, translateMatrix, result);

            var secondsSinceStarted = (DateTime.Now - TimeAnimationStarted).TotalSeconds;

            if (Loops)
            {
                secondsSinceStarted = secondsSinceStarted % Animation.Duration.TotalSeconds;
            }
            else
            {
                secondsSinceStarted = Math.Min(secondsSinceStarted, Animation.Duration.TotalSeconds);
            }

            Animation.SeekFrameTime(secondsSinceStarted);


            var absoluteX = this.GetAbsoluteX();
            var absoluteY = this.GetAbsoluteY();
            var boundingRect = new SKRect(absoluteX, absoluteY, absoluteX + this.Width, absoluteY + this.Height);

            var canvas = (managers as SystemManagers).Canvas;

            Animation.Render(canvas, boundingRect);
        }
    }

    public void StartBatch(ISystemManagers systemManagers)
    {
    }

    public void EndBatch(ISystemManagers systemManagers)
    {
    }
#else
    public void Render(SpriteRenderer spriteRenderer, SystemManagers managers)
    {
        // todo
    }


#endif
    public BlendState BlendState => BlendState.AlphaBlend;

    public int Alpha => 255;

    public bool ClipsChildren { get; set; }


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

    public string BatchKey => string.Empty;

    #endregion
}
