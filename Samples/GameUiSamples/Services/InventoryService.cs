using GameUiSamples.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameUiSamples.Services;
public class InventoryService
{
    Dictionary<string, InventoryItemDefinition> _inventoryItemDefinitions = new ();
    public IReadOnlyDictionary<string, InventoryItemDefinition> InventoryItemDefinitions => _inventoryItemDefinitions;

    // For now assume only 1 item in inventory. This could eventually support a count too
    public string?[] PlayerInventory { get; private set; } = new string?[36];

    public InventoryService()
    {
        _inventoryItemDefinitions = new ()
        {
            {"Key", new InventoryItemDefinition { Name = "Key", PixelLeft = 0*16, PixelTop = 0*16 } },
            {"CopperBar", new InventoryItemDefinition { Name = "CopperBar", PixelLeft = 1*16, PixelTop = 1*16 } },
            {"Apple", new InventoryItemDefinition { Name = "Apple", PixelLeft = 2*16, PixelTop = 8*16 } },
            {"SilverOre", new InventoryItemDefinition { Name = "SilverOre", PixelLeft = 3*16, PixelTop = 1*16 } },
            {"Scroll", new InventoryItemDefinition { Name = "Scroll", PixelLeft = 3*16, PixelTop = 6*16 } },
            {"Meat", new InventoryItemDefinition { Name = "Meat", PixelLeft = 3*16, PixelTop = 8*16 } },
            {"Fish", new InventoryItemDefinition { Name = "Fish", PixelLeft = 4*16, PixelTop = 8*16 } },
            {"HealthPotion", new InventoryItemDefinition { Name = "HealthPotion", PixelLeft = 7*16, PixelTop = 5*16 } },
            {"Topaz", new InventoryItemDefinition { Name = "Topaz", PixelLeft = 8*16, PixelTop = 3*16 } },
            {"Book",  new InventoryItemDefinition { Name = "Book", PixelLeft = 11*16, PixelTop = 3*16 } },
            // Add more items as needed
        };
    }
}
