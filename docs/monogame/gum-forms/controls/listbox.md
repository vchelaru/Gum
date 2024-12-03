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

The VisualTemplate class can optionally pass a parameter representing the item in the list box. This can be used to create different types of items based on the object added. For example the following code would compare the passed object as an integer to whether it is greater than 100 and returns a different item depending on this result:

```csharp
// assign the template before adding new list items
listBox.VisualTemplate = 
    new MonoGameGum.Forms.VisualTemplate((item) => 
    {
        // Be sure you know the type before casting:
        var itemAsInt = (int)item;
        if(itemAsInt > 100)
        {
            return new CustomListBoxItemHighValue(tryCreateFormsObject:false);
        }
        else
        {
            return new CustomListBoxItemLowValue(tryCreateFormsObject:false);
        }
        
    }
```

{% hint style="info" %}
Note that the code above uses the item to decide which type of list box item to create. If your list box item needs to react to changes on the item, you can also pass the item in the constructor to your list box. This allows for additional initialization based on the item. You can also hold on to the reference of the item to react to changes that may happen on the item, or to push changes to the item.

In other words, you are free to use the item for your game's needs; however, keep in mind that UpdateToObject will be called after your ListBoxItem is constructed. For more information on how to customize UpdateToObject, see the section below.
{% endhint %}

### Customizing Displayed Property with ListBoxItemFormsType

By default the ListBox calls ToString on each item. This is usually okay if you are dealing with primitive types. For example, the following code adds sequential integers to a ListBox:

```csharp
for (int i = 0; i < 20; i++)
{
    listBox.Items.Add(i);
}
```

<figure><img src="../../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>ListBox displaying integers</p></figcaption></figure>

Often you may want to add a list of items which should not use their ToString method. For example you may have a list of IDs which represent weapons in a dictionary. The display can be customized by inheriting from ListBoxItem as shown in the following code:

```csharp
listBox.ListBoxItemFormsType = typeof(WeaponDisplayingListBoxItem);

foreach(var weapon in weapons)
{
    listBox.Items.Add(weapon.Id);
}

// define WeaponDisplayingListBoxItem
class WeaponDisplayingListBoxItem : ListBoxItem
{
    public WeaponDisplayingListBoxItem(InteractiveGue gue) : base(gue) { }
    public override void UpdateToObject(object o)
    {
        var idAsInt = (int)o;
        // assuming this has access to Weapons:
        var weapon = Weapons[idAsInt];
        coreText.RawText = weapon.Name;
    }
}
```
