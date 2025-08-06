# ComboBox

## Introduction

This page shows how to customize a ComboBox.

## Customizing the ListBox

The ComboBoxVisual includes a ListBoxInstance which can be customized or completely replaced.

The following code shows how to change the ListBox background:

```csharp
var comboBox = new ComboBox();
comboBox.AddToRoot();

var comboBoxVisual = (ComboBoxVisual)comboBox.Visual;

var listBoxVisual = comboBoxVisual.ListBoxInstance;
listBoxVisual.Background.Color = Color.DarkGreen;
listBoxVisual.Name = "ListBoxInstance";
comboBoxVisual.ListBoxInstance = listBoxVisual;

for(int i = 0; i < 10; i++)
{
    comboBox.Items.Add($"Item {i}");
}
```

<figure><img src="../../../../.gitbook/assets/05_21 16 27.gif" alt=""><figcaption><p>ComboBox with customized ListBox background</p></figcaption></figure>
