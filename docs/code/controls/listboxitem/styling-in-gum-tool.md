# Styling in Gum Tool

## Introduction

This page discusses how to style a ListBoxItem in the Gum tool. The recommended approach is to create a copy of the existing ListBoxItem, or to modify the ListBoxItem in place.

## Styling Requirements

The following are required:

* Behavior named `ListBoxItemBehavior`&#x20;

The following are optional:

* Text instance named `TextInstance` . This is used by the `ListBoxItem`'s UpdateToObject method.

## States

Adding the `ListBoxItemBehavior` behavior creates a category named `ListBoxItemCategory` on your component, along with the states the behavior requires:

* `Enabled`
* `Highlighted`
* `Selected`
* `Focused`

Set variables such as colors and visibility in each of these states so the item gives the player visual feedback as they move over it and select it.

Two more states are recognized at runtime, but the behavior does not create them:

* `Disabled`, applied when the item or one of its parents is not enabled. The ListBoxItem added by **Add Forms Components** already includes this state, so you only need to add it if you built your component from scratch.
* `SelectedHighlighted`, applied when the cursor is over an item that is already selected. No ListBoxItem includes this state by default. If it is missing, a selected item stays in its `Selected` state while the cursor is over it.

To add either one, select `ListBoxItemCategory` in the **States** tab and add a state with the matching name. For more information see the [Categories](../../../gum-tool/gum-elements/states/categories.md) page.


## Using Your Component in One ListBox

To use your component for one ListBox only, set that ListBox's `FrameworkElementTemplate` in code. This example uses a component named `ShopItem`, which code generation turns into a `ShopItem` class that derives from `ListBoxItem`:

```csharp
// In CustomInitialize
ShopListBox.FrameworkElementTemplate = new Gum.Forms.FrameworkElementTemplate(() => new ShopItem());
```

This applies to the **Gum Forms (recommended)** Output Library, which new projects use. A deprecated Output Library such as **MonoGame (deprecated)** generates a `ShopItemRuntime` visual class instead, which goes in the ListBox's `VisualTemplate`:

```csharp
// In CustomInitialize
ShopListBox.VisualTemplate = new Gum.Forms.VisualTemplate(() => new ShopItemRuntime(tryCreateFormsObject: false));
```

For more information see the [Customizing with VisualTemplate](../listbox.md#customizing-with-visualtemplate) section. Making your component the `ListBoxItemBehavior`'s [Default Implementation](../../../gum-tool/gum-elements/behaviors/default-implementation.md) instead changes the items of every ListBox and ComboBox.
