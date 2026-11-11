# TabControl

## Introduction

TabControl shows a strip of tab headers and the page of the selected tab. It inherits from [ItemsControl](itemscontrol.md). Each entry in `Items` creates one `TabItem` header in the strip and one `TabPage` in the content area, paired by position.

{% hint style="info" %}
Available in December 2026, or now if building Gum from source.
{% endhint %}

## Code Example: Adding Tabs

`AddTab` adds a tab with a header and the content shown when the tab is selected. Adding tabs does not select one, so the example sets `SelectedIndex`.

```csharp
// Initialize
var tabControl = new TabControl();
tabControl.AddToRoot();
tabControl.X = 50;
tabControl.Y = 50;
tabControl.Width = 400;
tabControl.Height = 200;

var playButton = new Button();
playButton.Text = "Play";
tabControl.AddTab("Game", playButton);

var volumeLabel = new Label();
volumeLabel.Text = "Volume";
tabControl.AddTab("Audio", volumeLabel);

tabControl.SelectedIndex = 0;
```

`AddTab` returns the new `TabItem`. Content can be a Forms control, a `GraphicalUiElement`, or a `TabPage`.

## Items

`Items` is the only collection to edit. `TabItems` and `Pages` are read-only views of the tabs and pages that `Items` created.

An object added to `Items` makes a tab whose header shows the object's `ToString()`. `VisualTemplate` changes the header and `ContentTemplate` creates the page content. Both receive the item, which also becomes the `BindingContext` of the page.

```csharp
// Initialize
var tabControl = new TabControl();
tabControl.AddToRoot();
tabControl.ContentTemplate = new VisualTemplate(item =>
{
    var label = new Label();
    label.Text = "Page for " + item;
    return label.Visual;
});

tabControl.Items.Add("Inventory");
tabControl.Items.Add("Map");
tabControl.SelectedIndex = 0;
```

As with [ListBox](listbox.md), assigning a bound collection to `Items` replaces tabs that were authored in the Gum tool.

## Selection

`SelectedIndex` and `SelectedObject` read and set the selected tab. `SelectedIndex` is `-1` when no tab is selected, and an out-of-range value is ignored. Removing the selected tab leaves nothing selected. `SelectionChanged` is raised whenever the selection changes, including when the user clicks a tab.

```csharp
// Initialize
var statusLabel = new Label();
statusLabel.AddToRoot();
statusLabel.Y = 300;

tabControl.SelectionChanged += (_, args) =>
{
    statusLabel.Text = "Selected tab " + tabControl.SelectedIndex;
};
```

## Keyboard and Gamepad

A focused TabControl selects the previous or next enabled tab with the left and right arrow keys, the gamepad DPad, or the left stick. Selection stops at the first and last tab. Call `GumUI.UseKeyboardDefaults()` or `GumUI.UseGamepadDefaults()` first to enable the input device.

## Styling

TabControl, TabItem, and TabPage are styled by your theme. The Standard and Bubblegum themes include components for all three, so tabs and pages can be authored in the Gum tool and parented to the `InnerPanelInstance` and `ContentPanelInstance` of the TabControl.
