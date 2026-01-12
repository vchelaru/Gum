using GameUiSamples.Components;
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
using System.Linq;


using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
using RenderingLibrary;
using System;
using GameUiSamples.Services;
using Microsoft.Xna.Framework;
using MonoGameGum;
using GameUiSamples.Components;
using MonoGameGum.Forms;
using MonoGameGum.ExtensionMethods;
using GameUiSamples.Data;

namespace GameUiSamples.Screens
{
    partial class HyTaleInventoryScreen : IUpdateScreen
    {
        private static readonly Random _random = new Random();

        HyTaleInventoryService _inventoryService;

        HyTaleItemIcon _grabbedIcon;

        partial void CustomInitialize()
        {
            _inventoryService = Game1.ServiceContainer.GetService<HyTaleInventoryService>();

            CreateGrabbedIcon();

            GumService.Default.PopupRoot.AddChild(_grabbedIcon);

            InitializePlayerInventory();

            //InitializeInventoryItems();

            //UpdateToInventory();
        }

        private void CreateGrabbedIcon()
        {
            _grabbedIcon = new HyTaleItemIcon();
            _grabbedIcon.IsVisible = false;
            _grabbedIcon.Name = "Grabbed icon";
            // So that it doesn't register as the cursor being over it:
            _grabbedIcon.Visual.HasEvents = false;

            _grabbedIcon.Visual.XOrigin = HorizontalAlignment.Center;
            _grabbedIcon.Visual.YOrigin = VerticalAlignment.Center;
        }


        //private void InitializeInventoryItems()
        //{
        //    foreach (var stackPanel in HyTaleInventoryInstance.ContainerInstance2.Children)
        //    {
        //        if (stackPanel is StackPanel)
        //        {
        //            foreach (HyTaleItemSlot item in ((StackPanel)stackPanel).Children)
        //            {
        //                item.Visual.Push += HandleInventoryItemPushed;

        //                item.Visual.RemovedAsPushed += HandleInventoryItemRemovedAsPushed;
        //            }
        //        }
        //    }
        //}

        //private void HandleInventoryItemPushed(object? sender, EventArgs e)
        //{
        //    var visualPushed = sender as InteractiveGue;
        //    var itemSlotPushed = visualPushed?.FormsControlAsObject as HyTaleItemSlot;

        //    if (itemSlotPushed != null)
        //    {
        //        var index = GetItemSlotIndex(itemSlotPushed);

        //        if (index >= 0 && index < _inventoryService.PlayerInventory.Length)
        //        {
        //            var item = _inventoryService.PlayerInventory[index];
        //            if (item != null)
        //            {
        //                itemSlotPushed.HyTaleItemIconInstance.IsVisible = false;
        //                _grabbedIcon.IsVisible = true;
        //                _grabbedIcon.ItemStartX = itemSlotPushed.ItemStartX;
        //                _grabbedIcon.ItemStartY = itemSlotPushed.ItemStartY;
        //            }
        //        }

        //    }
        //}

        //private void HandleInventoryItemRemovedAsPushed(object? sender, EventArgs e)
        //{
        //    var visualPushed = sender as InteractiveGue;
        //    var itemSlotPushed = visualPushed?.FormsControlAsObject as HyTaleItemSlot;

        //    var didSwap = false;

        //    if (itemSlotPushed != null)
        //    {
        //        _grabbedIcon.IsVisible = false;

        //        var visualOver = GumService.Default.Cursor.VisualOver;

        //        var itemSlotDropped = visualOver?.FormsControlAsObject as HyTaleItemSlot;

        //        if (itemSlotDropped != null)
        //        {
        //            var oldInventoryType = itemSlotPushed.Visual.Tag as string;
        //            var newInventoryType = itemSlotDropped.Visual.Tag as string;
        //            var oldIndex = GetItemSlotIndex(itemSlotPushed);
        //            var newIndex = GetItemSlotIndex(itemSlotDropped);

        //            // swap them in the inventory:
        //            var inventory = _inventoryService.PlayerInventory;

        //            inventory[oldIndex] = newInventoryType;
        //            inventory[newIndex] = oldInventoryType;

        //        }
        //    }
        //    UpdateToInventory();
        //}

        //int GetItemSlotIndex(HyTaleItemSlot itemSlot)
        //{
        //    // Made this hard for myself by having rows but the spacing for GRID was off

        //    //return InventoryGridInstance.MainGrid.Children.IndexOf(itemSlot);

        //    if (itemSlot.Name

        //}

        private void InitializePlayerInventory()
        {
            var inventory = _inventoryService.PlayerInventory;
            // clear the inventory:
            for (int i = 0; i < inventory.Length; i++)
            {
                inventory[i] = null;
            }

            inventory[0] = PickRandomItemFromDictionary();
            inventory[1] = PickRandomItemFromDictionary();
            inventory[2] = PickRandomItemFromDictionary();
            inventory[3] = PickRandomItemFromDictionary();
            inventory[4] = PickRandomItemFromDictionary();
            inventory[5] = PickRandomItemFromDictionary();

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

        //private void UpdateToInventory()
        //{
        //    var inventory = _inventoryService.PlayerInventory;

        //    for (int i = 0; i < inventory.Length; i++)
        //    {
        //        var item = inventory[i];

        //        var itemSlot = this.InventoryGridInstance.GetItemSlotByIndex(i);

        //        itemSlot.Visual.Tag = item;

        //        if (item == null)
        //        {
        //            itemSlot.ItemIconInstance.IsVisible = false;
        //        }
        //        else
        //        {
        //            var definition = _inventoryService.InventoryItemDefinitions[item];
        //            itemSlot.ItemIconInstance.IsVisible = true;
        //            itemSlot.ItemIconInstance.TextureLeft = definition.PixelLeft;
        //            itemSlot.ItemIconInstance.TextureTop = definition.PixelTop;
        //        }
        //    }
        //}

        public void Update(GameTime gameTime)
        {
            var cursor = GumService.Default.Cursor;

            if (cursor.PrimaryDown && _grabbedIcon.IsVisible)
            {
                _grabbedIcon.X = cursor.XRespectingGumZoomAndBounds();
                _grabbedIcon.Y = cursor.YRespectingGumZoomAndBounds();
            }

        }
    }
}
