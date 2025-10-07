using Gum.Wireframe;
using SkiaGum.Renderables;

namespace SkiaGum.GueDeriving;

public class ContainerRuntime : BindableGue
{
    public ContainerRuntime(bool fullInstantiation = true)
    {
        if(fullInstantiation)
        {
            SetContainedObject(new RenderableShapeBase());
            Width = 150;
            Height = 150;
        }
    }

    public override GraphicalUiElement Clone()
    {
        var toReturn = (ContainerRuntime)base.Clone();

        // no need to set this, the same way we do for other stuff
        //toReturn.mContainedCircle = null;

        return toReturn;
    }


}
