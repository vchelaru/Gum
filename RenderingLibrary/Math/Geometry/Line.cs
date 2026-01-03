using RenderingLibrary.Graphics;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.ObjectModel;
using BlendState = Gum.BlendState;
using Vector2 = System.Numerics.Vector2;
using Color = System.Drawing.Color;

namespace RenderingLibrary.Math.Geometry;

public class Line : SpriteBatchRenderableBase, IRenderableIpso
{
    #region Fields

    LinePrimitive mLinePrimitive;

    public Vector2 RelativePoint;


    IRenderableIpso mParent;

    ObservableCollectionNoReset<IRenderableIpso> mChildren;
    SystemManagers mManagers;

    #endregion

    #region Properties
    ColorOperation IRenderableIpso.ColorOperation => ColorOperation.Modulate;


    public float Rotation { get; set; }

    public bool FlipHorizontal { get; set; }

    public string Name
    {
        get;
        set;
    }

    public float X
    {
        get;
        set;
    }

    public float Y
    {
        get;
        set;
    }

    public float Z
    {
        get;
        set;
    }


    public Color Color
    {
        get
        {
            return mLinePrimitive.Color;
        }
        set
        {
            mLinePrimitive.Color = value;
        }
    }

    int IRenderableIpso.Alpha => Color.A;

    public BlendState BlendState
    {
        get { return BlendState.NonPremultiplied; }
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


    bool IRenderableIpso.ClipsChildren
    {
        get
        {
            return false;
        }
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

    public ObservableCollection<IRenderableIpso> Children
    {
        get { return mChildren; }
    }

    public object Tag
    {
        get;
        set;
    }

    private Renderer AssociatedRenderer
    {
        get
        {
            if (mManagers != null)
            {
                return mManagers.Renderer;
            }
            else
            {
                return Renderer.Self;
            }
        }
    }

    public bool IsDotted
    {
        get;
        set;
    }

    public bool Wrap
    {
        get { return true; }
    }

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

    bool IRenderableIpso.IsRenderTarget => false;

    #endregion

    public Line()
        : this(null)
    {

    }

    public Line(SystemManagers managers)
    {
        mManagers = managers;

        Visible = true;
        if (mManagers != null)
        {
            mLinePrimitive = new LinePrimitive(mManagers.Renderer.SinglePixelTexture);
        }
        else
        {
            mLinePrimitive = new LinePrimitive(Renderer.Self.SinglePixelTexture);
        }

        mChildren = new ();
        UpdatePoints();
    }

    private void UpdatePoints()
    {
        while (mLinePrimitive.VectorCount < 2)
        {
            mLinePrimitive.Add(0, 0);
        }

        mLinePrimitive.Replace(1, this.RelativePoint);

        mLinePrimitive.Position.X = this.GetAbsoluteX();
        mLinePrimitive.Position.Y = this.GetAbsoluteY() ;
    }

    void IRenderableIpso.SetParentDirect(IRenderableIpso? parent)
    {
        mParent = parent;
    }

    public override void Render(ISystemManagers managers)
    {
        UpdatePoints();
        // See nineslice for explanation of this Visible check
        //if (Visible)
        {

            Texture2D textureToUse = AssociatedRenderer.SinglePixelTexture;

            if (IsDotted)
            {
                textureToUse = AssociatedRenderer.DottedLineTexture;
            }

            var systemManagers = managers as SystemManagers;

            mLinePrimitive.Render(systemManagers.Renderer.SpriteRenderer, systemManagers, textureToUse, .2f * AssociatedRenderer.Camera.Zoom);
        }
    }



    void IRenderable.PreRender() { }

    public override string ToString()
    {
        if(!string.IsNullOrEmpty(Name))
        {
            return $"{Name} (Line)";
        }
        else
        {
            return base.ToString() ;
        }
    }
}
