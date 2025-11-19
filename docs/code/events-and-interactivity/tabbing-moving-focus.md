# Tabbing (Moving Focus)

## Introduction

Gum supports tabbing focus between controls. Tabbing can be performed with the keyboard or gamepad.

## Keyboard Tabbing

The keyboard can be used to interact with controls. Keyboards can be used to:

* Tab forward and back to pass focus to new controls
* To click controls by pressing enter
* To perform control-specific actions such as changing the value of a slider

To enable gamepad control, add the following code. This code only needs to run once, so add it to your game's Initialize or other code which runs at startup.

```csharp
GumUI.UseKeyboardDefaults();
```

Keep in mind that a control must first be explicitly set to receive keyboard input.

For example, the following code gives a Button focus assuming MyButton is a valid button:

```csharp
MyButton.IsFocused = true;
```

Note that TextBox and PasswordBox automatically have IsFocused set to true when clicked on.

Controls can be skipped when tabbed by setting `GamepadTabbingFocusBehavior`. For example the following code results in a button being skipped if it receives focus from tabbing:

```csharp
MyButton.GamepadTabbingFocusBehavior = TabbingFocusBehavior.SkipOnTab;
```

{% hint style="info" %}
Despite its name, the GamepadTabbingFocusBehavior property controls tabbing for both gamepad and keyboard tabbing. Future versions of Gum may change this property to more clearly indicate its purpose.
{% endhint %}

## Gamepad Tabbing

To enable gamepad support in your game:

1. Be sure to have a gamepad plugged in. Any gamepad that is usable in MonoGame will also work as a gamepad in Gum Forms
2. Add the gamepad to the FrameworkElement.GamepadsForUiControl. You can add multiple gamepads for multiplayer games.
3. Set the initial control to have focus by setting its `IsFocused = true`

For example, the following code enables gamepad control for a game assuming MyButton is a valid button:

```csharp
// The first gamepad:
var gamepad = GumUI.Gamepads[0];
// If this code is run multiple times then the gamepad
// may get added multiple times as well. To be safe, clear
// the list:
FrameworkElement.GamePadsForUiControl.Clear();
FrameworkElement.GamePadsForUiControl.Add(gamepad);
MyButton.IsFocused = true;
```
