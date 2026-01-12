using GameUiSamples.Components;
using GameUiSamples.Data;
using GameUiSamples.Services;
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
        HyTaleInventoryService _inventoryService;

        partial void CustomInitialize()
        {
            _inventoryService = Game1.ServiceContainer.GetService<HyTaleInventoryService>();

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
            FillHotBar();
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

            HyTaleItemDefinition item = _inventoryService.HyTaleInventoryItemDefinitions[itemName];
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
