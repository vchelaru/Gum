# ListBoxItem (Customizing ListBox and ComboBox)

## Introduction

ListBoxItems are a control type which are typically created automatically by ListBox and ComboBox instances whenever the property changes. Since the ListBoxItems are not created manually, they must be customized through template objects.

This document shows how to customize ListBoxItem instances using template classes.

## Customizing Using VisualTemplate

ListBox and ComboBox include a VisualTemplate property which can be used to customize ListBoxItem instances. The following code modifies the color of each ListBoxItem.

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
