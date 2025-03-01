using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameAndGum.DataDefinitions;
internal class StoreInventory
{
    public List<ShopItem> Items { get; private set; } = new List<ShopItem>();


    public StoreInventory()
    {
        Add("Health Potion", 100);
        Add("Mana Potion", 500);
        Add("Revive Potion", 2000);
        Add("Shield", 3000);
        Add("Boots", 1000);

        void Add(string name, int cost)
        {
            Items.Add(new ShopItem
            {
                Name = name,
                Cost = cost
            });
        }
    }
}