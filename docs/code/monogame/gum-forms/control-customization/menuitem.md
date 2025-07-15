# MenuItem

## Introduction

This page shows how to customize MenuItem instances.

## Customizing MenuItem Text

The following code shows how to adjust the color of menu items:

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
    itemVisual.MenuItemCategory.RemoveValues("TextInstance.Color");

    var textRuntime = itemVisual.TextInstance;
    textRuntime.Color = new Color(
        100 + random.Next(155),
        100 + random.Next(155),
        100 + random.Next(155));

    fileMenuItem.Items.Add(item);
}
```

<figure><img src="../../../../.gitbook/assets/14_22 56 32.gif" alt=""><figcaption></figcaption></figure>

For more information on working with TextRuntime instances, see the [TextRuntime page](../../../gum-code-reference/textruntime/).

