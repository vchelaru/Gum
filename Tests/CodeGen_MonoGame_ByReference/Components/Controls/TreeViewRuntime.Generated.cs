//Code for Controls/TreeView (Container)
using CodeGen_MonoGame_ByReference.Components.Controls;
using Gum;
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGen_MonoGame_ByReference.Components.Controls;
partial class TreeViewRuntime : ContainerRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/TreeView", typeof(TreeViewRuntime));
    }
    public NineSliceRuntime Background { get; protected set; }
    public ScrollBarRuntime VerticalScrollBarInstance { get; protected set; }
    public ContainerRuntime ClipContainerInstance { get; protected set; }
    public ContainerRuntime InnerPanelInstance { get; protected set; }
    public NineSliceRuntime FocusedIndicator { get; protected set; }

    public TreeViewRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("Controls/TreeView");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        Background = this.GetGraphicalUiElementByName("Background") as global::Gum.GueDeriving.NineSliceRuntime;
        VerticalScrollBarInstance = this.GetGraphicalUiElementByName("VerticalScrollBarInstance") as CodeGen_MonoGame_ByReference.Components.Controls.ScrollBarRuntime;
        ClipContainerInstance = this.GetGraphicalUiElementByName("ClipContainerInstance") as global::Gum.GueDeriving.ContainerRuntime;
        InnerPanelInstance = this.GetGraphicalUiElementByName("InnerPanelInstance") as global::Gum.GueDeriving.ContainerRuntime;
        FocusedIndicator = this.GetGraphicalUiElementByName("FocusedIndicator") as global::Gum.GueDeriving.NineSliceRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
