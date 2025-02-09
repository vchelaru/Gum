# Gamepad Support

## Introduction

Gum Forms supports using a gamepad to control the UI. In general, when using a gamepad a single UI control has focus. Pressing up or down on the gamepad moves focus to the next item. Each forms control can support different interaction. For example, Buttons can be clicked, but a ListBox supports selecting items within the ListBox.

## Enabling Gamepad Controls

To enable gamepad support in your screen:

1. Be sure to have a gamepad plugged in. Any gamepad that is usable in MonoGame will also work as a gamepad in Gum Forms
2. Add the gamepad to the FrameworkElement.GamepadsForUiControl. You can add multiple gamepads for multiplayer games.
3. Set the initial control to have focus by setting its `IsFocused = true`

For example, the following code enables gamepad control for a game assuming MyButton is a valid button:

```csharp
// The first gamepad:
var gamepad = FormsUtilities.Gamepads[0];
FrameworkElement.GamePadsForUiControl.Add(gamepad);
MyButton.IsFocused = true;
```

## Button Control Click

By default a gamepad's A button can be used to select the focused control. If the focused control is a `Button` then pressing a gamepad's A button. The following example code shows how to detect clicks on a button which happen with the gamepad:

```csharp
FrameworkElement.GamePadsForUiControl.AddRange(
    FormsUtilities.Gamepads);

TopButton.FormsControl.IsFocused = true;
TopButton.FormsControl.Click += (_, _) =>
    TextInstance.Text = $"Top button clicked at {DateTime.Now}";

BottomButton.FormsControl.Click += (_, _) =>
    TextInstance.Text = $"Bottom button clicked at {DateTime.Now}";
```

Pressing the A button raises the focused button's Click event.

<figure><img src="../../../.gitbook/assets/09_09 57 16.gif" alt=""><figcaption><p>Pressing the A button clicks the highlighted Button</p></figcaption></figure>

Handling buttons specifically can be handled by subscribing to ControllerButtonPushed.

```csharp
TopButton.FormsControl.ControllerButtonPushed += (button) =>
    TextInstance.Text = $"Top button button pushed: {button} @ {DateTime.Now}";
```

<figure><img src="../../../.gitbook/assets/09_10 01 14.gif" alt=""><figcaption><p>Buttons can respond to any gamepad button push</p></figcaption></figure>

If additional flexibility is needed, gamepad events can be polled in an Update method.

```csharp
var gamepads = FrameworkElement.GamePadsForUiControl;
for (int i = 0; i < gamepads.Count; i++)
{
    var gamepad = gamepads[i];

    if(gamepad.ButtonPushed(Microsoft.Xna.Framework.Input.Buttons.A))
    {
        var focusedElement = InteractiveGue.CurrentInputReceiver;

        if(focusedElement != null)
        {
            TextInstance.Text =
                $"Gamepad {i} pressed A on {focusedElement} of type " +
                $"{focusedElement?.GetType()}";
        }
    }
}
```

<figure><img src="../../../.gitbook/assets/09_10 56 13.png" alt=""><figcaption></figcaption></figure>
