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

<figure><img src="../../.gitbook/assets/27_07 29 52.gif" alt=""><figcaption><p>ComboBox displaying a list of items</p></figcaption></figure>

## Adjusting the Drop-Down ListBox

`ComboBox` provides a `ListBox` property which can be used to customize the ListBox instance.

This can be modified, even if it hasn't yet been shown. The following code shows how to modify the default size of the ListBox:

```csharp
// Initialize
var comboBox = new ComboBox();
comboBox.AddToRoot();
comboBox.Anchor(Anchor.Top);
comboBox.ListBox.Height = 400;

for(int i = 0; i < 40; i++)
{
    comboBox.Items.Add($"Item {i+1}");
}
```

<figure><img src="../../.gitbook/assets/25_07 59 59.png" alt=""><figcaption><p>ComboBox ListBox with custom Height</p></figcaption></figure>

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
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACqvm5VJQUPIsdi_NVbJSKCkqTdUBi2TmZZZkJuZkVqUChZXKEosUkvNzk_Kd8isUbBXyUssVnKFcDU3rmDyYnJ5jSkpIflB-fglYOC2_SCMzr0QhE6jHwBpI2SiYAiltbc2YvOqYPAUggOv0LEnNLQbp11CJUQJxFKoza2OUQMbUIlkQnJqTmlySmuKZl5IKcoqRtRIvVy0An04fSsYAAAA" target="_blank">Try on XnaFiddle.NET</a>

## SelectionChanged

The `SelectionChanged` event fires whenever the user picks a different item. The handler receives a `SelectionChangedEventArgs` with information about the previously and newly selected items. You can also read `SelectedObject` or `SelectedIndex` directly inside the handler.

```csharp
// Initialize
var comboBox = new ComboBox();
comboBox.AddToRoot();
for(int i = 0; i < 5; i++)
{
    comboBox.Items.Add($"Item {i}");
}
comboBox.SelectionChanged += (sender, args) =>
{
    // comboBox.SelectedObject holds the newly selected item
    System.Console.WriteLine($"Selected: {comboBox.SelectedObject}");
};
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACnVPwUrDQBC9F_oPw-IhISX14qUxguYgBUGwgpdc0u7YjGxmYHer1pB_d7dtai_u5e28x3tvpp9OANTSPe46tQBvdzg7MMTkqTH0g4FWn42FjXRreZBvKIHxC6rTmKRFzaOW32v9Ki8i_kC_i02IPVDwXBcBbuEmQJalNfc1Q3hn59Jj56I_uapVHKCnoVYxZrgoWKHBjSfhqm14ixqyEhKHrNHOoLFbl0J5d06fz_8Kjk7Uz-uPgNCK0Q58i_Easwd3koFC99G92rvwzythJwbzNxukJ2KMG45pC-j_aRiXL9R0MvwCXTCUyGcBAAA" target="_blank">Try on XnaFiddle.NET</a>
