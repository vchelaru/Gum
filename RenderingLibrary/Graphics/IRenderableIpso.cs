using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RenderingLibrary.Graphics
{
    public interface IRenderableIpso : IRenderable, IPositionedSizedObject
    {
        bool ClipsChildren { get;  }
        IRenderableIpso Parent { get; set; }
        List<IRenderableIpso> Children { get; }
        void SetParentDirect(IRenderableIpso newParent);

    }
}
