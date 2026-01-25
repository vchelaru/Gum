using GameUiSamples.Data;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using MonoGameGum.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Xml.Linq;


namespace GameUiSamples.Components
{
    partial class HyTaleHotBar
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

            foreach (FrameworkElement child in HyTaleHotbarRowInstance.HotBarRowContainer.Children)
            {
                child.Visual.Click += HandleItemSlotClicked;
            }

            SetupHotkeyNumbers();
        }


        // We do this in Gum Tool, but we could have also done it here
        private void SetupHotkeyNumbers()
        {
            for (int i = 0; i < HyTaleHotbarRowInstance.HotBarRowContainer.Children.Count; i++)
            {
                var slot = (HyTaleItemSlot)HyTaleHotbarRowInstance.HotBarRowContainer.Children[i];

                slot.HotBarSlotNumber = (i + 1).ToString();
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
            var frameworkEement = (sender as Gum.Wireframe.InteractiveGue).FormsControlAsObject;
            var index = HyTaleHotbarRowInstance.HotBarRowContainer.Children.IndexOf(frameworkEement);
            SelectedIndex = index;
        }

        void UpdateToSelectedIndex()
        {
            for (int i = 0; i < HyTaleHotbarRowInstance.HotBarRowContainer.Children.Count; i++)
            {
                var slot = (HyTaleItemSlot)HyTaleHotbarRowInstance.HotBarRowContainer.Children[i];

                slot.IsHighlighted = i == SelectedIndex;
            }
        }
    }
}
