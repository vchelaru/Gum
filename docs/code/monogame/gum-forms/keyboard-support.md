# Keyboard Support

## Introduction

Gum Forms can use the keyboard to interact with various controls. Controls receive keyboard input if they are focused and if keyboard actions have been enabled. Note that TextBoxes always receive input if they are focused even if keyboards have not been added to FrameworkElement.KeyboardsForUiControl.

## TextBox Input

TextBoxes receive input from the keyboard if they are focused. A TextBox's focus can be set in code by setting IsFocused to true, or through the UI by clicking on the TextBox or by tabbing to the TextBox. For more information see the TextBox page.

## Tabbing and UI Interaction

The keyboard can be used to interact with controls. Keyboards can be used to:

* Tab forward and back to pass focus to new controls
* To click controls by pressing enter
* To perform control-specific actions such as changing the value of a slider

To enable gamepad control, add the following code. This code only needs to run once, so add it to your game's Initialize orother code which runs at startup.

```csharp
FrameworkElement.KeyboardsForUiControl.Add(Gum.Keyboard);
```

Keep in mind that a control must first be explicitly to receive keyboard input.&#x20;

For example, the following code enables gamepad control for a game assuming MyButton is a valid button:

```csharp
MyButton.IsFocused = true;
```
