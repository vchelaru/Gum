//Code for StardewComponents/Hotbar (Container)
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
    public partial class HotbarRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("StardewComponents/Hotbar", typeof(HotbarRuntime));
        }
        public NineSliceRuntime NineSliceInstance { get; protected set; }
        public ItemSlotRuntime ItemSlotInstance1 { get; protected set; }
        public ItemSlotRuntime ItemSlotInstance2 { get; protected set; }
        public ItemSlotRuntime ItemSlotInstance3 { get; protected set; }
        public ItemSlotRuntime ItemSlotInstance4 { get; protected set; }
        public ItemSlotRuntime ItemSlotInstance5 { get; protected set; }
        public ItemSlotRuntime ItemSlotInstance6 { get; protected set; }
        public ItemSlotRuntime ItemSlotInstance7 { get; protected set; }
        public ItemSlotRuntime ItemSlotInstance8 { get; protected set; }
        public ItemSlotRuntime ItemSlotInstance9 { get; protected set; }
        public ContainerRuntime ItemSlotContainer { get; protected set; }

        public HotbarRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {


        }
        public override void AfterFullCreation()
        {
            NineSliceInstance = this.GetGraphicalUiElementByName("NineSliceInstance") as NineSliceRuntime;
            ItemSlotInstance1 = this.GetGraphicalUiElementByName("ItemSlotInstance1") as ItemSlotRuntime;
            ItemSlotInstance2 = this.GetGraphicalUiElementByName("ItemSlotInstance2") as ItemSlotRuntime;
            ItemSlotInstance3 = this.GetGraphicalUiElementByName("ItemSlotInstance3") as ItemSlotRuntime;
            ItemSlotInstance4 = this.GetGraphicalUiElementByName("ItemSlotInstance4") as ItemSlotRuntime;
            ItemSlotInstance5 = this.GetGraphicalUiElementByName("ItemSlotInstance5") as ItemSlotRuntime;
            ItemSlotInstance6 = this.GetGraphicalUiElementByName("ItemSlotInstance6") as ItemSlotRuntime;
            ItemSlotInstance7 = this.GetGraphicalUiElementByName("ItemSlotInstance7") as ItemSlotRuntime;
            ItemSlotInstance8 = this.GetGraphicalUiElementByName("ItemSlotInstance8") as ItemSlotRuntime;
            ItemSlotInstance9 = this.GetGraphicalUiElementByName("ItemSlotInstance9") as ItemSlotRuntime;
            ItemSlotContainer = this.GetGraphicalUiElementByName("ItemSlotContainer") as ContainerRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
