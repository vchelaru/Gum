using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
using MonoGameAndGum.ViewModels;
partial class StoreListBoxItemRuntime : ContainerRuntime
{

    // This will automatically be bound to an instance of ShopItemViewModel

    partial void CustomInitialize()
    {
        this.ItemNameTextInstance.SetBinding(
            nameof(ItemNameTextInstance.Text),
            nameof(ShopItemViewModel.Name));

        this.CostTextInstance.SetBinding(
            nameof(CostTextInstance.Text),
            nameof(ShopItemViewModel.CostDisplay));
    }
}
