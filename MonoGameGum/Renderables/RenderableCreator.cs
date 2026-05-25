using Gum.Wireframe;
using RenderingLibrary.Graphics;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if RAYLIB || SOKOL
namespace Gum.Renderables;
#else
namespace MonoGameGum.Renderables;
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
