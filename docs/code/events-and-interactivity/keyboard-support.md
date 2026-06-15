# Keyboard Support

## Introduction

Gum Forms can use the keyboard to interact with various controls. Controls receive keyboard input if they are focused and if keyboard actions have been enabled. Note that TextBoxes always receive input if they are focused even if keyboards have not been added to FrameworkElement.KeyboardsForUiControl.

For information on tabbing with the keyboard, see the [Tabbing](tabbing-moving-focus.md) page.

## TextBox Input

TextBoxes receive input from the keyboard if they are focused. A TextBox's focus can be set in code by setting IsFocused to true, or through the UI by clicking on the TextBox or by tabbing to the TextBox. For more information see the TextBox page.

## ListBox Navigation

A focused `ListBox` starts at the top level, where it participates in [tabbing](tabbing-moving-focus.md) between controls. Press Enter to move focus into the items; the up and down arrow keys then move the highlighted item, and Enter selects the highlighted item and returns focus to the top level.

To have a `ListBox` start with an item already focused — so the arrow keys work without first pressing Enter — see [Keyboard and Gamepad Navigation](../controls/listbox.md#keyboard-and-gamepad-navigation) on the ListBox page.
