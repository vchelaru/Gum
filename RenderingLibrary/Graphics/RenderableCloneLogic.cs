using Gum.Wireframe;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace RenderingLibrary.Graphics;

public static class RenderableCloneLogic
{
    public static IRenderable Clone(IRenderable original)
    {
        IRenderable clonedRenderable = null;
        switch (original)
        {
            case SolidRectangle solidRectangle:
                clonedRenderable = solidRectangle.Clone();
                break;
            case NineSlice nineSlice:
                clonedRenderable = nineSlice.Clone();
                break;
            default:
                throw new NotImplementedException(
                    $"Cannot clone the object because the renderable type {original.GetType()} is not currently cloneable. Please file an issue on github or mention this on discord");
                //break;
        }


        return clonedRenderable;

    }
}
