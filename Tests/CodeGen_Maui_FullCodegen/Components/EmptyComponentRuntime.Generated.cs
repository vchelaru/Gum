//Code for EmptyComponent (Container)
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using SkiaGum.GueDeriving;
using System.Linq;
namespace CodeGen_Maui_FullCodegen.Components;
partial class EmptyComponentRuntime : SkiaGum.GueDeriving.ContainerRuntime
{

    public EmptyComponentRuntime(bool fullInstantiation = true)
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
