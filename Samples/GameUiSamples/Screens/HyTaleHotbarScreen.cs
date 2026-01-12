using GameUiSamples.Components;
using GameUiSamples.Data;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
namespace GameUiSamples.Screens
{
    partial class HyTaleHotbarScreen : IUpdateScreen
    {
        Dictionary<string, HyTaleItem> _itemIconsPositions;

        partial void CustomInitialize()
        {
            this.ExitButton.Click += (_, _) =>
            {
                GumService.Default.Root.Children.Clear();
                var mainMenu = new MainMenu();
                mainMenu.AddToRoot();
            };

            StatusInfo.Text = "Click below or press the number keys";
            this.HyTaleHotBarInstance.SelectedIndexChanged += (_, _) =>
            {
                var slotItem = (HyTaleItemSlot)HyTaleHotBarInstance.HyTaleHotbarRowInstance.HotBarRowContainer.Children[HyTaleHotBarInstance.SelectedIndex];

                StatusInfo.Text = $"Selected index {HyTaleHotBarInstance.SelectedIndex}\n@ {DateTime.Now}\n{slotItem.Quantity} {slotItem.ItemName}";
            };

            SetupItemIconPositions();

            FillHotBar();

        }


        private void SetupItemIconPositions()
        {
            _itemIconsPositions = new Dictionary<string, HyTaleItem>();

            // Row 1 spritesheet
            AddItemIcon("Sword", new Vector2(0, 96), HytaleItemCatergories.Weapon);
            AddItemIcon("Sword2", new Vector2(96 * 1, 96), HytaleItemCatergories.Weapon);
            AddItemIcon("BattleAxe", new Vector2(96 * 2, 96), HytaleItemCatergories.Weapon);
            AddItemIcon("Mace", new Vector2(96 * 3, 96), HytaleItemCatergories.Weapon);
            AddItemIcon("Long Hammer", new Vector2(96 * 4, 96), HytaleItemCatergories.Weapon);
            AddItemIcon("Bow", new Vector2(96 * 5, 96), HytaleItemCatergories.Weapon);
            AddItemIcon("Quiver", new Vector2(96 * 6, 96), HytaleItemCatergories.Weapon);
            AddItemIcon("Arrow", new Vector2(96 * 7, 96), HytaleItemCatergories.Item);

            // Row 2 spritesheet
            AddItemIcon("Axe", new Vector2(0, 96 * 2), HytaleItemCatergories.Tool);
            AddItemIcon("Pickaxe", new Vector2(96 * 1, 96 * 2), HytaleItemCatergories.Tool);
            AddItemIcon("Shovel", new Vector2(96 * 2, 96 * 2), HytaleItemCatergories.Tool);
            AddItemIcon("Hoe", new Vector2(96 * 3, 96 * 2), HytaleItemCatergories.Tool);
            AddItemIcon("Hammer", new Vector2(96 * 4, 96 * 2), HytaleItemCatergories.Tool);
            AddItemIcon("Chisel", new Vector2(96 * 5, 96 * 2), HytaleItemCatergories.Tool);
            AddItemIcon("Sickle", new Vector2(96 * 6, 96 * 2), HytaleItemCatergories.Tool);
            AddItemIcon("Workbench", new Vector2(96 * 7, 96 * 2), HytaleItemCatergories.CraftingBench);
            AddItemIcon("Anvil", new Vector2(96 * 8, 96 * 2), HytaleItemCatergories.CraftingBench);
            AddItemIcon("Grinder", new Vector2(96 * 9, 96 * 2), HytaleItemCatergories.CraftingBench);


            // Row 3 spritesheet
            AddItemIcon("Boards", new Vector2(0, 96 * 3), HytaleItemCatergories.Item);
            AddItemIcon("Twigs", new Vector2(96 * 1, 96 * 3), HytaleItemCatergories.Item);
            AddItemIcon("Hide", new Vector2(96 * 2, 96 * 3), HytaleItemCatergories.Item);
            AddItemIcon("Rope", new Vector2(96 * 3, 96 * 3), HytaleItemCatergories.Item);
            AddItemIcon("Coal", new Vector2(96 * 4, 96 * 3), HytaleItemCatergories.Ore);
            AddItemIcon("Sulfur", new Vector2(96 * 5, 96 * 3), HytaleItemCatergories.Ore);
            AddItemIcon("IronOre", new Vector2(96 * 6, 96 * 3), HytaleItemCatergories.Ore);
            AddItemIcon("GoldOre", new Vector2(96 * 7, 96 * 3), HytaleItemCatergories.Ore);
            AddItemIcon("IronDust", new Vector2(96 * 8, 96 * 3), HytaleItemCatergories.Item);
            AddItemIcon("GoldDust", new Vector2(96 * 9, 96 * 3), HytaleItemCatergories.Item);

            // Row 4 spritesheet
            AddItemIcon("Radish", new Vector2(0, 96 * 4), HytaleItemCatergories.Food);
            AddItemIcon("Potato", new Vector2(96 * 1, 96 * 4), HytaleItemCatergories.Food);
            AddItemIcon("Eggplant", new Vector2(96 * 2, 96 * 4), HytaleItemCatergories.Food);
            AddItemIcon("Carrot", new Vector2(96 * 3, 96 * 4), HytaleItemCatergories.Food);
            AddItemIcon("Mushroom Red", new Vector2(96 * 4, 96 * 4), HytaleItemCatergories.Food);
            AddItemIcon("Mushroom Brown", new Vector2(96 * 5, 96 * 4), HytaleItemCatergories.Food);
            AddItemIcon("Hay", new Vector2(96 * 6, 96 * 4), HytaleItemCatergories.Food);
            AddItemIcon("Meat", new Vector2(96 * 7, 96 * 4), HytaleItemCatergories.Food);
            AddItemIcon("Fish", new Vector2(96 * 8, 96 * 4), HytaleItemCatergories.Food);
            AddItemIcon("Bread", new Vector2(96 * 9, 96 * 4), HytaleItemCatergories.Food);

            // Row 5 spritesheet
            AddItemIcon("IronBlock", new Vector2(0, 96 * 5), HytaleItemCatergories.Block);
            AddItemIcon("EmeraldBlock", new Vector2(96 * 1, 96 * 5), HytaleItemCatergories.Block);
            AddItemIcon("DiamondBlock", new Vector2(96 * 2, 96 * 5), HytaleItemCatergories.Block);
            AddItemIcon("TanzaniteBlock", new Vector2(96 * 3, 96 * 5), HytaleItemCatergories.Block);
            AddItemIcon("Lapis lazuli", new Vector2(96 * 4, 96 * 5), HytaleItemCatergories.Ore);
            AddItemIcon("Emerald", new Vector2(96 * 5, 96 * 5), HytaleItemCatergories.Ore);
            AddItemIcon("Sapphire", new Vector2(96 * 6, 96 * 5), HytaleItemCatergories.Ore);
            AddItemIcon("Ruby", new Vector2(96 * 7, 96 * 5), HytaleItemCatergories.Ore);

            // Row 6 spritesheet
            AddItemIcon("IronIngot", new Vector2(0, 96 * 6), HytaleItemCatergories.Ingot);
            AddItemIcon("GoldIngot", new Vector2(96 * 1, 96 * 6), HytaleItemCatergories.Ingot);

            // Row 7 spiresheet
            AddItemIcon("Crate", new Vector2(0, 96 * 7), HytaleItemCatergories.Container);
            AddItemIcon("Chest", new Vector2(96 * 1, 96 * 7), HytaleItemCatergories.Container);
            AddItemIcon("Barrel", new Vector2(96 * 2, 96 * 7), HytaleItemCatergories.Container);
            AddItemIcon("Bag", new Vector2(96 * 3, 96 * 7), HytaleItemCatergories.Container);
            AddItemIcon("Bone", new Vector2(96 * 4, 96 * 7), HytaleItemCatergories.Item);
        }

        private void AddItemIcon(string name, Vector2 topLeft, HytaleItemCatergories category)
        {
            _itemIconsPositions.Add(name, new HyTaleItem(name, topLeft, category));
        }

        private void FillHotBar()
        {
            AssignIcon(HyTaleHotBarInstance.HyTaleHotbarRowInstance.HotBarItem1, "Pickaxe", 1, 75);
            AssignIcon(HyTaleHotBarInstance.HyTaleHotbarRowInstance.HotBarItem2, "Axe", 1, 100);
            AssignIcon(HyTaleHotBarInstance.HyTaleHotbarRowInstance.HotBarItem3, "Sword", 1, 25);
            AssignIcon(HyTaleHotBarInstance.HyTaleHotbarRowInstance.HotBarItem4, "Bow", 1, 92);
            AssignIcon(HyTaleHotBarInstance.HyTaleHotbarRowInstance.HotBarItem5, "IronIngot", 64);
            AssignIcon(HyTaleHotBarInstance.HyTaleHotbarRowInstance.HotBarItem6, "Boards", 64);
            AssignIcon(HyTaleHotBarInstance.HyTaleHotbarRowInstance.HotBarItem7, "Meat", 5);
            AssignIcon(HyTaleHotBarInstance.HyTaleHotbarRowInstance.HotBarItem8);
            AssignIcon(HyTaleHotBarInstance.HyTaleHotbarRowInstance.HotBarItem9, "Bread", 23);

        }

        private void AssignIcon(HyTaleItemSlot slot, string itemName = null, int quantity = 1, int durabilityLeft = 100)
        {
            slot.IsHotBarItemState = HyTaleItemSlot.IsHotBarItem.True;

            if (String.IsNullOrEmpty(itemName))
            {
                slot.HasItemState = HyTaleItemSlot.HasItem.False;
                slot.HasDamageState = HyTaleItemSlot.HasDamage.False;
                slot.HasMoreThanOneState = HyTaleItemSlot.HasMoreThanOne.False;
                slot.Quantity = "";
                return;
            }

            slot.HasItemState = HyTaleItemSlot.HasItem.True;
            slot.HasDamageState = HyTaleItemSlot.HasDamage.False;
            slot.HasMoreThanOneState = HyTaleItemSlot.HasMoreThanOne.False;

            HyTaleItem item = _itemIconsPositions[itemName];
            if (item != null)
            {
                slot.ItemName = itemName;

                slot.ItemStartX = (int)item.TextureTopLeft.X;
                slot.ItemStartY = (int)item.TextureTopLeft.Y;

                if (item.ItemCatgegory == HytaleItemCatergories.Weapon || item.ItemCatgegory == HytaleItemCatergories.Tool)
                {
                    if (durabilityLeft < 100)
                    {
                        slot.HasDamageState = HyTaleItemSlot.HasDamage.True;
                        slot.DurabilityRatio = durabilityLeft;
                    }
                    else
                    {
                        slot.HasDamageState = HyTaleItemSlot.HasDamage.False;
                        slot.DurabilityRatio = 100;
                    }
                }

                if (quantity > 1)
                {
                    slot.HasMoreThanOneState = HyTaleItemSlot.HasMoreThanOne.True;
                    slot.Quantity = quantity.ToString();
                }
                else
                {
                    slot.Quantity = "";
                }
            }

        }

        public void Update(GameTime gameTime)
        {

            HyTaleHotBarInstance.HandleKeyboardInput();
        }
    }
}
