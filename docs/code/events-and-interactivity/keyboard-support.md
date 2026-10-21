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

## Key Repeat

When a key is held, `KeyTyped` is true when the key is first pressed, again after `RepeatDelay`, then every `RepeatRate`. Arrow keys in a `ListBox` and Backspace in a `TextBox` repeat this way. Both are `TimeSpan` properties on `IInputReceiverKeyboard`, so they are available on any host. They default to 500 ms and 70 ms. On Windows and macOS, `Initialize` reads the repeat delay and repeat speed from the system keyboard settings once and uses those instead. On macOS the settings are only present after the user has changed them in System Settings; otherwise the defaults apply. On Linux and other platforms the defaults apply. Changes to the system setting while the game runs are not picked up. Reading the settings only at startup is intentional, and a future version may follow changes made while the game runs. A value you set after `Initialize` always wins.

```csharp
using System;

// Initialize
GumUI.UseKeyboardDefaults();
GumUI.Keyboard.RepeatDelay = TimeSpan.FromMilliseconds(300);
GumUI.Keyboard.RepeatRate = TimeSpan.FromMilliseconds(40);
```

Typed characters repeat as the operating system repeats them and are not affected. On raylib this includes the newline that Enter inserts into a multi-line `TextBox`. A custom keyboard that does not override the properties ignores what you set.

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

Code written against `IGumService` can read the active keyboard with `IGumService.Default.Keyboard`.

{% hint style="info" %}
`IGumService.Default.Keyboard` is available in November 2026, or now if building Gum from source.
{% endhint %}
