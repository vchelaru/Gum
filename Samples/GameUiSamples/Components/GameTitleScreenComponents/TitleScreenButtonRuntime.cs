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
namespace GameUiSamples.Components
{
    partial class TitleScreenButtonRuntime : ContainerRuntime
    {
        partial void CustomInitialize()
        {
            FormsControl.Click += (_, _) => FormsControl.IsFocused = true;

            FormsControl.KeyDown += HandleKeyDown;
        }

        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Microsoft.Xna.Framework.Input.Keys.Up:
                    FormsControl.HandleTab(TabDirection.Up);
                    break;
                case Microsoft.Xna.Framework.Input.Keys.Down:
                    FormsControl.HandleTab(TabDirection.Down);
                    break;
                case Microsoft.Xna.Framework.Input.Keys.Enter:
                    FormsControl.PerformClick();
                    break;
            }
        }
    }
}
