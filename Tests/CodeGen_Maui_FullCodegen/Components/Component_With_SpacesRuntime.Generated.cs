//Code for Component With Spaces (Container)
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGen_Maui_FullCodegen.Components;
partial class Component_With_SpacesRuntime : Gum.GueDeriving.ContainerRuntime
{

    public Component_With_SpacesRuntime(bool fullInstantiation = true)
    {
        if(fullInstantiation)
        {
            this.SetContainedObject(new InvisibleRenderable());
        }


        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
    }
    protected virtual void AssignParents()
    {
    }
    private void ApplyDefaultVariables()
    {
    }
    partial void CustomInitialize();
}
