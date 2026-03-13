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
// Initialize
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
// Initialize
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
// Initialize
TopButton.ControllerButtonPushed += (button) =>
    TextInstance.Text = $"Top button button pushed: {button} @ {DateTime.Now}";
```

<figure><img src="../../.gitbook/assets/09_10 01 14.gif" alt=""><figcaption><p>Buttons can respond to any gamepad button push</p></figcaption></figure>

The Click event may be raised with InputEventArgs containing the gamepad. Remember, clicks can happen a variety of ways including the mouse or even being directly invoked, so you need to check whether the second parameter is of type `InputEventArgs` and if the device is a GamePad.

```csharp
// Initialize
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
// Update
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

## ListBox Navigation

When a `ListBox` has focus, pressing up or down on the D-pad (or tilting the left stick up or down) moves the selection through the list. The `SelectionChanged` event fires each time the selected item changes.

The following example adds items to a `ListBox`, enables gamepad input, and reacts to selection changes:

```csharp
// Initialize
var listBox = new ListBox();
listBox.Width = 200;
listBox.Height = 150;
listBox.Items.Add("Option 1");
listBox.Items.Add("Option 2");
listBox.Items.Add("Option 3");
listBox.Items.Add("Option 4");
listBox.AddToRoot();

listBox.SelectionChanged += (_, args) =>
    TextInstance.Text = $"Selected: {listBox.SelectedObject}";

FrameworkElement.GamePadsForUiControl.Clear();
FrameworkElement.GamePadsForUiControl.AddRange(GumUI.Gamepads);
listBox.IsFocused = true;
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACqVSYUtCMRT9LvgfLqMPSvJSKwLFwDRNCBRTIhjEdBddPTfZ9tIS_3v3zZQ0yKB92e69595zdrZVNgPAOq6dzFgFvE2wEDJKK69ErD6Q0uxNWIiV8zdmCTXQuID7TZTLV7n-qkSPSvop1cvF4rfsHarJ1FO6dFk8BA-JxVGJ2KOm8GLwPkcXNdUMtVNGp-U0FdVHzsSJxx9j_zGg43HmorqUOc66c09oKHGW_x1RPoo4P4q42EdQbWD6xvjgJdfBazHCeOt0et74nJ6ip9TKq-IuHuAydZeznkXnIJmfSbPQQEQTMcO5kOANjKdCTxAcxjhORXC26z_k3-p62GIboVfCaQ1yzwUQduLyULvmesU10NqTccLZphFlBVb7s1B2Ry-0rwP7OrC1LIlcGPt6GyM9mo_aFPeEdC1jh6phtLcmjhoxChv0_Q1Pd-qnonP0L4adgCEj3N7DUMc4cXSvWvj2VZbNrLOZT_A5IeIQAwAA" target="_blank">Try on XnaFiddle.NET</a>

Pressing down selects the next item and pressing up selects the previous item. The selection does not wrap when it reaches the first or last item.

## Slider Adjustment

When a `Slider` has focus, pressing left or right on the D-pad (or tilting the left stick left or right) decreases or increases the slider's `Value` by `SmallChange`. By default, a `Slider` sets `IsUsingLeftAndRightGamepadDirectionsForNavigation` to `false` so that left and right inputs adjust the value rather than moving focus to another control.

The following example creates a `Slider` with a range of 0–100, enables gamepad input, and displays the current value:

```csharp
// Initialize
var slider = new Slider();
slider.Width = 200;
slider.Minimum = 0;
slider.Maximum = 100;
slider.Value = 50;
slider.SmallChange = 10;
slider.AddToRoot();

var label = new Label();
label.Y = 40;
label.Text = $"Value: {slider.Value}";
label.AddToRoot();

slider.ValueChanged += (_, _) =>
    label.Text = $"Value: {slider.Value}";

FrameworkElement.GamePadsForUiControl.Clear();
FrameworkElement.GamePadsForUiControl.AddRange(GumUI.Gamepads);
slider.IsFocused = true;
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACo2SUWvCMBDH3wW_w1H2oExCHduL0oHUKcKEoXVjUJC4HjMsTSRJp5v43XeN4oJP9qn3-9_d_67XfbMBEE3suCqjHjhTYccToYQTXIpfJBx9cwNWigINJKBwC3MftNr9XB05exOFW5N6F8cXcEGtLClkwYbc8exng5YNRYnKCq1quUZssLJaVg7_y6c0RVmVVBv0nPLdCXZDq1cuKyT4ELB5yaVM11x91ko3UAZFkemZ1s6vkKt6QclXKE_7PdfvXvOUvRO_j89hhjtH5CaPvG0P9uEUhzw6Z14ahXnHyQq4TaC17MCyDcljrva5AnquNzr4xiPDS9xq8_Ukkb6sY2OKX3hhR9osRKqVM1qyVCI_nu26fBp_Vg_ZouMtJj5nQznB3SdU8FFZWiPxv08_ajYOzcYfiDE4m1gCAAA" target="_blank">Try on XnaFiddle.NET</a>

Each left or right press changes `Value` by the amount set in `SmallChange`. You can also set `LargeChange` for larger increments when clicking the track area with a mouse or cursor.
