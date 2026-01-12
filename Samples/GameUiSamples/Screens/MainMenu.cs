using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
using System;
using GameUiSamples.Screens.FrbClicker;
using Gum.Forms.Controls;
using MonoGameGum;
namespace GameUiSamples.Screens;

partial class MainMenu
{
    partial void CustomInitialize()
    {
        ListBoxInstance.SelectedIndex = 0;

        ButtonConfirmInstance.Click += GoToSelectedScreen;
    }

    private void GoToSelectedScreen(object sender, EventArgs e)
    {
        if (ListBoxInstance.SelectedIndex != -1)
        {
            var item = ListBoxInstance.ListBoxItems[ListBoxInstance.SelectedIndex];

            if (item == GameTitleScreenItem)
            {
                GoToScreen(new GameTitleScreen());
            }
            else if (item == GameHudHollowKnight)
            {
                GoToScreen(new HollowKnightHudScreen());
            }
            else if (item == HotbarStardew)
            {
                GoToScreen(new StardewHotbarScreen());
            }
            else if(item == InventoryStardew)
            {
                GoToScreen(new StardewInventoryScreen());
            }
            else if (item == FrbClicker)
            {
                GoToScreen(new FrbClickerCodeOnly());
            }
            else if (item == HotbarHyTale)
            {
                GoToScreen(new HyTaleHotbarScreen());
            }
            else if (item == InventoryHyTale)
            {
                GoToScreen(new StardewInventoryScreen());
            }
        }
    }

    private void GoToScreen(FrameworkElement newScreen)
    {
        GumService.Default.Root.Children.Clear();
        newScreen.AddToRoot();
    }

    private void GoToScreen(GraphicalUiElement newScreen)
    {
        GumService.Default.Root.Children.Clear();
        newScreen.AddToRoot();
    }
}
