using RenderingLibrary;
using RenderingLibrary.Graphics;
using SkiaGum.GueDeriving;
using System;
using System.Collections.ObjectModel;
using Vector2 = System.Numerics.Vector2;
using Matrix = System.Numerics.Matrix4x4;
using Gum;

namespace SkiaGum.Renderables;

internal class CanvasRenderable : IRenderableIpso, IVisible
{
    public object Tag { get; set; }
    public string Name
    {
        get;
        set;
    }

    public bool IsRenderTarget => false;

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

    public ColorOperation ColorOperation { get; set; } = ColorOperation.Modulate;

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

    public float Rotation { get; set; }


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

    public bool Wrap => false;

    public event Action<SkiaSharp.SKCanvas> CustomRender;

    public CanvasRenderable()
    {
        mChildren = new ObservableCollection<IRenderableIpso>();

        Visible = true;
        Width = 100;
        Height = 100;
    }
    public int Alpha => 255;


    public void PreRender() { }

#if SKIA
    public void Render(ISystemManagers managers)
    {
        var canvas = (managers as SystemManagers).Canvas;

        if (AbsoluteVisible)
        {
            var absoluteX = this.GetAbsoluteX();
            var absoluteY = this.GetAbsoluteY();
            var boundingRect = new SkiaSharp.SKRect(absoluteX, absoluteY, absoluteX + this.Width, absoluteY + this.Height);

            // offset by the X/Y so that everything is relative to this absolute:
            canvas.Save();

            canvas.Translate(absoluteX, absoluteY);

            CustomRender?.Invoke(canvas);

            canvas.Restore();
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
