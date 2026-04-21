# ListBox

## Introduction

The ListBox control provides a scrollable list of ListBoxItems for displaying and selecting from a list.

## Code Example: Adding a ListBox

The following code adds items to a ListBox when a button is clicked. When an item is added, `ScrollIntoView` is called so the item is shown.

```csharp
// Initialize
var listBox = new ListBox();
listBox.AddToRoot();
listBox.X = 50;
listBox.Y = 50;
listBox.Width = 400;
listBox.Height = 200;

var button = new Button();
button.AddToRoot();
button.X = 50;
button.Y = 270;
button.Text = "Add to ListBox";
button.Click += (s, e) =>
{
    var newItem = $"Item {listBox.Items.Count} @ {DateTime.Now}";
    listBox.Items.Add(newItem);
    listBox.ScrollIntoView(newItem);
};
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAACl2PX0vDMBTF3wv9DpfgQ4ejjKEIjopughbEBy3-gbzUNbiLaQLtrR2WfneTLNNsecr55eSec4c4AmB5e9fV7BKo6cTUEVRIWEr8EQaz77IBiS0t9RYyUKKHh51KJguu_Et6U1WFftKaDuib-XE-C8D7MXjFijYGns1Cei_wc0MGzx3mypb46Ii08h2WTriwHT9q4OFfAa9t_vwiAIXY2hzOzHcgvd-Ns3_LSuL6C04zSNopiAlkV1wNXIE5tpZpk5OozZATztxt2K9hVZuudKdohGsYbksSBdYifdT96CLskEO36ZH4kXaP0PC8brSUuSL9gqIPXeOCxdEYR78IY1Gh0gEAAA)

<figure><img src="../../.gitbook/assets/13_09 05 48.gif" alt=""><figcaption><p>Adding items to a ListBox by clicking a button</p></figcaption></figure>

## Items

The Items property contains the data that is displayed by the ListBox. Whenever an object is added to Items, the ListBox creates a ListBoxItem instance.

Any object can be added to Items. By default, `ToList` is called on any added item. The following code shows how `int` and `string` instances can be added and mixed in a ListBox:

```csharp
// Initialize
StackPanel stackPanel = new();
stackPanel.AddToRoot();
stackPanel.Anchor(Gum.Wireframe.Anchor.Center);

ListBox listBox = new();
stackPanel.AddChild(listBox);

Button addIntButton = new();
addIntButton.Text = "Add Integer";
stackPanel.AddChild(addIntButton);
addIntButton.Click += (_, _) =>
{
    listBox.Items.Add(new Random().Next(0, 100));
};

Button addStringButton = new();
addStringButton.Text = "Add String";
stackPanel.AddChild(addStringButton);
addStringButton.Click += (_, _) =>
{
    listBox.Items.Add("Hello " + DateTime.Now.Ticks);
};
```

Try on XnaFiddle.NET

<figure><img src="../../.gitbook/assets/26_21 44 00.gif" alt=""><figcaption><p>Items added to a ListBox</p></figcaption></figure>

### Adding ListBoxItems to Items

If a ListBoxItem is added directly to the Items property, then the ListBox uses this ListBoxItem directly rather than creating a new ListBoxItem. This simplifies the creation of ListBoxItems.

For example, the following code shows how to create ListBoxItems with custom colors:

```csharp
// Initialize
ListBox listBox = new();
listBox.AddToRoot();
listBox.Anchor(Gum.Wireframe.Anchor.Center);

for(int i  = 0; i < 5; i++)
{
    ListBoxItem item = new();
    item.UpdateToObject("Item " + i);

    var visual = (ListBoxItemVisual)item.Visual;

    visual.ForegroundColor = Color.Pink;
    visual.HighlightedBackgroundColor = Color.Red;
    visual.SelectedBackgroundColor = Color.Green;

    // add the ListBoxitem directly
    listBox.Items.Add(item);
}
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAAA3VRTU8CMRD9K01PuwkpBOIF4sHFiCQmGkQ8uB7qdoCRbmvaLn4Q_rv9WEQSbbKd6dv3ptM3Ozq1k6amQ2ca6NDGolpZOnyiHmRX2tSWXcKSN9It0DZcWrYY0OcORYUOucQvoEN6g9YV-oPINp4TBe9ZPipVi7ALIeZ6prU7RVW11iYLVz2igaXhNbQgG4NyYAJ76SmoHEHiC_dGPpZNrzcYkzOfh7RfpD0v1a5UxK-2oamDmmDYjh2F3wFiD2-CO5jr25dXqFwWS_SjIqUpFASDKum23JBttMEXzH5dkrzJY92UHzXxGKyEldGNEmMttfH6GNkdqs3ohHmNq7X0nwNR8Grzl2gG4lRzD9K_4X_BxACon5a6XcKFIG4NB6OiR8KPoHLyM5EOMwrPs2F-WSAFL_Z0_w08LEF_NgIAAA)

<figure><img src="../../.gitbook/assets/26_21 51 15.gif" alt=""><figcaption><p>ListBoxItems directly added</p></figcaption></figure>

### Removing Items

Items can be removed by reference with `Remove`, by index with `RemoveAt`, or all at once with `Clear`.

The following code adds a remove button that deletes the currently selected item from the ListBox.

```csharp
// Initialize
var listBox = new ListBox();
listBox.AddToRoot();
listBox.X = 50;
listBox.Y = 50;
listBox.Width = 400;
listBox.Height = 200;

for (int i = 0; i < 10; i++)
{
    listBox.Items.Add("Item " + i);
}

var removeButton = new Button();
removeButton.AddToRoot();
removeButton.X = 50;
removeButton.Y = 270;
removeButton.Text = "Remove Selected";
removeButton.Click += (_, _) =>
{
    if (listBox.SelectedObject != null)
    {
        listBox.Items.Remove(listBox.SelectedObject);
    }
};
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAACnWQW0vDMBSA3wf7D8c8pXRIHYpgreB80IEgzMEUAmOumTuaJtCmczj63z3pZbQd9uU037nkfDkMBwBsmj3mCbsBm-ZyVBLUaHGl8FcSZrtVCgozOzF7iEDLH3iuTtwLha4z5_dxPDczY2yHvlHHVdAC732wwNhuCV4Gbfok8XNrCY9LLPTGpMBRW0CCQUjhFi5c9H1P6IPQQF_TPbUyydxGXDD3D4KBD-gWK9wwZ5TKxOzkJLfW6FqrOpT7t7M9tU7q6NehTnJ8fYLncu-UBJuVFF6lkmsrY8H6lQ8K19_gR8CXI1h6EN0dHXEDvPFsBrx8fFGEM9LIlaL3cIV1_em7VLf_M8QZuhZ6pyJkw0HxB_VbbiUkAgAA)

## Selection

ListBox items can be selected. The ListBox class provides a number of ways to work with the selection.

Selection can be set by index:

```csharp
// Initialize
listBox.SelectedIndex = 0;
```

Item can also be selected by the reference itself, assuming the referenced item is part of the list box:

```csharp
// Initialize
listBox.SelectedObject = "English";
```

Selection can be cleared by setting `SelectedIndex` to -1:

```csharp
// Initialize
listBox.SelectedIndex = -1;
```

Whenever the selection changes, the SelectionChanged event is raised:

```csharp
// Initialize
listBox.SelectionChanged += (sender, args) =>
{
    System.Diagnostics.Debug.WriteLine($"Selected item: {listBox.SelectedObject}");
};
```

## Multi-Selection

Multi-selection can be controlled through the SelectionMode property.&#x20;

```csharp
ListBox listBox = new();
listBox.AddToRoot();
listBox.Anchor(Anchor.Center);

for(int i  = 0; i < 10; i++)
{
    ListBoxItem item = new();
    item.UpdateToObject("Item " + i);
    listBox.Items.Add(item);
}

// Uncomment one of the following:
// Only 1 at a time
//listBox.SelectionMode = SelectionMode.Single;

// Multiple just by clicking to select/deselect
listBox.SelectionMode = SelectionMode.Multiple;

// Multiple, through CTRL and SHIFT clicks
//listBox.SelectionMode = SelectionMode.Extended;
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAAA1VPwWrDMAz9FaNTCsVk3S1hh6WHUlgZbB07LDtkjbqqOHJxZDZW-u-zmpRSH_SkJ-n56QjLfhE7KCREnELsib97KD4gkfadAm5D0yF8ToGYhBpHfwgFPFEvlf81bsQHw_iTTcqaR8Y-tu3av3gvtyxvdj5kA9g5smDQ_jaRxGLIJKm8TFjHPL-fmzstNJ9VQ5zUfKzZpDd6WAp2hjRcTWhbKft2aBvBtX_-2uNGsrPE7LwxpANUhi5bF6M60-sRmepo93S94hVdUiPPK99i-vamtqvohA4OSzj9Aw4ZK_xeAQAA)

## Reordering With Drag+Drop

The `DragDropReorderMode` controls whether the user can automatically reorder `ListBoxItems` by pushing on an item and dragging it to a new location. By default this value is set to `NoReorder`, but it can be changed to enable reordering.

The following code creates a ListBox which supports reordering:

```csharp
// Initialize
var listBox = new ListBox();
listBox.AddToRoot();

for(int i = 0; i < 10; i++)
{
    listBox.Items.Add("Item " + i);
}

listBox.DragDropReorderMode = DragDropReorderMode.Immediate;
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAACm2MwQrCMAyG74O9Q-hpYzL06vSgDGSgl-Gxl0KjBNZGuk7FsXe3nXozlz_5knxjmgCIpj8MRqzBuwEXMyFLnlRHLwxY3JWDjnq_5ydsweIDjp8pyytpv5typ_WZW2Y_U2kv7DKyHij8LKsQG1jFLIpc2lFaCPX7bTyaPhoyKWIPUkABFEVTdP3uaqeuteNbi-w0uhNrDPY_tGyMQU3KYyXSZEqTN_BbeGDqAAAA)

<figure><img src="../../.gitbook/assets/13_09 06 43.gif" alt=""><figcaption><p>ListBoxItems reordering</p></figcaption></figure>

## Customizing with VisualTemplate

The VisualTemplate lets you customize the type of ListBoxItem created for the ListBox. The following code shows how to assign a VisualTemplate to a runtime object named CustomListBoxItemRuntime:

```csharp
// Initialize
// assign the template before adding new list items
listBox.VisualTemplate =
    new MonoGameGum.Forms.VisualTemplate(() =>
        // do not create a forms object because this template will be
        // automatically added to a ListBoxItem by the ListBox:
        new CustomListBoxItemRuntime(tryCreateFormsObject:false));
```

The VisualTemplate class can optionally pass a parameter representing the item in the list box. This can be used to create different types of items based on the object added. For example the following code would compare the passed object as an integer to whether it is greater than 100 and returns a different item depending on this result:

```csharp
// Initialize
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

## Customizing Displayed Property with ListBoxItemFormsType

By default the ListBox calls ToString on each item. This is usually okay if you are dealing with primitive types. For example, the following code adds sequential integers to a ListBox:

```csharp
// Initialize
for (int i = 0; i < 20; i++)
{
    listBox.Items.Add(i);
}
```

<figure><img src="../../.gitbook/assets/13_09 07 53.png" alt=""><figcaption><p>ListBox displaying integers</p></figcaption></figure>

Often you may want to add a list of items which should not use their ToString method. For example you may have a list of IDs which represent weapons in a dictionary. The display can be customized by inheriting from ListBoxItem as shown in the following code:

```csharp
// Initialize
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
