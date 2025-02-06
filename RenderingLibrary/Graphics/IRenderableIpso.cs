#if SKIA
using SkiaGum.GueDeriving;
using SkiaGum.Renderables;
using SkiaSharp;
#endif
using Gum;
using System.Collections.ObjectModel;

namespace RenderingLibrary.Graphics
{
    public interface IRenderableIpso : IRenderable, IPositionedSizedObject, IVisible
    {
        bool IsRenderTarget { get; }

        bool ClipsChildren { get;  }
        IRenderableIpso Parent { get; set; }
        ObservableCollection<IRenderableIpso> Children { get; }
        ColorOperation ColorOperation { get; }

        void SetParentDirect(IRenderableIpso newParent);

    }
}
