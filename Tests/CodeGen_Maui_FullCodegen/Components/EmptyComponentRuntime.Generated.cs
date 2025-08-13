//Code for EmptyComponent (Container)
using GumRuntime;
using System.Linq;
using SkiaGum.GueDeriving;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

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
