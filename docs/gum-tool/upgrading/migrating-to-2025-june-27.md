# Migrating to 2025 June 27

## Introduction

This page discusses breaking changes when migrating from `2025 May 28` to `2025 June 27`.

## Upgrading Gum Tool

To upgrade the Gum tool:

1. Download Gum.zip from the release on Github: [https://github.com/vchelaru/Gum/releases/tag/Release\_June\_27\_2025](https://github.com/vchelaru/Gum/releases/tag/Release_June_27_2025)
2. Delete the old tool from your machine
3. Unzip the gum tool to the same location as to not break any file associations

## Upgrading Runtime

Upgrade your Gum NuGet packages to version 2025.6.26.1. For more information, see the NuGet packages for your particular platform:

* MonoGame - [https://www.nuget.org/packages/Gum.MonoGame/](https://www.nuget.org/packages/Gum.MonoGame/)
* KNI - [https://www.nuget.org/packages/Gum.KNI/](https://www.nuget.org/packages/Gum.KNI/)
* FNA - [https://www.nuget.org/packages/Gum.FNA/](https://www.nuget.org/packages/Gum.FNA/)
* SkiaSharp - [https://www.nuget.org/packages/Gum.SkiaSharp/](https://www.nuget.org/packages/Gum.SkiaSharp/)

For other platforms you need to build Gum from source

See below for breaking changes and updates.

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
