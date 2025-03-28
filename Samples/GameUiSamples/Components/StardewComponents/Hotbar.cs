using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
using System;
using MonoGameGum.Forms;
using MonoGameGum;
using Microsoft.Xna.Framework.Input;
namespace GameUiSamples.Components;

partial class Hotbar
{
    public event EventHandler SelectedIndexChanged;


    int selectedIndex = -1;
    public int SelectedIndex
    {
        get => selectedIndex;
        set
        {
            if (value != selectedIndex)
            {
                selectedIndex = value;
                UpdateToSelectedIndex();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    partial void CustomInitialize()
    {
        foreach (InteractiveGue child in ItemSlotContainer.Children)
        {
            child.Click += HandleItemSlotClicked;
        }
    }


    internal void HandleKeyboardInput()
    {
        var keyboard = GumService.Default.Keyboard;
        int? indexToSelect = null;
        if (keyboard.KeyPushed(Keys.D1)) indexToSelect = 0;
        if (keyboard.KeyPushed(Keys.D2)) indexToSelect = 1;
        if (keyboard.KeyPushed(Keys.D3)) indexToSelect = 2;
        if (keyboard.KeyPushed(Keys.D4)) indexToSelect = 3;
        if (keyboard.KeyPushed(Keys.D5)) indexToSelect = 4;
        if (keyboard.KeyPushed(Keys.D6)) indexToSelect = 5;
        if (keyboard.KeyPushed(Keys.D7)) indexToSelect = 6;
        if (keyboard.KeyPushed(Keys.D8)) indexToSelect = 7;
        if (keyboard.KeyPushed(Keys.D9)) indexToSelect = 8;

        if (indexToSelect != null)
        {
            SelectedIndex = indexToSelect.Value;
        }
    }


    private void HandleItemSlotClicked(object sender, EventArgs args)
    {
        var itemSlot = (ItemSlot)(sender as InteractiveGue).FormsControlAsObject;

        var index = ItemSlotContainer.Children.IndexOf(itemSlot.Visual);
        SelectedIndex = index;
    }

    void UpdateToSelectedIndex()
    {
        for (int i = 0; i < ItemSlotContainer.Children.Count; i++)
        {
            var childGue = (InteractiveGue)ItemSlotContainer.Children[i];
            var slot = (ItemSlot)childGue.FormsControlAsObject;

            slot.IsHighlighted = i == SelectedIndex;
        }
    }
}
