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
    public static IRenderable? HandleCreateGraphicalComponent(string type, ISystemManagers systemManagers)
    {
        return FallbackRenderableFactory.TryHandleAsBaseType(type, systemManagers as SystemManagers);
    }
}
