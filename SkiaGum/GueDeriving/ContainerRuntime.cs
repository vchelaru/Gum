using Gum.Wireframe;
using SkiaGum.Renderables;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkiaGum.GueDeriving
{
    public class ContainerRuntime : BindableGraphicalUiElement
    {
        public ContainerRuntime(bool fullInstantiation = true)
        {
            if(fullInstantiation)
            {
                SetContainedObject(new RenderableBase());
            }
        }

    }
}
