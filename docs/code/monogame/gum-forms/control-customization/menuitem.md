# MenuItem

## Introduction

This page shows how to customize MenuItem instances.

## MenuItem and DefaultMenuItem

By default instances of MenuItem use a Visual of type DefaultMenuItemRuntime. The full source for this type can be found here:

{% embed url="https://github.com/vchelaru/Gum/blob/master/MonoGameGum/Forms/DefaultVisuals/DefaultMenuItemRuntime.cs" %}

{% hint style="info" %}
The DefaultMenuItem class is likely to change in future. Please refer to the source if your code is changing these defaults.
{% endhint %}

At the time of this writing, the DefaultMenuItem contains three items:

* The DefaultMenuItem itself which inherits from InteractiveGue. Any property on this can be modified to customize the MenuItem's appearance
* TextInstance which is of type TextRuntime. Any property on TextInstance can be modified to customize the MenuItem's appearance
* Background which is of type ColoredRectangleRuntime. This instance can be modified, but some properties are reserved for modification in response to state changes. At the time of this writing its Visible and Color properties are reserved for modification through states. If these properties are directly set they may be overwritten in response to state changes such as highlighting or selecting the menu item.

## Customizing a MenuItem

By default MenuItem instances use a Visual which inherits from `DefaultMenuItemRuntime`. Instances can access GraphialUiElements or States by name to modify the appearance of the MenuItem.

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

The code above shows how to access a TextRuntime instance by calling GetGrahpicalUiElementByName. One the TextRuntime has been obtained, any property can be modified. The code above changes `Red`, `Green`, and `Blue`, but any property could be modified including changing the TextRuntime's font. For more information on working with TextRuntime instances, see the [TextRuntime page](../../runtime-objects-graphicaluielement-deriving/textruntime.md).

