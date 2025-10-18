//Code for Controls/MessageBox (Controls/UserControl)
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
partial class MessageBoxRuntime : UserControlRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/MessageBox", typeof(MessageBoxRuntime));
    }
    public LabelRuntime LabelInstance { get; protected set; }
    public ContainerRuntime ContainerInstance { get; protected set; }
    public ButtonConfirmRuntime OkButton { get; protected set; }
    public ButtonDenyRuntime CancelButton { get; protected set; }

    public MessageBoxRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("Controls/MessageBox");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        base.AfterFullCreation();
        LabelInstance = this.GetGraphicalUiElementByName("LabelInstance") as GumFormsSample.Components.LabelRuntime;
        ContainerInstance = this.GetGraphicalUiElementByName("ContainerInstance") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        OkButton = this.GetGraphicalUiElementByName("OkButton") as GumFormsSample.Components.ButtonConfirmRuntime;
        CancelButton = this.GetGraphicalUiElementByName("CancelButton") as GumFormsSample.Components.ButtonDenyRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
