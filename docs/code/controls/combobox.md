# ComboBox

## Introduction

The ComboBox control provides a compact way for users to select from a list of options.

## Code Example: Adding a ComboBox

```csharp
// Initialize
var comboBox = new ComboBox();
comboBox.AddToRoot();
for(int i = 0; i < 10; i++)
{
    comboBox.Items.Add($"Item {i}");
}
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAACqvm5VJQUPIsdi_NVbJSKCkqTdUBi2TmZZZkJuZkVqUChZXKEosUkvNzk_Kd8isUbBXyUssVnKFcDU3rmDyYnJ5jSkpIflB-fglYOC2_SCMzr0QhE6jHwBpI2SgYgmhtbc2YvOqYPAUggGv1LEnNLQYZoKESowTiKFRn1sYogcypVeLlquXlAgAcjglhrAAAAA)

<figure><img src="../../.gitbook/assets/27_07 29 52.gif" alt=""><figcaption><p>ComboBox displaying a list of items</p></figcaption></figure>

{% hint style="info" %}
ComboBox's internal ListBox is hidden until the dropdown opens, so Gum automatically skips layout when adding items. This means adding hundreds of items to a ComboBox is fast without any extra code. If you are adding many items to a visible ListBox or other ItemsControl, see [Reducing Layout Calls](../performance-and-optimization/measuring-layout-calls.md#reducing-layout-calls) for how to suspend layout during bulk adds.
{% endhint %}

## Adjusting the Drop-Down ListBox

`ComboBox` provides a `ListBox` property which can be used to customize the ListBox instance.

This can be modified, even if it hasn't yet been shown. The following code shows how to modify the default size of the ListBox:

```csharp
// Initialize
var comboBox = new ComboBox();
comboBox.AddToRoot();
comboBox.Anchor(Gum.Wireframe.Anchor.Top);
comboBox.ListBox.Height = 400;

for(int i = 0; i < 40; i++)
{
    comboBox.Items.Add($"Item {i+1}");
}
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAAA12OPQ-CMBCG_0rTOEBiSEUniIMwKImTIXHpglDkEtuaUtRI-O9eBU30hvt43vvqadZuO0kjazoxp6DAQnGBp6ARvRWGlFqedKIfZE2UuJN0Kj0_5uqjBZuqyvVBa_uHVdlo4-H64AhG1KaQYoJBrq8_vXtorYs7AefG4rUVY6jXOA_KEkDCYgy8Y2yZooqFy8Nk9D5XPVcE7bsys0K27jdv9u4JHSD9NLYYRui-GOjwAqrqntIIAQAA)

<figure><img src="../../.gitbook/assets/25_07 59 59.png" alt=""><figcaption><p>ComboBox ListBox with custom Height</p></figcaption></figure>

## Performance with Many Items

If all items in the ComboBox dropdown are the same height (no text wrapping or variable-sized icons), you can improve dropdown open performance by setting `UseFixedStackChildrenSize` on the ListBox's inner panel. This tells the layout engine to measure only the first child and assume all others are the same size, skipping per-child measurement.

```csharp
// Initialize
var comboBox = new ComboBox();
comboBox.AddToRoot();
comboBox.ListBox.InnerPanel.UseFixedStackChildrenSize = true;

for(int i = 0; i < 800; i++)
{
    comboBox.Items.Add($"Item {i}");
}
```

{% hint style="warning" %}
Only use `UseFixedStackChildrenSize` when all items have the same height. If items have different heights (for example, from text wrapping or varying content), this setting produces incorrect layout.
{% endhint %}

## SelectedObject and SelectedIndex

Use `SelectedIndex` to select an item by its zero-based position in `Items`, or read it back to find which item is currently selected. Use `SelectedObject` to get or set the selected item directly as an object.

```csharp
// Initialize
var comboBox = new ComboBox();
comboBox.AddToRoot();
for(int i = 0; i < 5; i++)
{
    comboBox.Items.Add($"Item {i}");
}
comboBox.SelectedIndex = 2;
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAACqvm5VJQUPIsdi_NVbJSKCkqTdUBi2TmZZZkJuZkVqUChZXKEosUkvNzk_Kd8isUbBXyUssVnKFcDU3rmDyYnJ5jSkpIflB-fglYOC2_SCMzr0QhE6jHwBpI2SiYAiltbc2YvOqYPAUggOv0LEnNLQbp11CJUQJxFKoza2OUQMbUIlkQnJqTmlySmuKZl5IKcoqRtRIvVy0An04fSsYAAAA)

## SelectionChanged

The `SelectionChanged` event fires whenever the user picks a different item. The handler receives a `SelectionChangedEventArgs` with information about the previously and newly selected items. You can also read `SelectedObject` or `SelectedIndex` directly inside the handler.

```csharp
// Initialize
var label = new Label();
label.AddToRoot();

var comboBox = new ComboBox();
comboBox.AddToRoot();
comboBox.Y = 24;
for(int i = 0; i < 5; i++)
{
    comboBox.Items.Add($"Item {i}");
}
comboBox.SelectionChanged += (sender, args) =>
{
    // comboBox.SelectedObject holds the newly selected item
    label.Text = $"Selected: {comboBox.SelectedObject}";
};
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAAA3VQTWvDMAz9K8LskEBZQ7ZdEnJYQymFwWDrZZCLU2uNh2OD7WzdQv77bMcNozAfLOlJ7-ljJHuzG3pSWD3ginDJLaeC_yApyCfVIGiLAiqQ-AVP3k_SspEBvX1k7KBelLIB89VH1bdqo86RUMcw5C-5K9oCvzlSfu-Qd6UTLi1wB2SlM82QZXc1PDjfu_lm_tNGjo0E9xaNvcXe-AbJTajJPQAjn-bI95v-tHxFgUfLlaw7Kk_IYBauIDEoGeoVUH0yKVRhgu3Sb72GKw1kz-2Hs9ApwQzYDv0BxDeYmAbuJpnZ8_EOeLZuwTjnRaWA8R_luILfoCTTL8FPcWC3AQAA)
