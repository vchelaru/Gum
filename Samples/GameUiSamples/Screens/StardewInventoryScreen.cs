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
namespace GameUiSamples.Screens;

partial class StardewInventoryScreen : IUpdateScreen
{
    private InventoryService _inventoryService;

    ItemIcon _grabbedIcon;

    partial void CustomInitialize()
    {
        _inventoryService = Game1.ServiceContainer.GetService<InventoryService>();

        CreateGrabbedIcon();

        GumService.Default.PopupRoot.AddChild(_grabbedIcon);

        InitializeZoom();

        InitializePlayerInventory();

        InitializeInventoryItems();

        UpdateToInventory();
    }

    private void CreateGrabbedIcon()
    {
        _grabbedIcon = new ItemIcon();
        _grabbedIcon.IsVisible = false;
        _grabbedIcon.Name = "Grabbed icon";
        // So that it doesn't register as the cursor being over it:
        _grabbedIcon.Visual.HasEvents = false;

        _grabbedIcon.Visual.XOrigin = HorizontalAlignment.Center;
        _grabbedIcon.Visual.YOrigin = VerticalAlignment.Center;
    }

    private void InitializeInventoryItems()
    {
        foreach(ItemSlot item in InventoryGridInstance.MainGrid.Children)
        {
            item.Visual.Push += HandleInventoryItemPushed;

            item.Visual.RemovedAsPushed += HandleInventoryItemRemovedAsPushed;
        }
    }

    private void HandleInventoryItemPushed(object? sender, EventArgs e)
    {
        var visualPushed = sender as InteractiveGue;
        var itemSlotPushed = visualPushed?.FormsControlAsObject as ItemSlot;

        if (itemSlotPushed != null)
        {
            var index = GetItemSlotIndex(itemSlotPushed);

            if(index >= 0 && index < _inventoryService.PlayerInventory.Length)
            {
                var item = _inventoryService.PlayerInventory[index];
                if(item != null)
                {
                    itemSlotPushed.ItemIconInstance.IsVisible = false;
                    _grabbedIcon.IsVisible = true;
                    _grabbedIcon.TextureLeft = itemSlotPushed.ItemIconInstance.TextureLeft;
                    _grabbedIcon.TextureTop = itemSlotPushed.ItemIconInstance.TextureTop;
                }
            }

        }
    }

    private void HandleInventoryItemRemovedAsPushed(object? sender, EventArgs e)
    {
        var visualPushed = sender as InteractiveGue;
        var itemSlotPushed = visualPushed?.FormsControlAsObject as ItemSlot;

        var didSwap = false;

        if (itemSlotPushed != null)
        {
            _grabbedIcon.IsVisible = false;

            var visualOver = GumService.Default.Cursor.WindowOver;

            var itemSlotDropped = visualOver?.FormsControlAsObject as ItemSlot;

            if(itemSlotDropped != null)
            {
                var oldInventoryType = itemSlotPushed.Visual.Tag as string;
                var newInventoryType = itemSlotDropped.Visual.Tag as string;
                var oldIndex = GetItemSlotIndex(itemSlotPushed);
                var newIndex = GetItemSlotIndex(itemSlotDropped);

                // swap them in the inventory:
                var inventory = _inventoryService.PlayerInventory;

                inventory[oldIndex] = newInventoryType;
                inventory[newIndex] = oldInventoryType;

            }
        }
        UpdateToInventory();
    }

    int GetItemSlotIndex(ItemSlot itemSlot)
    {
        return InventoryGridInstance.MainGrid.Children.IndexOf(itemSlot);
    }

    private void InitializePlayerInventory()
    {
        var inventory = _inventoryService.PlayerInventory;
        // clear the inventory:
        for (int i = 0; i < inventory.Length; i++)
        {
            inventory[i] = null;
        }

        inventory[0] = "Apple";
        inventory[1] = "Meat";
        inventory[2] = "Key";
        inventory[3] = "Topaz";
        inventory[4] = "Book";
        inventory[5] = "Fish";

    }

    private void UpdateToInventory()
    {
        var inventory = _inventoryService.PlayerInventory;

        for (int i = 0; i < inventory.Length; i++)
        {
            var item = inventory[i];

            var itemSlot = this.InventoryGridInstance.GetItemSlotByIndex(i);

            itemSlot.Visual.Tag = item;

            if(item == null)
            {
                itemSlot.ItemIconInstance.IsVisible = false;
            }
            else
            {
                var definition = _inventoryService.InventoryItemDefinitions[item];
                itemSlot.ItemIconInstance.IsVisible = true;
                itemSlot.ItemIconInstance.TextureLeft = definition.PixelLeft;
                itemSlot.ItemIconInstance.TextureTop = definition.PixelTop;
            }
        }
    }

    private void InitializeZoom()
    {
        var zoom = 3f;
        SetZoom(zoom);

        this.Visual.UpdateLayout();

    }

    private static void SetZoom(float zoom)
    {
        SystemManagers.Default.Renderer.Camera.Zoom = zoom;
        GraphicalUiElement.CanvasWidth = SystemManagers.Default.Renderer.Camera.ClientWidth / zoom;
        GraphicalUiElement.CanvasHeight = SystemManagers.Default.Renderer.Camera.ClientHeight / zoom;
    }

    public void Update(GameTime gameTime)
    {
        var cursor = GumService.Default.Cursor;

        if(cursor.PrimaryDown && _grabbedIcon.IsVisible)
        {
            _grabbedIcon.X = cursor.XRespectingGumZoomAndBounds();
            _grabbedIcon.Y = cursor.YRespectingGumZoomAndBounds(); 
        }

    }
}


