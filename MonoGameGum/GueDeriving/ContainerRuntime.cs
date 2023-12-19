using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.GueDeriving
{
    public class ContainerRuntime : global::Gum.Wireframe.GraphicalUiElement
    {
        public ContainerRuntime(bool fullInstantiation = true)
        {
            if (fullInstantiation)
            {
                SetContainedObject(new InvisibleRenderable());
                Width = 150;
                Height = 150;
                Visible = true;
            }
        }

        public void AddToManagers() => base.AddToManagers(SystemManagers.Default, layer: null);
        
    }
}
