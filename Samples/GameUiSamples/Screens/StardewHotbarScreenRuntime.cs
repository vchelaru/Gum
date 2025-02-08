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
namespace GameUiSamples.Screens
{
    partial class StardewHotbarScreenRuntime : Gum.Wireframe.BindableGue, IUpdateScreen
    {

        partial void CustomInitialize()
        {
            var zoom = 3f;
            SystemManagers.Default.Renderer.Camera.Zoom = zoom;
            GraphicalUiElement.CanvasWidth = SystemManagers.Default.Renderer.Camera.ClientWidth / zoom;
            GraphicalUiElement.CanvasHeight = SystemManagers.Default.Renderer.Camera.ClientHeight / zoom;

            this.UpdateLayout();
        }

        public void Update(GameTime gameTime)
        {
            var keyboard = FormsUtilities.Keyboard;
            int? indexToSelect = null;
            if (keyboard.KeyPushed(Keys.D1)) indexToSelect = 0;
            if (keyboard.KeyPushed(Keys.D2)) indexToSelect = 1;
            if (keyboard.KeyPushed(Keys.D3)) indexToSelect = 2;
            if (keyboard.KeyPushed(Keys.D4)) indexToSelect = 3;
            if (keyboard.KeyPushed(Keys.D5)) indexToSelect = 4;
            if (keyboard.KeyPushed(Keys.D6)) indexToSelect = 5;
            if (keyboard.KeyPushed(Keys.D7)) indexToSelect = 6;
            if (keyboard.KeyPushed(Keys.D8)) indexToSelect = 7;
            if (keyboard.KeyPushed(Keys.D9)) indexToSelect = 8;

            if(indexToSelect != null)
            {
                SelectIndex(indexToSelect.Value);
            }
        }

        private void SelectIndex(int value)
        {
        }
    }
}
