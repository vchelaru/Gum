# IsFocused

### Introduction

The IsFocused property gets or sets whether a component has focus. For example a TextBox will have its IsFocused set to true if the caret is visible and if it is taking input from the keyboard.

### Code Example - Setting IsFocused

IsFocused can be directly assigned to give focus to an object. For example the following code gives a TextBoxInstance focus:

```csharp
// Initialize
TextBoxInstance.IsFocused = true;
```

If you are setting focus on a TextBox in response to a click event, you may need to also clear the Cursor's input. For example, the following would set focus on TextBoxInstance when ButtonInstance is clicked:

```csharp
// Initialize
ButtonInstance.Click += (_,_) =>
{
    TextBoxInstance.IsFocused = true;
    // Remove the clear so that the TextBoxInstance doesn't immediately lose focus
    // on the same frame it got focus due to the cursor having been clicked
    GumService.Default.Cursor.ClearInputValues();
};
```

### Focusing Disabled Controls

A disabled control (`IsEnabled` set to `false`) cannot take focus by default. Tab order and gamepad or keyboard navigation skip it, and setting `IsFocused` to `true` does nothing. Set `AllowFocusWhenDisabled` to `true` on a control to change this. For example, a shop menu can let a gamepad player reach an upgrade they cannot afford yet to read its description.

```csharp
// Initialize
UpgradeButton.IsEnabled = false;
UpgradeButton.AllowFocusWhenDisabled = true;
```

A focused disabled control shows its **DisabledFocused** state and ignores confirm, click, typing, and value changes just as it does when unfocused. Focus can still move away from it with the d-pad, arrow keys, or tab. The property applies to each control individually and is not inherited, and a control inside a disabled parent stays out of navigation. See [Moving Focus](../../events-and-interactivity/tabbing-moving-focus.md) for how navigation picks the next control.

{% hint style="info" %}
Available in November 2026, or now if building Gum from source.
{% endhint %}
