using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Gum;
using Gum.Forms.Controls;
using System;
using Gum.Forms;
namespace GameUiSamples.Screens;

partial class GameTitleScreen
{
    GumService GumUI => GumService.Default;

    partial void CustomInitialize()
    {
        Player1Button.IsFocused = true;

        ExitButton.Click += HandleExitClicked;
    }

    private void HandleExitClicked(object? sender, EventArgs e)
    {
        GumUI.Root.Children.Clear();
        var mainMenu = new MainMenu();
        mainMenu.AddToRoot();
    }

}
