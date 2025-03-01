using Gum.Mvvm;
using MonoGameAndGum.DataDefinitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameAndGum.ViewModels;
internal class ShopItemViewModel : ViewModel
{
    public string Name 
    {
        get => Get<string>();
        set => Set(value);
    }
    public int Cost
    {
        get => Get<int>();
        set => Set(value);
    }


    [DependsOn(nameof(Cost))]
    public string CostDisplay => Cost.ToString("N0");

    public void SetFrom(ShopItem shopItem)
    {
        Name = shopItem.Name;
        Cost = shopItem.Cost;
    }

    public override string ToString()
    {
        return $"{Name}...{Cost}";
    }
}
