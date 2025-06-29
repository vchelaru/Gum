# Migrating to 2025 June 27

## Introduction

This page discusses breaking changes when migrating from `2025 May 28` to `2025 June 27`.

## Default TextBox and PasswordBox States

If using a code-only setup, the default TextBox and PasswordBox have changed default states from `"Selected"` to `"Focused"` . This change only affects code which attempts to access the Selected state. It does not affect code which creates a Selected state, nor does it affect projects using the Gum tool since loading from Gum tool creates a Selected state.

❌ The following code will now throw an exception:

```csharp
var textBox = new TextBox();
var category = textBox.Visual.Categories["TextBoxCategory"];
var state = category.States.First(item => item.Name == "Selected");
// make modifications to the Selected state
```

✅ The code should be replaced with the following block:

```csharp
var textBox = new TextBox();
var category = textBox.Visual.Categories["TextBoxCategory"];
var state = category.States.First(item => item.Name == "Focused");
// make modifications to the Focused state
```

The reason for this change is because all other Forms types have a `Focused` state which is used when the control's `IsFocused` property is set to true. `TextBox` used an incorrectly-named `Selected` state, which is used by controls which can display a selected visual state (such as `ListBoxItem` ) even when they do not have focus. This change makes `TextBox` consistent with other controls such as `Button` and `CheckBox`.
