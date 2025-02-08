//Code for StardewComponents/ItemSlot (Container)
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
    public partial class ItemSlotRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("StardewComponents/ItemSlot", typeof(ItemSlotRuntime));
        }
        public NineSliceRuntime NineSliceInstance { get; protected set; }
        public ItemIconRuntime ItemIconInstance { get; protected set; }
        public TextRuntime TextInstance { get; protected set; }
        public NineSliceRuntime HighlightNineSlice { get; protected set; }

        public bool IsHighlighted
        {
            get => HighlightNineSlice.Visible;
            set => HighlightNineSlice.Visible = value;
        }

        public string HotkeyText
        {
            get => TextInstance.Text;
            set => TextInstance.Text = value;
        }

        public ItemSlotRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {


        }
        public override void AfterFullCreation()
        {
            NineSliceInstance = this.GetGraphicalUiElementByName("NineSliceInstance") as NineSliceRuntime;
            ItemIconInstance = this.GetGraphicalUiElementByName("ItemIconInstance") as ItemIconRuntime;
            TextInstance = this.GetGraphicalUiElementByName("TextInstance") as TextRuntime;
            HighlightNineSlice = this.GetGraphicalUiElementByName("HighlightNineSlice") as NineSliceRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
