using Gum;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using BlendState = Gum.BlendState;

namespace RenderingLibrary.Graphics;

public class InvisibleRenderable : IVisible, IRenderableIpso, 
    ISetClipsChildren, ICloneable
{
    public bool IsRenderTarget { get; set; }
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

    public BlendState BlendState { get; set; } = BlendState.NonPremultiplied;

    ObservableCollection<IRenderableIpso> children = new ObservableCollection<IRenderableIpso>();
    public ObservableCollection<IRenderableIpso> Children => children;
    ColorOperation IRenderableIpso.ColorOperation => ColorOperation.Modulate;
    // If a GUE uses this, it needs to support storing the values.
    public bool ClipsChildren { get; set; }

    float height;
    public float Height
    {
        get { return height; }
        set
        {
#if DEBUG
            if(float.IsPositiveInfinity(value))
            {
                throw new ArgumentException();
            }
#endif
            height = value;
        }
    }

    public string Name { get; set; }

    IRenderableIpso? mParent;
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

    public float Rotation { get; set; }

    public object Tag { get; set; }

    public bool Visible { get; set; } = true;

    public float Width { get; set; }

    public bool Wrap => false;

    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }

    public bool FlipHorizontal { get; set; }

    public float Alpha { get; set; } = 255;

    int IRenderableIpso.Alpha => (int)this.Alpha;

    IVisible IVisible.Parent { get { return Parent as IVisible; } }

    public virtual void PreRender()
    {

    }

    public virtual void Render(ISystemManagers managers)
    {
    }


    void IRenderableIpso.SetParentDirect(IRenderableIpso? parent)
    {
        mParent = parent;
    }

    public override string ToString()
    {
        return Name;
    }



    public InvisibleRenderable Clone()
    {
        var newInstance = (InvisibleRenderable)this.MemberwiseClone();
        newInstance.mParent = null;
        newInstance.children = new ObservableCollection<IRenderableIpso>();

        return newInstance;
    }

    object ICloneable.Clone()
    {
        return Clone();
    }
}
