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
                GoToScreen(new GameTitleScreenRuntime());
            }
            else if (item == GameHudHollowKnight.FormsControlAsObject)
            {
                GoToScreen(new HollowKnightHudScreenRuntime());
            }
            else if(item == HotbarStardew.FormsControlAsObject)
            {
                GoToScreen(new StardewHotbarScreenRuntime());
            }
            else if(item == FrbClicker.FormsControlAsObject)
            {
                GoToScreen(new FrbClickerCodeOnly());
            }
        }
    }

    private void GoToScreen(GraphicalUiElement newScreen)
    {
        Game1.Root.RemoveFromManagers();
        newScreen.AddToManagers();
        Game1.Root = newScreen;
    }
}
