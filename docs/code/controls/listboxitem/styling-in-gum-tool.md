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

