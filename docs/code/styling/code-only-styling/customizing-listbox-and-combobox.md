# Customizing ListBox and ComboBox

## Introduction

The ListBoxItems control type is created automatically by `ListBox` and `ComboBox` instances whenever the Items property changes. Since `ListBoxItems` are not created manually, they must be customized through template objects.

This document shows how to customize `ListBoxItem` instances using template classes.

## Customizing Using VisualTemplate

`ListBox` and `ComboBox` include a `VisualTemplate` property which can be used to customize `ListBoxItem` instances. The following code modifies the color of each `ListBoxItem`.

```csharp
var listBox = new ListBox();
listBox.AddToRoot();
listBox.Anchor(Anchor.Center);
listBox.VisualTemplate = new VisualTemplate(() =>
{
    var visual = new ListBoxItemVisual();

    visual.ForegroundColor = Color.Yellow;
    visual.HighlightedBackgroundColor = Color.DarkGreen;
    visual.SelectedBackgroundColor = Color.Green;

    return visual;
});

for(int i = 0; i < 20; i++)
{
    listBox.Items.Add($"Item {i}");
}
```

<figure><img src="../../../.gitbook/assets/27_04 50 44.gif" alt=""><figcaption><p>Customized ListBoxItems in a ListBox</p></figcaption></figure>

`ComboBox` uses the same syntax as `ListBox` for its `VisualTemplate` since internally `ComboBoxes` create `ListBoxes` as their dropdown. The following code shows how to customize a `ComboBox's` dropdown items:

```csharp
var comboBox = new ComboBox();
comboBox.AddToRoot();
comboBox.Anchor(Anchor.Center);
comboBox.VisualTemplate = new VisualTemplate(() =>
{
    var visual = new ListBoxItemVisual();

    visual.ForegroundColor = Color.Yellow;
    visual.HighlightedBackgroundColor = Color.DarkGreen;
    visual.SelectedBackgroundColor = Color.Green;

    return visual;
});

for(int i = 0; i < 20; i++)
{
    comboBox.Items.Add($"Item {i}");
}
```

<figure><img src="../../../.gitbook/assets/27_06 14 31.gif" alt=""><figcaption><p>Customized ListBoxItems in a ComboBox dropdown</p></figcaption></figure>

