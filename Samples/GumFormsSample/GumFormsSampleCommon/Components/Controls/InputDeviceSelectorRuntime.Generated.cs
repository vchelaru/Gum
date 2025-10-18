//Code for Controls/InputDeviceSelector (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using GumFormsSample.Components;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GumFormsSample.Components;
partial class InputDeviceSelectorRuntime : ContainerRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/InputDeviceSelector", typeof(InputDeviceSelectorRuntime));
    }
    public NineSliceRuntime Background { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public TextRuntime TextInstance1 { get; protected set; }
    public ContainerRuntime ContainerInstance1 { get; protected set; }
    public ContainerRuntime InputDeviceContainerInstance { get; protected set; }
    public ContainerRuntime ContainerInstance2 { get; protected set; }
    public InputDeviceSelectionItemRuntime InputDeviceSelectionItemInstance { get; protected set; }
    public InputDeviceSelectionItemRuntime InputDeviceSelectionItemInstance1 { get; protected set; }
    public InputDeviceSelectionItemRuntime InputDeviceSelectionItemInstance2 { get; protected set; }
    public InputDeviceSelectionItemRuntime InputDeviceSelectionItemInstance3 { get; protected set; }

    public InputDeviceSelectorRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("Controls/InputDeviceSelector");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        Background = this.GetGraphicalUiElementByName("Background") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        TextInstance = this.GetGraphicalUiElementByName("TextInstance") as global::MonoGameGum.GueDeriving.TextRuntime;
        TextInstance1 = this.GetGraphicalUiElementByName("TextInstance1") as global::MonoGameGum.GueDeriving.TextRuntime;
        ContainerInstance1 = this.GetGraphicalUiElementByName("ContainerInstance1") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        InputDeviceContainerInstance = this.GetGraphicalUiElementByName("InputDeviceContainerInstance") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        ContainerInstance2 = this.GetGraphicalUiElementByName("ContainerInstance2") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        InputDeviceSelectionItemInstance = this.GetGraphicalUiElementByName("InputDeviceSelectionItemInstance") as GumFormsSample.Components.InputDeviceSelectionItemRuntime;
        InputDeviceSelectionItemInstance1 = this.GetGraphicalUiElementByName("InputDeviceSelectionItemInstance1") as GumFormsSample.Components.InputDeviceSelectionItemRuntime;
        InputDeviceSelectionItemInstance2 = this.GetGraphicalUiElementByName("InputDeviceSelectionItemInstance2") as GumFormsSample.Components.InputDeviceSelectionItemRuntime;
        InputDeviceSelectionItemInstance3 = this.GetGraphicalUiElementByName("InputDeviceSelectionItemInstance3") as GumFormsSample.Components.InputDeviceSelectionItemRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
