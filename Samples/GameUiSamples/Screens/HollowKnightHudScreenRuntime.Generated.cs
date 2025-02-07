//Code for HollowKnightHudScreen
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
    public partial class HollowKnightHudScreenRuntime:Gum.Wireframe.BindableGue
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("HollowKnightHudScreen", typeof(HollowKnightHudScreenRuntime));
        }
        public ContainerRuntime MainHudContainer { get; protected set; }
        public SpriteRuntime SpriteInstance { get; protected set; }

        public HollowKnightHudScreenRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {


        }
        public override void AfterFullCreation()
        {
            MainHudContainer = this.GetGraphicalUiElementByName("MainHudContainer") as ContainerRuntime;
            SpriteInstance = this.GetGraphicalUiElementByName("SpriteInstance") as SpriteRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
