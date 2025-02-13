using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using GameUiSamples.Components;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
using MonoGameGum.Forms.Controls;
using System;
using MonoGameGum.Forms;
namespace GameUiSamples.Components
{
    partial class TitleScreenButtonRuntime : ContainerRuntime
    {
        partial void CustomInitialize()
        {
            FormsControl.Click += (_, _) => FormsControl.IsFocused = true;

            FormsControl.KeyDown += HandleKeyDown;

            // We could update which gamepad button is considered a "click":
            // For example to set it to B instead of A:
            //FormsControl.ClickGamepadButton = Microsoft.Xna.Framework.Input.Buttons.B;
        }

        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Microsoft.Xna.Framework.Input.Keys.Up:
                    FormsControl.HandleTab(TabDirection.Up, loop:true);
                    break;
                case Microsoft.Xna.Framework.Input.Keys.Down:
                    FormsControl.HandleTab(TabDirection.Down, loop:true);
                    break;
                case Microsoft.Xna.Framework.Input.Keys.Enter:
                    FormsControl.PerformClick(FormsUtilities.Keyboard);
                    break;
            }
        }
    }
}
