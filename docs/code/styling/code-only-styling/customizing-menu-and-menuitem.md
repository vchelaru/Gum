# Customizing Menu and MenuItem

## Introduction

MenuItems can contain sub-items. This page discusses how to customize individual MenuItem instances as well as the container that holds MenuItem instances (ScrollViewer).

## Customizing MenuItem Text Color

```csharp
var menu = new Menu();
menu.AddToRoot();

var fileMenuItem = new MenuItem();
menu.Items.Add(fileMenuItem);
fileMenuItem.Header = "File";

var random = new System.Random();

for (int i = 0; i < 10; i++)
{
    var item = new MenuItem();
    item.Header = "File " + i;
    var itemVisual = (MenuItemVisual)item.Visual;
    // Remove the color values from the states so they can
    // be set directly
    itemVisual.ForegroundColor = new Color(
        100 + random.Next(155),
        100 + random.Next(155),
        100 + random.Next(155));

    fileMenuItem.Items.Add(item);
}
```

<figure><img src="../../../.gitbook/assets/14_22 56 32.gif" alt=""><figcaption></figcaption></figure>

For more information on working with `TextRuntime` instances, see the [TextRuntime page](../../standard-visuals/textruntime/).

## Customizing Sub-Item Background (ScrollViewer)

Each `MenuItem` can contain sub-items which appear when the user hovers over the `MenuItem`. These sub-items are shown as children of a `ScrollViewer` instance. This pop-up `ScrollViewer` can be customized by assigning the `MenuItem.ScrollViewerVisualTemplate` property.

The following code shows how to customize the `ScrollViewer` so it has a red background.

```csharp
var menu = new Menu();
menu.AddToRoot();

var menuItem = new MenuItem();
menu.Items.Add(menuItem);
menuItem.ScrollViewerVisualTemplate = new VisualTemplate(() =>
{
    var visual = new ScrollViewerVisual();
    visual.BackgroundColor = Color.Red;
    return visual;
}); 

menuItem.Header = "File";

for( int i = 0; i < 10; i++)
{
    var subItem = new MenuItem();
    subItem.Header = $"Sub Item {i + 1}";
    menuItem.Items.Add(subItem);
}
```

<figure><img src="../../../.gitbook/assets/05_08 04 19.gif" alt=""><figcaption><p>File menu displaying sub-items on a red background</p></figcaption></figure>
