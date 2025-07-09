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
        return (ItemSlot)MainGrid.Children[index];
    }

    partial void CustomInitialize()
    {
    
    }
}
