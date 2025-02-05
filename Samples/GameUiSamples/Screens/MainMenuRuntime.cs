using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
using MonoGameGum.Forms.Controls;
using System;
namespace GameUiSamples.Screens;

partial class MainMenuRuntime : Gum.Wireframe.BindableGue
{
    ListBox listBox;
    partial void CustomInitialize()
    {
        listBox = (ListBox)ListBoxInstance.FormsControlAsObject;
        //listBox.SelectedIndex = 0;

        var button = (Button)ButtonConfirmInstance.FormsControlAsObject;
        button.Click += GoToSelectedScreen;

    }

    private void GoToSelectedScreen(object sender, EventArgs e)
    {
        if(listBox.SelectedIndex != -1)
        {
            var item = listBox.ListBoxItems[listBox.SelectedIndex];

            if(item == GameTitleScreenItem.FormsControlAsObject)
            {
                // go to screen
            }
        }
    }
}
