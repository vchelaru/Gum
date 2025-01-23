# Gamepad Support

## Introduction

Gum Forms supports using a gamepad to control the UI. In general, when using a gamepad a single UI control has focus. Pressing up or down on the gamepad moves focus to the next item. Each forms control can support different interaction. For example, a Button can be clicked, but a ListBox supports selecting items within the ListBox.

## Enabling Gamepad Controls

To enable gamepad support in your screen:

1. Be sure to have a gamepad plugged in. Any gamepad that is usable in MonoGame will also work as a gamepad in Gum Forms
2. Add the gamepad index to the FrameworkElement.GamepadsForUiControl
3. Set the initial control to have focus by setting its `IsFocused = true`

For example, the following code enables gamepad control for a game assuming MyButton is a valid button:

```csharp
// The first gamepad:
var gamepad = FormsUtilities.Gamepads[0];
FrameworkElement.GamePadsForUiControl.Add(gamepad);
MyButton.IsFocused = true;
```
