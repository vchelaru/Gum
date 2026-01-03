#if SKIA
using SkiaGum.GueDeriving;
using SkiaGum.Renderables;
using SkiaSharp;
#endif
using Gum;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace RenderingLibrary.Graphics;

public interface IRenderableIpso : IRenderable, IPositionedSizedObject, IVisible
{
    bool IsRenderTarget { get; }

    int Alpha { get; }
    bool ClipsChildren { get;  }
    new IRenderableIpso? Parent { get; set; }
    ObservableCollection<IRenderableIpso> Children { get; }
    ColorOperation ColorOperation { get; }

    void SetParentDirect(IRenderableIpso? newParent);

}


public static class IRenderableIpsoExtensions
{
    public static bool IsInRenderTargetRecursively(this IRenderableIpso ipso)
    {
        if(ipso.IsRenderTarget)
        {
            return true;
        }
        else if(ipso.Parent != null)
        {
            return ipso.Parent.IsInRenderTargetRecursively();
        }
        else
        {
            return false;
        }
    }
}

public class ObservableCollectionNoReset<T> : ObservableCollection<T>
{
    protected override void ClearItems()
    {
        List<T> removed = new List<T>(this);
        if(this.Count > 0)
        {
            base.ClearItems();
            var args = new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Remove, 
                removed,
                0);
            base.OnCollectionChanged(args);
        }
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Reset)
            base.OnCollectionChanged(e);
    }
}