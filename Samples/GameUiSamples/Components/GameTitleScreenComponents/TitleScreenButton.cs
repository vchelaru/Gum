using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
using MonoGameGum.Forms.Controls;
using MonoGameGum.Forms;
using MonoGameGum;
namespace GameUiSamples.Components;

partial class TitleScreenButton
{
    partial void CustomInitialize()
    {
        Click += (_, _) => IsFocused = true;
        KeyDown += HandleKeyDown;

    }

    private void HandleKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Microsoft.Xna.Framework.Input.Keys.Up:
                HandleTab(TabDirection.Up, loop: true);
                break;
            case Microsoft.Xna.Framework.Input.Keys.Down:
                HandleTab(TabDirection.Down, loop: true);
                break;
            case Microsoft.Xna.Framework.Input.Keys.Enter:
                PerformClick(GumService.Default.Keyboard);
                break;
        }
    }
}
