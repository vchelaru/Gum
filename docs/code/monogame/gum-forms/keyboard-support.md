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

To enable gamepad control, add the following code. This code only needs to run once, so add it to your game's Initialize or other code which runs at startup.

```csharp
FrameworkElement.KeyboardsForUiControl.Add(GumUI.Keyboard);
```

Keep in mind that a control must first be explicitly to receive keyboard input.&#x20;

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

