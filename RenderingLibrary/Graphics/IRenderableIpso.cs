#if SKIA
using SkiaGum.GueDeriving;
using SkiaGum.Renderables;
using SkiaSharp;
#endif
using Gum;
using System.Collections.ObjectModel;

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