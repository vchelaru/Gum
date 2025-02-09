//Code for StardewComponents/ItemIcon (Container)
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
    public partial class ItemIconRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("StardewComponents/ItemIcon", typeof(ItemIconRuntime));
        }
        public SpriteRuntime SpriteInstance { get; protected set; }

        public int TextureLeft
        {
            get => SpriteInstance.TextureLeft;
            set => SpriteInstance.TextureLeft = value;
        }

        public int TextureTop
        {
            get => SpriteInstance.TextureTop;
            set => SpriteInstance.TextureTop = value;
        }

        public ItemIconRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {


        }
        public override void AfterFullCreation()
        {
            SpriteInstance = this.GetGraphicalUiElementByName("SpriteInstance") as SpriteRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
