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
namespace GameUiSamples.Screens;

partial class StardewInventoryScreen : IUpdateScreen
{
    private InventoryService _inventoryService;

    ItemSlot _grabbedItemSlot;

    int? inventoryIndexGrabbed = null;

    partial void CustomInitialize()
    {
        _inventoryService = Game1.ServiceContainer.GetService<InventoryService>();

        _grabbedItemSlot = new ItemSlot();
        _grabbedItemSlot.IsVisible = false;
        GumService.Default.PopupRoot.AddChild(_grabbedItemSlot);

        InitializeZoom();

        InitializePlayerInventory();

        UpdateToInventory();
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
                var definition = _inventoryService.InventoryItems[item];
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
        System.Diagnostics.Debug.WriteLine(cursor.WindowOver);
        if(cursor.PrimaryPush)
        {
            // Future versions of Gum should inclue a Cursor.FrameworkElementPushed, but until
            // then we have to cast.
            var visualPushed = cursor.WindowPushed as InteractiveGue;
            var itemSlotPushed = visualPushed?.FormsControlAsObject as ItemSlot;

            if(itemSlotPushed != null)
            {
                _grabbedItemSlot.IsVisible = true;
                _grabbedItemSlot.ItemIconInstance.TextureLeft = itemSlotPushed.ItemIconInstance.TextureLeft;
                _grabbedItemSlot.ItemIconInstance.TextureTop = itemSlotPushed.ItemIconInstance.TextureTop;
            }
        }

        if(cursor.PrimaryDown && _grabbedItemSlot.IsVisible)
        {
            _grabbedItemSlot.X = cursor.X;
            _grabbedItemSlot.Y = cursor.Y;
        }
    }
}


