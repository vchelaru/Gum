# ComboBox

## Introduction

The ComboBox control provides a compact way for users to select from a list of options.

## Code Example: Adding a ComboBox

```csharp
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
