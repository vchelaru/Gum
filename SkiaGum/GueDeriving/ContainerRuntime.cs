using Gum.Wireframe;
using SkiaGum.Renderables;

namespace SkiaGum.GueDeriving
{
    public class ContainerRuntime : BindableGue
    {
        public ContainerRuntime(bool fullInstantiation = true)
        {
            if(fullInstantiation)
            {
                SetContainedObject(new RenderableBase());
                Width = 150;
                Height = 150;
            }
        }

    }
}
