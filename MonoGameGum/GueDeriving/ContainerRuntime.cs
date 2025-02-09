using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.GueDeriving
{
    public class ContainerRuntime : InteractiveGue
    {
        public float Alpha
        {
            get => (RenderableComponent as InvisibleRenderable)?.Alpha ?? 255;
            set
            {
                if (RenderableComponent is InvisibleRenderable invisibleRenderable)
                {
                    invisibleRenderable.Alpha = value;
                }
            }
        }

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
