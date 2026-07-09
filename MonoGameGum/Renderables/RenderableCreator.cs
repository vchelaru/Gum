#if MONOGAME || KNI || FNA
#define XNALIKE
#endif
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if XNALIKE
namespace MonoGameGum.Renderables;
#else
namespace Gum.Renderables;
#endif
public static class RenderableCreator
{
    public static IRenderable HandleCreateGraphicalComponent(string type, ISystemManagers systemManagers)
    {

        IRenderable containedObject = null;

        containedObject = FallbackRenderableFactory.TryHandleAsBaseType(type, systemManagers as SystemManagers);


        // todo - have a custom method...

        return containedObject;
    }
}
