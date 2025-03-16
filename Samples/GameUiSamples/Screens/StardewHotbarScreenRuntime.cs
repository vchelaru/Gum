using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using GameUiSamples.Components;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
using RenderingLibrary;
using Microsoft.Xna.Framework;
using MonoGameGum.Forms;
using Microsoft.Xna.Framework.Input;
using System;
using GumRuntime;
using MonoGameGum;
namespace GameUiSamples.Screens
{
    partial class StardewHotbarScreenRuntime : Gum.Wireframe.BindableGue, IUpdateScreen
    {

        partial void CustomInitialize()
        {
            InitializeZoom();
            InitializeIcons();

            this.ExitButton.FormsControl.Click += (_, _) =>
            {
                SetZoom(1);

                GumService.Default.Root.Children.Clear();
                var mainMenu = new MainMenuRuntime();
                mainMenu.AddToRoot();
            };

            StatusInfo.Text = "Click below or press the number keys";
            this.HotbarInstance.SelectedIndexChanged += (_, _) => 
                StatusInfo.Text = $"Selected index {HotbarInstance.SelectedIndex}\n@ {DateTime.Now}";
        }

        private void InitializeIcons()
        {
            AssignIconByCell(HotbarInstance.ItemSlotInstance1, 1, 1);
            AssignIconByCell(HotbarInstance.ItemSlotInstance2, 2, 8);
            AssignIconByCell(HotbarInstance.ItemSlotInstance3, 3, 6);
            AssignIconByCell(HotbarInstance.ItemSlotInstance4, 3, 8);
            AssignIconByCell(HotbarInstance.ItemSlotInstance5, 4, 8);
            AssignIconByCell(HotbarInstance.ItemSlotInstance6, 7, 5);
            AssignIconByCell(HotbarInstance.ItemSlotInstance7, 8, 3);
            AssignIconByCell(HotbarInstance.ItemSlotInstance8, 11, 3);
            AssignIconByCell(HotbarInstance.ItemSlotInstance9, 3, 1);
        }

        private void InitializeZoom()
        {
            var zoom = 3f;
            SetZoom(zoom);

            this.UpdateLayout();

            // let's fill the bar with a few items. In this case we'll just assign the top and left coordinates
            // but a real game may have items which have their own texture coordinates
        }

        private static void SetZoom(float zoom)
        {
            SystemManagers.Default.Renderer.Camera.Zoom = zoom;
            GraphicalUiElement.CanvasWidth = SystemManagers.Default.Renderer.Camera.ClientWidth / zoom;
            GraphicalUiElement.CanvasHeight = SystemManagers.Default.Renderer.Camera.ClientHeight / zoom;
        }

        private void AssignIconByCell(ItemSlotRuntime itemSlot, int x, int y)
        {
            var textureLeft = x * 16;
            var textureTop = y * 16;

            itemSlot.ItemIconInstance.TextureLeft = textureLeft;
            itemSlot.ItemIconInstance.TextureTop = textureTop;
        }

        public void Update(GameTime gameTime)
        {

            HotbarInstance.HandleKeyboardInput();
        }

    }
}
