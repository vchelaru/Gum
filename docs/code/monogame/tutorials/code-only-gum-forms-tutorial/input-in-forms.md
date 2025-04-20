# Input in Forms

## Introduction

Gum supports reading input from the mouse, touch screen, keyboard, and gamepads. Some types of input is automatically enabled such as clicks on Buttons. Other types of input must be enabled through code, such as giving controls focus for tabbing.

This tutorial covers the various ways input can be used to interact with forms objects.

## Automatic Behavior

By default Gum controls work with the mouse and touch screen. The mouse automatically highlights all controls when it moves over the visible area of a control. Furthermore, the following events and actions also happen automatically:

* Button
  * Click event
* CheckBox
  * Click event
  * Checked event
  * Unchecked event
* ComboBox
  * Expand/Collapse
  * Item selection&#x20;
  * SelectionChanged event
* ListBox
  * Item selection
  * SelectionChanged event
  * ItemClicked
  * ItemPushed
  * Scroll on mouse wheel
* Menu
  * Item selection
  * Item expansion
  * Selected (MenuItem)
  * Clicked (MenuItem)
* RadioButton
  * Click event
  * Checked event
  * Unchecked event
* Slider
  * Drag thumb to change value
  * Click on track to change value
  * ValueChanged, ValueChangeCompleted, ValueChangedByUi events
* TextBox/PasswordBox
  * Enter text with keyboard
  * CTRL+V paste
  * CTRL+C copy (TextBox only)
  * CTRL+X cut (TextBox only)
  * CTRL+A select all
  * Shift+arrow to select
  * Cursor drag to select
  * Caret movement with arrows
  * Delete and backspace to remove letters
  * Double-click to select all

## IsFocused and Tabbing

Gum supports tabbing once a control has focus.&#x20;



continue here...
