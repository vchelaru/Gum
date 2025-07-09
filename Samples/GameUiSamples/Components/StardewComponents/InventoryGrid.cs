using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
using MonoGameGum.Forms.Controls;
namespace GameUiSamples.Components;

partial class InventoryGrid
{

    public ItemSlot GetItemSlotByIndex(int index)
    {
        var visualAtIndex = (InteractiveGue)MainGrid.Visual.Children[index];
        return (ItemSlot)visualAtIndex.FormsControlAsObject;
    }

    partial void CustomInitialize()
    {
    
    }
}
