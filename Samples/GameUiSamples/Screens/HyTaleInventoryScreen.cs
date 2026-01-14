using GameUiSamples.Components;
using GameUiSamples.Services;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameGum;
using RenderingLibrary.Graphics;
using System;
using System.Linq;
using GameUiSamples.Data;

// for the troubleshooting
using MonoGameGum.Input;

namespace GameUiSamples.Screens
{
    partial class HyTaleInventoryScreen : IUpdateScreen
    {
        private static readonly Random _random = new Random();

        HyTaleInventoryService _inventoryService;

        HyTaleItemSlot _grabbedIcon;

        //GumService GumUI => GumService.Default;

        partial void CustomInitialize()
        {
            _inventoryService = Game1.ServiceContainer.GetService<HyTaleInventoryService>();

            CreateGrabbedIcon();

            GumService.Default.PopupRoot.AddChild(_grabbedIcon);

            InitializePlayerInventory();

            FillHotBar();

            InitializeInventoryItems();

            UpdateToInventory();
        }

        private void CreateGrabbedIcon()
        {
            _grabbedIcon = new HyTaleItemSlot();
            _grabbedIcon.IsVisible = false;
            _grabbedIcon.Name = "Grabbed icon";
            // So that it doesn't register as the cursor being over it:
            _grabbedIcon.Visual.HasEvents = false;

            _grabbedIcon.Visual.XOrigin = HorizontalAlignment.Center;
            _grabbedIcon.Visual.YOrigin = VerticalAlignment.Center;
        }


        private void InitializePlayerInventory()
        {
            var inventory = _inventoryService.PlayerInventory;
            // clear the inventory:
            for (int i = 0; i < inventory.Length; i++)
            {
                inventory[i] = null;
            }

            // Populate some random items
            for (int i = 0; i< 23; i++)
            {
                inventory[i] = PickRandomItemFromDictionary();
            }
        }

        private HyTaleItem PickRandomItemFromDictionary()
        {
            // No item in this slot
            if (_random.Next(10) < 3)
                return null;

            var item = _inventoryService.HyTaleInventoryItemDefinitions.ElementAt(_random.Next(_inventoryService.HyTaleInventoryItemDefinitions.Count));
            HyTaleItemDefinition itemDef = item.Value;

            return new HyTaleItem(itemDef.Name, _random.Next(64), _random.Next(100));
        }

        private void FillHotBar()
        {
            AssignIconAndUpdateInventory(HyTaleInventoryInstance.HyTaleHotbarRowInstance.HotBarItem1, "Pickaxe", 1, 75);
            AssignIconAndUpdateInventory(HyTaleInventoryInstance.HyTaleHotbarRowInstance.HotBarItem2, "Axe", 1, 100);
            AssignIconAndUpdateInventory(HyTaleInventoryInstance.HyTaleHotbarRowInstance.HotBarItem3, "Sword", 1, 25);
            AssignIconAndUpdateInventory(HyTaleInventoryInstance.HyTaleHotbarRowInstance.HotBarItem4, "Bow", 1, 92);
            AssignIconAndUpdateInventory(HyTaleInventoryInstance.HyTaleHotbarRowInstance.HotBarItem5, "IronIngot", 64);
            AssignIconAndUpdateInventory(HyTaleInventoryInstance.HyTaleHotbarRowInstance.HotBarItem6, "Boards", 64);
            AssignIconAndUpdateInventory(HyTaleInventoryInstance.HyTaleHotbarRowInstance.HotBarItem7, "Meat", 5);
            AssignIconAndUpdateInventory(HyTaleInventoryInstance.HyTaleHotbarRowInstance.HotBarItem8);
            AssignIconAndUpdateInventory(HyTaleInventoryInstance.HyTaleHotbarRowInstance.HotBarItem9, "Bread", 23);

        }

        private void AssignIconAndUpdateInventory(HyTaleItemSlot slot, string itemName = null, int quantity = 1, int durabilityLeft = 100)
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

            // Add the item to the actual inventory
            HyTaleItem item = new HyTaleItem(itemName, quantity, durabilityLeft);
            int inventoryIndex = GetItemSlotIndex(slot);
            _inventoryService.PlayerInventory[inventoryIndex] = item;

            AssignIcon(slot, item);
        }


        private void AssignIcon(HyTaleItemSlot slot, HyTaleItem item)
        {
            if (item is null)
            {
                slot.HasItemState = HyTaleItemSlot.HasItem.False;
                slot.HasDamageState = HyTaleItemSlot.HasDamage.False;
                slot.HasMoreThanOneState = HyTaleItemSlot.HasMoreThanOne.False;
                slot.Quantity = "";
                return;
            }

            // Setup the visual for the item
            slot.HasItemState = HyTaleItemSlot.HasItem.True;
            slot.HasDamageState = HyTaleItemSlot.HasDamage.False;
            slot.HasMoreThanOneState = HyTaleItemSlot.HasMoreThanOne.False;

            HyTaleItemDefinition itemDefinition = _inventoryService.HyTaleInventoryItemDefinitions[item.Name];
            if (itemDefinition != null)
            {
                slot.ItemName = item.Name;

                slot.ItemStartX = (int)itemDefinition.TextureTopLeft.X;
                slot.ItemStartY = (int)itemDefinition.TextureTopLeft.Y;

                if (itemDefinition.ItemCatgegory == HytaleItemCatergories.Weapon || itemDefinition.ItemCatgegory == HytaleItemCatergories.Tool)
                {
                    if (item.Durability < 100)
                    {
                        slot.HasDamageState = HyTaleItemSlot.HasDamage.True;
                        slot.DurabilityRatio = item.Durability;

                        //Set to green
                        slot.HyTaleDurabilityBarInstance.BarPercent.Color = new Color(41, 142, 68);

                        if (item.Durability < 5)
                        {
                            slot.HyTaleDurabilityBarInstance.BarPercent.Color = Color.Red;
                        }
                        else if (item.Durability < 25)
                        {
                            slot.HyTaleDurabilityBarInstance.BarPercent.Color = Color.Orange;
                        }
                        else if (item.Durability < 50)
                        {
                            slot.HyTaleDurabilityBarInstance.BarPercent.Color = Color.Yellow;
                        }
                    }
                    else
                    {
                        slot.HasDamageState = HyTaleItemSlot.HasDamage.False;
                        slot.DurabilityRatio = 100;
                    }
                }
                else
                {
                    if (item.Quantity > 1)
                    {
                        slot.HasMoreThanOneState = HyTaleItemSlot.HasMoreThanOne.True;
                        slot.Quantity = item.Quantity.ToString();
                    }
                    else
                    {
                        slot.Quantity = "";
                    }
                }
            }
        }

        private void HideSlotItem(HyTaleItemSlot slot)
        {
            slot.HyTaleItemIconInstance.IsVisible = false;
            slot.HyTaleDurabilityBarInstance.IsVisible = false;
            slot.QuantityValueText.Visible = false;

            if (slot.HasItemState == HyTaleItemSlot.HasItem.True)
            {
                slot.ItemStartX = 0;
            }
            else
            {
                slot.ItemStartX = 96;
            }
        }

        private void UnhideSlotItem(HyTaleItemSlot slot)
        {
            slot.HyTaleItemIconInstance.IsVisible = true;
            slot.QuantityValueText.Visible = true;
            slot.HyTaleDurabilityBarInstance.IsVisible = true;

            if (slot.HasItemState == HyTaleItemSlot.HasItem.True)
            {
                slot.ItemStartX = 96;
            }
            else
            {
                slot.ItemStartX = 0;
            }
        }

        private void InitializeInventoryItems()
        {
            foreach (var container in HyTaleInventoryInstance.MainGrid.Children)
            {
                StackPanel stackPanel;

                if (container is HyTaleHotbarRow)
                {
                    stackPanel = ((HyTaleHotbarRow)container).HotBarRowContainer;
                } 
                else //if (container is StackPanel)
                {
                    stackPanel = (StackPanel)container;
                }

                foreach (HyTaleItemSlot item in (stackPanel).Children)
                {
                    item.Visual.Push += HandleInventoryItemPushed;

                    item.Visual.RemovedAsPushed += HandleInventoryItemRemovedAsPushed;
                }
            }
        }

        private void HandleInventoryItemPushed(object? sender, EventArgs e)
        {
            var visualPushed = sender as InteractiveGue;
            var itemSlotPushed = visualPushed?.FormsControlAsObject as HyTaleItemSlot;
            
            if (itemSlotPushed != null)
            {
                var index = GetItemSlotIndex(itemSlotPushed);

                if (index >= 0 && index < _inventoryService.PlayerInventory.Length)
                {
                    var item = _inventoryService.PlayerInventory[index];
                    if (item != null)
                    {
                        HideSlotItem(itemSlotPushed);

                        AssignIcon(_grabbedIcon, item);
                        _grabbedIcon.IsVisible = true;
                        _grabbedIcon.IsHotBarItemState = HyTaleItemSlot.IsHotBarItem.False;
                        _grabbedIcon.HyTaleItemSlotBackground.Visible = false;

                        //_grabbedIcon.ItemStartX = itemSlotPushed.ItemStartX;
                        //_grabbedIcon.ItemStartY = itemSlotPushed.ItemStartY;
                    }
                }
            }
        }

        private void HandleInventoryItemRemovedAsPushed(object? sender, EventArgs e)
        {
            var visualPushed = sender as InteractiveGue;
            var itemSlotPushed = visualPushed?.FormsControlAsObject as HyTaleItemSlot;

            var didSwap = false;

            if (itemSlotPushed != null)
            {
                _grabbedIcon.IsVisible = false;

                var visualOver = GumService.Default.Cursor.VisualOver;

                var itemSlotDropped = visualOver?.FormsControlAsObject as HyTaleItemSlot;

                if (itemSlotDropped != null)
                {
                    var oldIndex = GetItemSlotIndex(itemSlotPushed);
                    var newIndex = GetItemSlotIndex(itemSlotDropped);

                    // swap them in the inventory:
                    var inventory = _inventoryService.PlayerInventory;

                    var oldItem = inventory[oldIndex];
                    inventory[oldIndex] = inventory[newIndex];
                    inventory[newIndex] = oldItem;

                }
            }
            UpdateToInventory();
        }

        int GetItemSlotIndex(HyTaleItemSlot itemSlot)
        {
            // Assume hotbar starting values
            // We're using 1 giant array for all items, so we'll just make the Hotbar items be 36, 37, 38, etc.
            string parentName = itemSlot.ParentFrameworkElement.Name;
            var slotNamePrefix = "HotBarItem";
            int slotIndexOffset = 35;

            // The normal inventory items will be 0, 1, 2, etc
            if (parentName.Contains("InventoryRow"))
            {
                slotIndexOffset = -1;
                slotNamePrefix = "HyTaleItemSlotInstance";
            }

            string numbersLeftover = itemSlot.Name.Replace(slotNamePrefix, "");
            int slotIndex;
            int.TryParse(numbersLeftover, out slotIndex);

            return slotIndex + slotIndexOffset;

        }


        private void UpdateToInventory()
        {
            var inventory = _inventoryService.PlayerInventory;

            for (int i = 0; i < inventory.Length; i++)
            {
                var item = inventory[i];

                var itemSlot = this.HyTaleInventoryInstance.GetItemSlotByIndex(i);

                if (item == null)
                {
                    itemSlot.HasItemState = HyTaleItemSlot.HasItem.False;
                }
                else
                {
                    AssignIcon(itemSlot, item);
                    var definition = _inventoryService.HyTaleInventoryItemDefinitions[item.Name];
                    //itemSlot.ItemIconInstance.IsVisible = true;
                    //itemSlot.ItemIconInstance.TextureLeft = definition.PixelLeft;
                    //itemSlot.ItemIconInstance.TextureTop = definition.PixelTop;
                }
            }
        }

        public void Update(GameTime gameTime)
        {
            var cursor = GumService.Default.Cursor;

            if (cursor.PrimaryDown && _grabbedIcon.IsVisible)
            {
                _grabbedIcon.X = cursor.XRespectingGumZoomAndBounds();
                _grabbedIcon.Y = cursor.YRespectingGumZoomAndBounds();
            }

            //var failureReason = GumUI.Cursor.GetEventFailureReason(HyTaleInventoryInstance.HyTaleHotbarRowInstance.HotBarItem1);
            //System.Diagnostics.Debug.WriteLine(failureReason);

        }
    }
}
