//Code for GameTitleScreen
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
    public partial class GameTitleScreenRuntime:Gum.Wireframe.BindableGue
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("GameTitleScreen", typeof(GameTitleScreenRuntime));
        }
        public ContainerRuntime ContainerInstance { get; protected set; }
        public TitleScreenButtonRuntime Player1Button { get; protected set; }
        public TitleScreenButtonRuntime Player2Button { get; protected set; }
        public TitleScreenButtonRuntime OptionsButton { get; protected set; }
        public TitleScreenButtonRuntime ExitButton { get; protected set; }
        public TextRuntime TextInstance { get; protected set; }

        public GameTitleScreenRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {


        }
        public override void AfterFullCreation()
        {
            ContainerInstance = this.GetGraphicalUiElementByName("ContainerInstance") as ContainerRuntime;
            Player1Button = this.GetGraphicalUiElementByName("Player1Button") as TitleScreenButtonRuntime;
            Player2Button = this.GetGraphicalUiElementByName("Player2Button") as TitleScreenButtonRuntime;
            OptionsButton = this.GetGraphicalUiElementByName("OptionsButton") as TitleScreenButtonRuntime;
            ExitButton = this.GetGraphicalUiElementByName("ExitButton") as TitleScreenButtonRuntime;
            TextInstance = this.GetGraphicalUiElementByName("TextInstance") as TextRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
