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
