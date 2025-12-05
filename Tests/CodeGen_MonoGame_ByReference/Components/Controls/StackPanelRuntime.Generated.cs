//Code for Controls/StackPanel (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGen_MonoGame_ByReference.Components.Controls;
partial class StackPanelRuntime : ContainerRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/StackPanel", typeof(StackPanelRuntime));
    }
    public global::Gum.Forms.Controls.StackPanel FormsControl => FormsControlAsObject as global::Gum.Forms.Controls.StackPanel;

    public StackPanelRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("Controls/StackPanel");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        if (FormsControl == null)
        {
            FormsControlAsObject = new global::Gum.Forms.Controls.StackPanel(this);
        }
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
