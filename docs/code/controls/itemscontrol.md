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
// Initialize
var itemsControl = new ItemsControl();
itemsControl.AddToRoot();
for(int i = 0; i < 10; i++)
{
    itemsControl.Items.Add(i);
}
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACqvm5VJQUPIsdi_NVbJSKCkqTdUBi2TmZZZkJuZkVqUChZXKEosUMktSc4ud8_NKivJzFGwV8lLLFTyRhDQ0rWOAehACeo4pKSH5Qfn5JWCptPwijcy8EoVMoF4DayBlo2AIorW1NWPyqmPyFIAARTvYcJAhGpkg_bVKvFy1vFwA64IJNbAAAAA" target="_blank">Try on XnaFiddle.NET</a>

<figure><img src="../../.gitbook/assets/19_05 28 54.png" alt=""><figcaption><p>ItemsControl with ten items</p></figcaption></figure>

## Code Example: Fixed-Size Scrollable List

Setting explicit `Width` and `Height` values gives the ItemsControl a fixed size so that its content scrolls when it overflows. This is useful for displaying a non-selectable scrollable list in a fixed area.

```csharp
// Initialize
var itemsControl = new ItemsControl();
itemsControl.AddToRoot();
itemsControl.Width = 200;
itemsControl.Height = 150;
for(int i = 0; i < 20; i++)
{
    itemsControl.Items.Add("Item " + i);
}
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACmWOPwvCMBDF90K_w5EppSJRcLE6iIN2FcElSyHRHrQJpFcFS7-7ly7-u-Udv8e7e0OaAIiyO_StWAOF3s4mgg4JqwaflrG4VwGQbNvtvaPgG9iCsw8oP5DMCs2ZN5jvjDn7k_f0b13QUM1Hlkr9WkeLt5rYW6yid_VBoiNAJqpg2XCINc8z7QbtgOcrP3WKv6UWcQctIAeMFUaRJuML5gZQPvEAAAA" target="_blank">Try on XnaFiddle.NET</a>
