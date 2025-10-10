using Gum;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using BlendState = Gum.BlendState;

namespace RenderingLibrary.Graphics;

public abstract class RenderableBase : IVisible, IRenderableIpso, 
    ISetClipsChildren
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

    protected ObservableCollection<IRenderableIpso> _children = new ObservableCollection<IRenderableIpso>();
    public ObservableCollection<IRenderableIpso> Children => _children;
    ColorOperation IRenderableIpso.ColorOperation => ColorOperation.Modulate;
    // If a GUE uses this, it needs to support storing the values.
    public bool ClipsChildren { get; set; }

    float height;
    public float Height
    {
        get { return height; }
        set
        {
#if FULL_DIAGNOSTICS
            if (float.IsPositiveInfinity(value))
            {
                throw new ArgumentException();
            }
#endif
            height = value;
        }
    }

    public string Name { get; set; }

    protected IRenderableIpso? _parent;
    public IRenderableIpso? Parent
    {
        get { return _parent; }
        set
        {
            if (_parent != value)
            {
                if (_parent != null)
                {
                    _parent.Children.Remove(this);
                }
                _parent = value;
                if (_parent != null)
                {
                    _parent.Children.Add(this);
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

    // Is this actually needed publicly?
    public float Alpha { get; set; } = 255;

    int IRenderableIpso.Alpha => (int)this.Alpha;

    IVisible? IVisible.Parent { get { return Parent as IVisible; } }

    public virtual string BatchKey => string.Empty;


    public virtual void StartBatch(ISystemManagers systemManagers) 
    {
    }
    public virtual void EndBatch(ISystemManagers systemManagers) 
    { 
    }

    public virtual void PreRender()
    {
        
    }

    public abstract void Render(ISystemManagers managers);


    void IRenderableIpso.SetParentDirect(IRenderableIpso? parent)
    {
        _parent = parent;
    }

    public override string ToString()
    {
        return Name;
    }
}
