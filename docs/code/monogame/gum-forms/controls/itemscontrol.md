# ItemsControl

## Introduction

`ItemsControl` provides a way to display a collection of controls. By default all contained controls are stacked and allow scrolling with a ScrollBar.

{% hint style="info" %}
ItemsControl is similar to ListBox, but it is more general since it does not support selection. When items are added to the Items property the ItemsControl does not create a control which inherits from ListBoxItem. Similarly, its VisualTemplate does not need to be a type which supports a ListBoxItem.
{% endhint %}

ItemsControl inherits from ScrollViewer. For more information about inherited proerties, see the [ScrollViewer](scrollviewer/) page.

## Code Example: Adding to Items Property

If an object is added to the `Items` property then the `ItemsControl` creates a view to represent this. By default this is a `Label` instance, but it can be customized through the `VisualTemplate` property.

The following code creates ten `Labels` by adding integers to the `Items` property.

```csharp
var itemsControl = new ItemsControl();
itemsControl.AddToRoot();
for(int i = 0; i < 10; i++)
{
    itemsControl.Items.Add(i);
}
```

<figure><img src="../../../../.gitbook/assets/19_05 28 54.png" alt=""><figcaption><p>ItemsControl with ten items</p></figcaption></figure>
