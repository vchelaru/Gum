using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameUiSamples.Data
{
    public enum HytaleItemCatergories
    {
        Weapon,
        Tool,
        CraftingBench,
        Block,
        Ore,
        Ingot,
        Food,
        Container, 
        Item
    }

    public class HyTaleItemDefinition
    {
        public string Name { get; set; }
        public Vector2 TextureTopLeft { get; set; }
        public HytaleItemCatergories ItemCatgegory { get; set; }

        public HyTaleItemDefinition(string name, int top, int left, HytaleItemCatergories category)
        {
            Name = name;
            TextureTopLeft = new Vector2(left, top);
            ItemCatgegory = category;
        }

        public HyTaleItemDefinition(string name, Vector2 topLeft, HytaleItemCatergories category)
        {
            Name = name;
            TextureTopLeft = topLeft;
            ItemCatgegory = category;
        }

    }
}
