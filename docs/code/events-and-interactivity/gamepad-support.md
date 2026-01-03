# Gamepad Support

## Introduction

Gum Forms supports using a gamepad to control the UI. In general, when using a gamepad a single UI control has focus. Pressing up or down on the gamepad moves focus to the next item. Each forms control can support different interaction. For example, Buttons can be clicked, but a ListBox supports selecting items within the ListBox.

For information on using gamepads for tabbing, see the [Tabbing](tabbing-moving-focus.md) page.

## Enabling Gamepad Controls

To enable gamepad support in your screen:

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

## Button Control Click

By default a gamepad's A button can be used to select the focused control. If the focused control is a `Button` then pressing a gamepad's A button. The following example code shows how to detect clicks on a button which happen with the gamepad:

```csharp
FrameworkElement.GamePadsForUiControl.AddRange(
    GumUI.Gamepads);

TopButton.IsFocused = true;
TopButton.Click += (_, _) =>
    TextInstance.Text = $"Top button clicked at {DateTime.Now}";

BottomButton.Click += (_, _) =>
    TextInstance.Text = $"Bottom button clicked at {DateTime.Now}";
```

Pressing the A button raises the focused button's Click event.

<figure><img src="../../.gitbook/assets/09_09 57 16.gif" alt=""><figcaption><p>Pressing the A button clicks the highlighted Button</p></figcaption></figure>

Handling buttons specifically can be handled by subscribing to `ControllerButtonPushed`.

```csharp
TopButton.ControllerButtonPushed += (button) =>
    TextInstance.Text = $"Top button button pushed: {button} @ {DateTime.Now}";
```

<figure><img src="../../.gitbook/assets/09_10 01 14.gif" alt=""><figcaption><p>Buttons can respond to any gamepad button push</p></figcaption></figure>

The Click event may be raised with InputEventArgs containing the gamepad. Remember, clicks can happen a variety of ways including the mouse or even being directly invoked, so you need to check whether the second parameter is of type `InputEventArgs` and if the device is a GamePad.

```csharp
TopButton.Click += HandleClick;

// later, define the Click event:
private void HandleClick(object sender, EventArgs args)
{
    // Click events can come from the cursor or even manually invoked with
    // no backing input device, so checks are needed
    if(args is InputEventArgs inputEventArgs &&
        inputEventArgs.InputDevice is MonoGameGum.Input.GamePad gamePad)
    {
        var index = Array.IndexOf(FormsUtilities.Gamepads, gamePad);
        TextInstance.Text = $"Clicked with gamepad index {index} at {DateTime.Now}";
    }
}
```

<figure><img src="../../.gitbook/assets/10_05 38 36.png" alt=""><figcaption><p>Output from clicking on a button with a gamepad</p></figcaption></figure>

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

<figure><img src="../../.gitbook/assets/09_10 56 13.png" alt=""><figcaption></figcaption></figure>
