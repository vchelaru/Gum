using Gum;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using BlendState = Gum.BlendState;

namespace RenderingLibrary.Graphics;

public abstract class RenderableBase : IVisible, IRenderableIpso,
    ISetClipsChildren, IRenderTargetRenderable
{
    public bool IsRenderTarget { get; set; }

    /// <inheritdoc/>
    /// <remarks>
    /// Set this via the strongly-typed runtime property (e.g.
    /// <c>ContainerRuntime.RenderTargetEffect</c>) rather than assigning it directly.
    /// </remarks>
    public object? RenderTargetEffect { get; set; }

    /// <inheritdoc/>
    public bool AbsoluteVisible => ((IVisible)this).GetAbsoluteVisible();
    /// <inheritdoc/>
    IVisible? IVisible.Parent { get { return Parent as IVisible; } }

    public BlendState BlendState { get; set; } = BlendState.NonPremultiplied;

    protected ObservableCollectionNoReset<IRenderableIpso> _children = new ();
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
            global::RenderingLibrary.Graphics.NonFiniteCheck.ThrowIfNotFinite(value, "Height");
            height = value;
        }
    }

    public string? Name { get; set; }

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

    float _rotationValue;
    public float Rotation
    {
        get { return _rotationValue; }
        set
        {
            global::RenderingLibrary.Graphics.NonFiniteCheck.ThrowIfNotFinite(value, "Rotation");
            _rotationValue = value;
        }
    }

    public object? Tag { get; set; }

    public bool Visible { get; set; } = true;

    float width;
    public float Width
    {
        get { return width; }
        set
        {
            global::RenderingLibrary.Graphics.NonFiniteCheck.ThrowIfNotFinite(value, "Width");
            width = value;
        }
    }

    public bool Wrap => false;

    float _xValue;
    public float X
    {
        get { return _xValue; }
        set
        {
            global::RenderingLibrary.Graphics.NonFiniteCheck.ThrowIfNotFinite(value, "X");
            _xValue = value;
        }
    }

    float _yValue;
    public float Y
    {
        get { return _yValue; }
        set
        {
            global::RenderingLibrary.Graphics.NonFiniteCheck.ThrowIfNotFinite(value, "Y");
            _yValue = value;
        }
    }

    public float Z { get; set; }

    public bool FlipHorizontal { get; set; }


    int IRenderableIpso.Alpha => 255;


    public virtual string BatchKey => string.Empty;

    public virtual object? BatchSortKey => null;


    public virtual void StartBatch(ISystemManagers systemManagers) 
    {
    }
    public virtual void EndBatch(ISystemManagers systemManagers) 
    { 
    }

    public virtual void PreRender() { }

    public abstract void Render(ISystemManagers managers);


    void IRenderableIpso.SetParentDirect(IRenderableIpso? parent)
    {
        _parent = parent;
    }

    public override string? ToString()
    {
        return Name;
    }
}
