# Migrating to 2025 June Preview

## Introduction

This page discusses breaking changes when migrating from `2025 May 28` to `2025 June Preview` .

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
