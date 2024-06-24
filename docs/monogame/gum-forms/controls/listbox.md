# ListBox

### Introduction

The ListBox control provides a scrollable list of ListBoxItems for displaying and selecting from a list.

### Code Example: Adding a ListBox

The following code adds items to a ListBox when a button is clicked. When an item is added, `ScrollIntoView` is called so the item is shown.

```csharp
var listBox = new ListBox();
this.Root.Children.Add(listBox.Visual);
listBox.X = 50;
listBox.Y = 50;
listBox.Width = 400;
listBox.Height = 200;

var button = new Button();
this.Root.Children.Add(button.Visual);
button.X = 50;
button.Y = 270;
button.Width = 200;
button.Height = 40;
button.Text = "Add to ListBox";
button.Click += (s, e) =>
{
    var newItem = $"Item @ {DateTime.Now}";
    listBox.Items.Add(newItem);
    listBox.ScrollIntoView(newItem);
};
```

<figure><img src="../../../.gitbook/assets/24_06 50 24.gif" alt=""><figcaption><p>Addign items to a ListBox by clicking a button</p></figcaption></figure>
