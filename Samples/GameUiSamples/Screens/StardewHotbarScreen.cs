using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
using MonoGameGum;
using RenderingLibrary;
using GameUiSamples.Components;
using Microsoft.Xna.Framework;
using System;
using GameUiSamples.Services;
using MonoGameGum.Forms.Controls;
namespace GameUiSamples.Screens;

partial class StardewHotbarScreen : IUpdateScreen
{
    private InventoryService _inventoryService;

    partial void CustomInitialize()
    {
        _inventoryService = Game1.ServiceContainer.GetService<InventoryService>();
        InitializeZoom();
        InitializeIcons();

        this.ExitButton.Click += (_, _) =>
        {
            SetZoom(1);

            GumService.Default.Root.Children.Clear();
            var mainMenu = new MainMenu();
            mainMenu.AddToRoot();
        };

        StatusInfo.Text = "Click below or press the number keys";
        this.HotbarInstance.SelectedIndexChanged += (_, _) =>
            StatusInfo.Text = $"Selected index {HotbarInstance.SelectedIndex}\n@ {DateTime.Now}";
    }


    private void InitializeIcons()
    {

        // let's fill the bar with a few items. In this case we'll just assign the top and left coordinates
        // but a real game may have items which have their own texture coordinates
        AssignIcon(HotbarInstance.ItemSlotInstance1, "CopperBar"); // Copper Bar
        AssignIcon(HotbarInstance.ItemSlotInstance2, "Apple"); // Apple
        AssignIcon(HotbarInstance.ItemSlotInstance3, "Scroll"); // Scroll
        AssignIcon(HotbarInstance.ItemSlotInstance4, "Meat"); // Meat
        AssignIcon(HotbarInstance.ItemSlotInstance5, "Fish"); // Fish
        AssignIcon(HotbarInstance.ItemSlotInstance6, "HealthPotion"); // HealthPotion
        AssignIcon(HotbarInstance.ItemSlotInstance7, "Topaz"); // Topaz
        AssignIcon(HotbarInstance.ItemSlotInstance8, "Book"); // Book
        AssignIcon(HotbarInstance.ItemSlotInstance9, "SilverOre"); // Silver Ore
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

    private void AssignIcon(ItemSlot itemSlot, string itemName)
    {
        var definition = _inventoryService.InventoryItemDefinitions[itemName];

        itemSlot.ItemIconInstance.TextureTop = definition.PixelTop;

        itemSlot.ItemIconInstance.TextureLeft = definition.PixelLeft;
    }

    public void Update(GameTime gameTime)
    {

        HotbarInstance.HandleKeyboardInput();
    }

}
