//Code for MainScreen
using GumRuntime;
using FnaSample.Components;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;
using MonoGameGum.GueDeriving;

namespace FnaSample.Screens
{
    public partial class MainScreenRuntime:Gum.Wireframe.BindableGue
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("MainScreen", typeof(MainScreenRuntime));
        }
        public ButtonStandardRuntime ButtonStandardInstance { get; protected set; }
        public ContainerRuntime ContainerInstance { get; protected set; }
        public TextBoxRuntime TextBoxInstance { get; protected set; }

        public MainScreenRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
                var element = ObjectFinder.Self.GetElementSave("MainScreen");
                element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
            }



        }
        public override void AfterFullCreation()
        {
            ButtonStandardInstance = this.GetGraphicalUiElementByName("ButtonStandardInstance") as ButtonStandardRuntime;
            ContainerInstance = this.GetGraphicalUiElementByName("ContainerInstance") as ContainerRuntime;
            TextBoxInstance = this.GetGraphicalUiElementByName("TextBoxInstance") as TextBoxRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
