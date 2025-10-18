using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
using Microsoft.Xna.Framework;
using MonoGameGum;
using Gum.Forms.Controls;
using System;
using Gum.Forms;
namespace GameUiSamples.Screens;

partial class GameTitleScreen
{
    partial void CustomInitialize()
    {
        Player1Button.IsFocused = true;

        ExitButton.Click += HandleExitClicked;

        FrameworkElement.GamePadsForUiControl.Clear();
        FrameworkElement.GamePadsForUiControl.AddRange(FormsUtilities.Gamepads);
    }

    private void HandleExitClicked(object? sender, EventArgs e)
    {
        GumService.Default.Root.Children.Clear();
        var mainMenu = new MainMenu();
        mainMenu.AddToRoot();
    }

}
