//Code for StardewHotbarScreen
using GumRuntime;
using GameUiSamples.Components;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GameUiSamples.Screens
{
    public partial class StardewHotbarScreenRuntime:Gum.Wireframe.BindableGue
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("StardewHotbarScreen", typeof(StardewHotbarScreenRuntime));
        }
        public HotbarRuntime HotbarInstance { get; protected set; }
        public ButtonStandardRuntime ExitButton { get; protected set; }
        public TextRuntime StatusInfo { get; protected set; }

        public StardewHotbarScreenRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
                var element = ObjectFinder.Self.GetElementSave("StardewHotbarScreen");
                element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
            }



        }
        public override void AfterFullCreation()
        {
            HotbarInstance = this.GetGraphicalUiElementByName("HotbarInstance") as HotbarRuntime;
            ExitButton = this.GetGraphicalUiElementByName("ExitButton") as ButtonStandardRuntime;
            StatusInfo = this.GetGraphicalUiElementByName("StatusInfo") as TextRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
