using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GameUiSamples.Components
{
    partial class HyTaleInventory
    {
        partial void CustomInitialize()
        {
        
        }

        public HyTaleItemSlot GetItemSlotByIndex(int index)
        {
            if (index > 35)
            {
                return (HyTaleItemSlot)HyTaleHotbarRowInstance.HotBarRowContainer.Children[index - 36];
            }

            //var stackPanelName = index % 9;
            var row = (index / 9) + 1;

            if (row == 1)
            {
                return (HyTaleItemSlot)InventoryRow1.Children[index];
            }
            else if (row == 2)
            {
                return (HyTaleItemSlot)InventoryRow2.Children[index - 9];
            }
            else if (row == 3)
            {
                return (HyTaleItemSlot)InventoryRow3.Children[index - 18];
            }
            else if (row == 4)
            {
                return (HyTaleItemSlot)InventoryRow4.Children[index - 27];
            }

            return null;
        }
    }
}
