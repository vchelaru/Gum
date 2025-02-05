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
namespace GameUiSamples.Screens;

partial class GameTitleScreenRuntime : Gum.Wireframe.BindableGue, IUpdateScreen
{

    partial void CustomInitialize()
    {
        Player1Button.FormsControl.IsFocused = true;

        Player1Button.FormsControl.Click += HandleButtonClicked;
        Player2Button.FormsControl.Click += HandleButtonClicked;
        OptionsButton.FormsControl.Click += HandleButtonClicked;
        ExitButton.FormsControl.Click += HandleButtonClicked;



        FrameworkElement.GamePadsForUiControl.Clear();
        FrameworkElement.GamePadsForUiControl.AddRange(FormsUtilities.Gamepads);
    }

    private void HandleButtonClicked(object sender, EventArgs e)
    {
        (sender as Button).IsFocused = true;

        if(sender == ExitButton.FormsControl)
        {
            Game1.Root.RemoveFromManagers();
            Game1.Root = ObjectFinder.Self.GetScreen("MainMenu")
                .ToGraphicalUiElement(SystemManagers.Default, addToManagers: true);
        }
    }

    FrameworkElement FocusedElement =>
        (InteractiveGue.CurrentInputReceiver as FrameworkElement);

    public void Update()
    {
        var keyboard = FormsUtilities.Keyboard;

        var currentItem = InteractiveGue.CurrentInputReceiver;

        if(keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Up))
        {
            FocusedElement?.HandleTab(TabDirection.Up);
        }
        else if(keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Down))
        {
            FocusedElement?.HandleTab(TabDirection.Down);
        }
        else if(keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Enter))
        {
            (FocusedElement as Button)?.PerformClick();
        }

    }
}
