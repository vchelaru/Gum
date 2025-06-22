using Gum.Mvvm;
using MonoGameAndGum.DataDefinitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameAndGum.ViewModels;
public class InventoryViewModel : ViewModel
{
    public int Count
    {
        get => Get<int>();
        set => Set(value);
    }

    public ShopItem ShopItem
    {
        get => Get<ShopItem>();
        set => Set(value);
    }
}
