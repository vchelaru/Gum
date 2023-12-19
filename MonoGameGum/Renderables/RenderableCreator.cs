using Gum.Wireframe;
using RenderingLibrary.Graphics;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Renderables
{
    public static class RenderableCreator
    {
        public static IRenderable HandleCreateGraphicalComponent(string type, ISystemManagers systemManagers)
        {

            IRenderable containedObject = null;

            containedObject = RuntimeObjectCreator.TryHandleAsBaseType(type, systemManagers as SystemManagers);


            // todo - have a custom method...

            return containedObject;
        }
    }
}
