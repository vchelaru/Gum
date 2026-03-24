# Controls

## Introduction

This section covers the Gum Forms controls — a set of interactive UI controls built on top of the Gum layout system. Forms controls combine visual appearance with built-in interaction logic, making it straightforward to build game UI without writing low-level input handling code.

## How Forms Controls Differ from Raw GraphicalUiElement Usage

When you use a raw `GraphicalUiElement` (or one of the standard visuals like `SpriteRuntime` or `ColoredRectangleRuntime`), you get full layout control but no interaction logic. Forms controls add:

* **Built-in interaction logic** — buttons respond to clicks, text boxes accept keyboard input, list boxes handle selection, and so on.
* **Default visuals** — each control ships with a default appearance so it works out of the box without a Gum project file.
* **State machines** — controls automatically transition between visual states (e.g., `Enabled`, `Disabled`, `Highlighted`, `Pushed`) in response to user input.

## FrameworkElement — The Base Class

Every Forms control inherits from `FrameworkElement`. It provides the common foundation shared by all controls, including:

* `IsEnabled` — disables a control so it stops receiving input.
* `IsFocused` — tracks whether the control currently has keyboard focus.
* Layout shortcuts (`X`, `Y`, `Width`, `Height`, `Anchor`, `Dock`) that forward to the underlying `Visual` property.
* Data binding support via `SetBinding` and `BindingContext`.

For details on these shared members, see the [FrameworkElement](frameworkelement/) section.

## Available Controls

* [Button](button.md)
* [CheckBox](checkbox.md)
* [ComboBox](combobox.md)
* [FrameworkElement](frameworkelement/)
* [Grid](grid.md)
* [ItemsControl](itemscontrol.md)
* [Label](label.md)
* [ListBox](listbox.md)
* [ListBoxItem](listboxitem/)
* [Menu](menu.md)
* [MenuItem](menuitem.md)
* [PasswordBox](passwordbox.md)
* [RadioButton](radiobutton.md)
* [ScrollBar](scrollbar.md)
* [ScrollViewer](scrollviewer/)
* [StackPanel](stackpanel.md)
* [Slider](slider.md)
* [Splitter](splitter.md)
* [TextBox](textbox.md)
* [Window](window.md)
