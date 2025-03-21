using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using GameUiSamples.Components;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
using System.ComponentModel.DataAnnotations;
using MonoGameGum.Forms;
using MonoGameGum.Forms.Controls;
using System;
using GumRuntime;
using RenderingLibrary;
using Microsoft.Xna.Framework;
using MonoGameGum;
namespace GameUiSamples.Screens;

partial class GameTitleScreenRuntime : Gum.Wireframe.BindableGue, IUpdateScreen
{

    partial void CustomInitialize()
    {
        Player1Button.FormsControl.IsFocused = true;

        // Note that some setup is being performed
        // in TitleScreenButtonRuntime.CustomInitialize.
        // See that file for code that affects the behavior
        // of TitleScreenButtons.

        ExitButton.FormsControl.Click += HandleExitClicked;


        FrameworkElement.GamePadsForUiControl.Clear();
        FrameworkElement.GamePadsForUiControl.AddRange(FormsUtilities.Gamepads);
    }

    private void HandleExitClicked(object sender, EventArgs e)
    {
        GumService.Default.Root.Children.Clear();
        var mainMenu = new MainMenuRuntime();
        mainMenu.AddToRoot();
    }

    public void Update(GameTime gameTime)
    {

    }
}
