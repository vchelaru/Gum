# Runtime Variable References

## Introduction

Variable references defined in the Gum tool can be re-evaluated at runtime, allowing you to change style values in code and propagate them across your entire project before creating UI. This is useful for theming, accessibility settings, or any scenario where centralized style values need to change at startup or during gameplay.

{% hint style="info" %}
Runtime variable references require Gum NuGet packages version 2026.4 or newer. The optional `Gum.Expressions` NuGet is also available starting from this version.
{% endhint %}

For information on setting up variable references in the Gum tool, see the [Variable References](../../gum-tool/gum-elements/general-properties/variable-references.md) page.

## Propagating Variable References

Variable references operate at the project level, modifying values on `ElementSave` and `StateSave` objects rather than on live `GraphicalUiElement` visuals. This means you should apply variable references before creating your UI. Any controls created after applying references will use the updated values.

{% hint style="info" %}
If you need to change style values after UI has already been created, you must apply the variable references and then re-create the affected controls for the changes to take effect.
{% endhint %}

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
