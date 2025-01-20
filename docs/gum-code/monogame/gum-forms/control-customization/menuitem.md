# MenuItem

## Introduction

This page shows how to customize MenuItem instances.

## Customizing a MenuItem

By default MenuItem instances use a Visual which inherits from `DefaultMenuItemRuntime`. Instances can access GraphialUiElements or States by name to modify the apperaance of the MenuItemRuntime.

The following code shows how to adjust the color of menu items:

```csharp
var random = new System.Random();

for(int i = 0; i < 10; i++)
{
    var item = new MenuItem();
    item.Header = "File " + i;
    var textRuntime = 
        (TextRuntime)item.Visual.GetGraphicalUiElementByName("TextInstance");

    textRuntime.Red = 100 + random.Next(155);
    textRuntime.Green = 100 + random.Next(155);
    textRuntime.Blue = 100 + random.Next(155);

    loadRecent.Items.Add(item);
}
```

<figure><img src="../../../../.gitbook/assets/20_15 51 35.png" alt=""><figcaption></figcaption></figure>
