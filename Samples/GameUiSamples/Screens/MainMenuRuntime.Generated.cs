//Code for MainMenu
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using GameUiSamples.Components;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GameUiSamples.Screens
{
    public partial class MainMenuRuntime:Gum.Wireframe.BindableGue
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("MainMenu", typeof(MainMenuRuntime));
        }
        public ListBoxRuntime ListBoxInstance { get; protected set; }
        public ButtonConfirmRuntime ButtonConfirmInstance { get; protected set; }
        public ListBoxItemRuntime GameTitleScreenItem { get; protected set; }
        public ListBoxItemRuntime GameHudHollowKnight { get; protected set; }
        public ListBoxItemRuntime HotbarStardew { get; protected set; }

        public MainMenuRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {


        }
        public override void AfterFullCreation()
        {
            ListBoxInstance = this.GetGraphicalUiElementByName("ListBoxInstance") as ListBoxRuntime;
            ButtonConfirmInstance = this.GetGraphicalUiElementByName("ButtonConfirmInstance") as ButtonConfirmRuntime;
            GameTitleScreenItem = this.GetGraphicalUiElementByName("GameTitleScreenItem") as ListBoxItemRuntime;
            GameHudHollowKnight = this.GetGraphicalUiElementByName("GameHudHollowKnight") as ListBoxItemRuntime;
            HotbarStardew = this.GetGraphicalUiElementByName("HotbarStardew") as ListBoxItemRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
