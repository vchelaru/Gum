# Scrolling

## Introduction

Gum includes controls which have built-in scrolling support. These are:

* `ListBox`
* `ItemsControl`
* `ScrollViewer`

Both `ListBox` and `ItemsControl` inherit from `ScrollViewer`, so many of the properties for scrolling are the same across all controls.

## Built-In Scrolling Behavior

All controls support the following built-in actions for scrolling:

* Mouse/touch screen interactions with scroll bars
* Mouse wheel scrolling
* Touch screen swipe scrolling

## Scrolling Speed

Scrolling speed is controlled by the following two variables:

* `SmallChange` - The amount to scroll when a scroll bar button is clicked, or when a _tick_ is scrolled on the mouse wheel
* `LargeChange`  - The amount to scroll when the user clicks on the scroll bar track

The following code creates two `ListBoxes` to compare the scrolling speed of each.

```csharp
var defaultListBox = new ListBox();
defaultListBox.AddToRoot();
defaultListBox.Anchor(Anchor.Center);
defaultListBox.X = -100;
for (int i = 0; i < 20; i++)
{
    defaultListBox.Items.Add(i);
}

var fastListBox = new ListBox();
fastListBox.AddToRoot();
fastListBox.Anchor(Anchor.Center);
fastListBox.X = 100;
// default is 10:
fastListBox.SmallChange = 50;
for (int i = 0; i < 20; i++)
{
    fastListBox.Items.Add(i);
}
```

<figure><img src="../../.gitbook/assets/24_21 36 30.gif" alt=""><figcaption><p>Two ListBoxes with different SmallChange values</p></figcaption></figure>
