//Code for KeyboardScreenGum
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
namespace GumFormsSample.Screens;
partial class KeyboardScreenGumRuntime : Gum.Wireframe.BindableGue
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("KeyboardScreenGum", typeof(KeyboardScreenGumRuntime));
    }
    public KeyboardRuntime KeyboardInstance { get; protected set; }
    public TextBoxRuntime TextBoxInstance { get; protected set; }

    public KeyboardScreenGumRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("KeyboardScreenGum");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        KeyboardInstance = this.GetGraphicalUiElementByName("KeyboardInstance") as GumFormsSample.Components.KeyboardRuntime;
        TextBoxInstance = this.GetGraphicalUiElementByName("TextBoxInstance") as GumFormsSample.Components.TextBoxRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
