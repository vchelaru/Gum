# Keyboard Support

## Introduction

Gum Forms can use the keyboard to interact with various controls. Controls receive keyboard input if they are focused and if keyboard actions have been enabled. Note that TextBoxes always receive input if they are focused even if keyboards have not been added to FrameworkElement.KeyboardsForUiControl.

For information on tabbing with the keyboard, see the [Tabbing](tabbing-moving-focus.md) page.

## TextBox Input

TextBoxes receive input from the keyboard if they are focused. A TextBox's focus can be set in code by setting IsFocused to true, or through the UI by clicking on the TextBox or by tabbing to the TextBox. For more information see the TextBox page.

## ListBox Navigation

Unlike a TextBox, a `ListBox` does not receive keyboard input simply by being focused. A keyboard must first be registered with `FrameworkElement.KeyboardsForUiControl`. Call `GumUI.UseKeyboardDefaults()` once at startup to do this. Without it, the arrow keys do nothing even when the `ListBox` is focused.

```csharp
// Initialize
GumUI.UseKeyboardDefaults();
```

A focused `ListBox` starts at the top level, where it participates in [tabbing](tabbing-moving-focus.md) between controls. Press Enter to move focus into the items; the up and down arrow keys then move the highlighted item, and Enter selects the highlighted item and returns focus to the top level.

To have a `ListBox` start with an item already focused, so the arrow keys work without first pressing Enter, see [Keyboard and Gamepad Navigation](../controls/listbox.md#keyboard-and-gamepad-navigation) on the ListBox page.

## Custom Keyboards

Forms reads keyboard input from the default keyboard that Gum creates at startup. You can replace it with your own implementation of `IInputReceiverKeyboard`, for example to replay recorded keystrokes, drive an on-screen keyboard, or feed input from accessibility tools.

```csharp
// Initialize
GumUI.UseKeyboardDefaults();

FormsUtilities.SetKeyboard(new ReplayKeyboard());
```

In this example `ReplayKeyboard` is your own class that implements `IInputReceiverKeyboard`. Focused TextBoxes receive the characters it reports, and ListBox navigation and [tabbing](tabbing-moving-focus.md) respond to the keys it reports.

`SetKeyboard` also replaces the old keyboard in `FrameworkElement.KeyboardsForUiControl` and updates `FrameworkElement.MainKeyboard`, so you do not need to change either one yourself. You can call it before or after `UseKeyboardDefaults`.

While a custom keyboard is installed, `GumUI.Keyboard` returns `null` because it only returns Gum's built-in keyboard. Use `FormsUtilities.Keyboard` to get whichever keyboard is active.

{% hint style="info" %}
Available in October 2026, or now if building Gum from source.
{% endhint %}
