# Runtime Variable References

## Introduction

Variable references defined in the Gum tool can be re-evaluated at runtime, allowing you to change style values in code and propagate them across your entire project before creating UI. This is useful for theming, accessibility settings, or any scenario where centralized style values need to change at startup or during gameplay.

{% hint style="info" %}
Runtime variable references require Gum NuGet packages version 2026.4 or newer. The optional `Gum.Expressions` NuGet is also available starting from this version.
{% endhint %}

For information on setting up variable references in the Gum tool, see the [Variable References](../../gum-tool/gum-elements/general-properties/variable-references.md) page.

## Propagating Variable References

Variable references operate at the project level, modifying values on `ElementSave` and `StateSave` objects rather than on live `GraphicalUiElement` visuals. This means you should apply variable references before creating your UI. Any controls created after applying references will use the updated values.

After loading a Gum project, you can modify style variables and call `ApplyAllVariableReferences` to propagate changes across all elements in dependency order. Elements that are referenced by others are applied first, so downstream references always pick up the latest values.

```csharp
// Initialize
var project = GumUI.Initialize(this, "GumProject/GumProject.gumx");

// Find your style component and change values
var styles = project.Components.First(item => item.Name == "Styles");

styles.DefaultState.SetValue("Primary.Red", 255);
styles.DefaultState.SetValue("Primary.Green", 0);
styles.DefaultState.SetValue("Primary.Blue", 0);

// Propagate all variable references across the project
ObjectFinder.Self.GumProjectSave.ApplyAllVariableReferences();

// Now create UI - it will use the updated values
project.Screens.First().ToGraphicalUiElement().AddToRoot();
```

The `ApplyAllVariableReferences` method iterates all elements (standards, components, and screens) and applies variable references on every state, including category states. It automatically handles dependency ordering so that if element B references element A, element A's references are applied first.

## Refreshing Live Visuals After Style Changes

If your UI is already on screen and you change style values, you can push those changes to live visuals without re-creating controls. After calling `ApplyAllVariableReferences`, call `RefreshStyles` to re-apply all default state values and Forms visual states recursively.

The simplest approach refreshes all three root containers (Root, PopupRoot, ModalRoot):

```csharp
// Update
// Change style values
var styles = ObjectFinder.Self.GumProjectSave.Components
    .First(item => item.Name == "Styles");
styles.DefaultState.SetValue("Primary.Red", 0);
styles.DefaultState.SetValue("Primary.Green", 128);
styles.DefaultState.SetValue("Primary.Blue", 255);

// Propagate variable references to all ElementSave states
ObjectFinder.Self.GumProjectSave.ApplyAllVariableReferences();

// Push updated values to all live visuals
GumService.Default.RefreshStyles();
```

You can also target a specific subtree if you know which part of the UI changed:

```csharp
// Update
ObjectFinder.Self.GumProjectSave.ApplyAllVariableReferences();

// Only refresh visuals under a specific element
GumService.Default.RefreshStyles(myScreenGue);
```

`RefreshStyles` re-applies default state values on every element in the tree and then re-applies the current Forms categorical state (such as Highlighted or Disabled) on each Forms control. This means controls retain their current interaction and runtime state while picking up the new style values.

### Automatic State Preservation

All Forms controls automatically re-apply their current categorical state after `RefreshStyles`. This means any state driven by a Forms property is preserved without any extra work. For example:

* CheckBox `IsChecked` state (checked/unchecked visual)
* RadioButton `IsChecked` and group selection
* ToggleButton `IsChecked` state
* Expander `IsExpanded` state (expanded/collapsed visual)
* ListBox and ListBoxItem selection (`SelectedIndex`, `IsSelected`)
* ComboBox selection
* MenuItem `IsSelected` and `IsHighlighted`
* Button, Label, and all controls' `IsEnabled` / `IsFocused` interaction states

These properties are Forms-level backing fields that are not overwritten by Gum state re-application, so they survive `RefreshStyles` naturally.

### Explicit Value Preservation

Some controls hold runtime values that *are* stored on the visual layer and would be overwritten by default state re-application. These are explicitly saved before the refresh and restored afterward:

* Button and Label `Text`
* TextBox and PasswordBox text content, caret position, and selection
* ComboBox displayed text
* Slider and ScrollBar `Value` (thumb position)
* ScrollViewer scroll offset (both vertical and horizontal)

## Preserving Custom Runtime Properties

If you set properties at runtime that are not automatically preserved by built-in controls (for example, the size of a resizable panel), you can register them for automatic preservation. Registered properties are captured before `RefreshStyles` re-applies default state values and restored afterward.

The simplest form takes a target object and a property name:

```csharp
// In CustomInitialize
LeftPanel.Width = 300;
this.RegisterRuntimeProperty(LeftPanel, nameof(LeftPanel.Width));
```

You can also register a property on the current control using the shorthand:

```csharp
// In CustomInitialize
this.RegisterRuntimeProperty(nameof(IsEnabled));
```

For computed or transformed values, use the lambda overload:

```csharp
// In CustomInitialize
this.RegisterRuntimeProperty(
    () => ContentPanel.GetAbsoluteWidth(),
    v => ContentPanel.Width = v);
```

Registrations persist for the lifetime of the control. Call `RegisterRuntimeProperty` once during initialization — it does not need to be called again after each refresh.

## Recomputing Values After a Style Refresh

If you need to recompute a value rather than restore it (for example, re-applying localized text after a locale change), subscribe to the `AfterRefreshStyles` event:

```csharp
// In CustomInitialize
void ApplyLocalizedText()
{
    Title.Text = MainMenuTexts.ExperimentalBuild;
    Body.Text  = MainMenuTexts.ThisIsAnExperimentalBuild;
}

ApplyLocalizedText();
this.AfterRefreshStyles += (_, _) => ApplyLocalizedText();
```

A corresponding `BeforeRefreshStyles` event fires before default state values are re-applied, allowing you to capture state if needed.

{% hint style="info" %}
Built-in controls like Label and Button already preserve their `Text` across `RefreshStyles` automatically. You only need `AfterRefreshStyles` when the value should be *recomputed* rather than restored — for example, when the locale may have changed between refreshes.
{% endhint %}

## Expression Support (Optional)

By default, variable references support simple dot-path lookups like `Width = OtherInstance.Width`. If your project uses arithmetic expressions in variable references (such as `Width = OtherInstance.Width + 20`), you need to add the `Gum.Expressions` NuGet package and initialize the expression evaluator.

Add the NuGet package:

```
dotnet add package Gum.Expressions
```

Then call `GumExpressionService.Initialize()` at startup:

```csharp
// Initialize
var project = GumUI.Initialize(this, "GumProject/GumProject.gumx");
GumExpressionService.Initialize();
```

{% hint style="info" %}
The `Gum.Expressions` package uses Microsoft Roslyn for expression parsing, which adds approximately 10 MB to your build output. If your variable references only use simple assignments (no arithmetic), you do not need this package.
{% endhint %}

If you are linking to Gum source instead of NuGet, see the setup page for your platform for instructions on adding GumExpressions as a project reference:

* [MonoGame/KNI/FNA Setup](../getting-started/setup/adding-initializing-gum/monogame-kni-fna/README.md)
* [Raylib Setup](../getting-started/setup/adding-initializing-gum/raylib-raylib-cs.md)
