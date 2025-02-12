using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
using MonoGameGum.Forms.Controls;
using System;
using System.ComponentModel.Design.Serialization;
using GumRuntime;
using RenderingLibrary;
using GameUiSamples.Screens.FrbClicker;
namespace GameUiSamples.Screens;

partial class MainMenuRuntime : Gum.Wireframe.BindableGue
{
    ListBox listBox;
    partial void CustomInitialize()
    {
        listBox = (ListBox)ListBoxInstance.FormsControlAsObject;
        listBox.SelectedIndex = 0;

        var button = (Button)ButtonConfirmInstance.FormsControlAsObject;
        button.Click += GoToSelectedScreen;

    }

    private void GoToSelectedScreen(object sender, EventArgs e)
    {
        if (listBox.SelectedIndex != -1)
        {
            var item = listBox.ListBoxItems[listBox.SelectedIndex];

            if (item == GameTitleScreenItem.FormsControlAsObject)
            {
                GoToScreen("GameTitleScreen");
            }
            else if (item == GameHudHollowKnight.FormsControlAsObject)
            {
                GoToScreen("HollowKnightHudScreen");
            }
            else if(item == HotbarStardew.FormsControlAsObject)
            {
                GoToScreen("StardewHotbarScreen");
            }
            else if(item == FrbClicker.FormsControlAsObject)
            {
                Game1.Root.RemoveFromManagers();
                var screen = new FrbClickerCodeOnly();
                screen.AddToManagers();
                Game1.Root = screen;
            }
        }
    }

    private void GoToScreen(string screenName)
    {
        Game1.Root.RemoveFromManagers();
        Game1.Root = ObjectFinder.Self.GetScreen(screenName)
            .ToGraphicalUiElement(SystemManagers.Default, addToManagers: true);
    }
}
