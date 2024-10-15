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

### Selection

ListBox items can be selected. The ListBox class provides a number of ways to work with the selection.

Selection can be set by index:

```csharp
listBox.SelectedIndex = 0;
```

Item can also be selected by the reference itself, assuming the referenced item is part of the list box:

```csharp
listBox.SelectedObject = "English";
```

Whenever the selection changes, the SelectionChanged event is raised:

```csharp
listBox.SelectionChanged += (sender, args) =>
{
    System.Diagnostics.Debug.WriteLine($"Selected item: {listBox.SelectedObject}");
};
```

### Customizing with VisualTemplate

The VisualTemplate lets you customize the type of ListBoxItem created for the ListBox. The following code shows how to assign a VisualTemplate to a runtime object named CustomListBoxItemRuntime:

```csharp
// assign the template before adding new list items
listBox.VisualTemplate = 
    new MonoGameGum.Forms.VisualTemplate(() => 
        // do not create a forms object because this template will be
        // automatically added to a ListBoxItem by the ListBox:
        new CustomListBoxItemRuntime(tryCreateFormsObject:false));
```
