//Code for HollowKnightComponents/ManaOrb (Container)
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using GameUiSamples.Components;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GameUiSamples.Components
{
    public partial class ManaOrbRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("HollowKnightComponents/ManaOrb", typeof(ManaOrbRuntime));
        }
        public SpriteRuntime SpriteInstance { get; protected set; }
        public ContainerRuntime ContainerInstance { get; protected set; }
        public SpriteRuntime SpriteInstance2 { get; protected set; }
        public SpriteRuntime WaveMaskSprite { get; protected set; }
        public ColoredRectangleRuntime ColoredRectangleInstance { get; protected set; }

        public ManaOrbRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {


        }
        public override void AfterFullCreation()
        {
            SpriteInstance = this.GetGraphicalUiElementByName("SpriteInstance") as SpriteRuntime;
            ContainerInstance = this.GetGraphicalUiElementByName("ContainerInstance") as ContainerRuntime;
            SpriteInstance2 = this.GetGraphicalUiElementByName("SpriteInstance2") as SpriteRuntime;
            WaveMaskSprite = this.GetGraphicalUiElementByName("WaveMaskSprite") as SpriteRuntime;
            ColoredRectangleInstance = this.GetGraphicalUiElementByName("ColoredRectangleInstance") as ColoredRectangleRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
