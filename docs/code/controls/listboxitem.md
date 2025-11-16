# ListBoxItem

## Introduction

The ListBoxItem class is used by the ListBox control for each instance added to the ListBox.Items property.

Gum Forms includes a default ListBoxItem implementation which includes a single label.

## Code Example: Adding ListBoxItems

ListBoxItems can be implicitly instantiated by adding any type of object to a ListBox. The following code creates 20 ListBoxItems, each displaying an integer.

```csharp
var listBox = new ListBox();
listBox.AddToRoot();
listBox.X = 50;
listBox.Y = 50;
listBox.Width = 400;
listBox.Height = 200;

for(int i = 0; i < 20; i++)
{
    listBox.Items.Add(i);
}
```

<figure><img src="../../.gitbook/assets/13_09 10 37.gif" alt=""><figcaption><p>ListBoxItems created by adding ints to an Items</p></figcaption></figure>

## Customizing ListBox Items

To customize the displayed text of a ListBoxItem, see the [ListBox Items tutorial](../getting-started/tutorials/code-only-gum-forms-tutorial/listbox-items.md).
