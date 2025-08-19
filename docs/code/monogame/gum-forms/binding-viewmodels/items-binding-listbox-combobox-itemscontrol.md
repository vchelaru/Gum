# Items Binding (ListBox, ComboBox, ItemsControl)

## Introduction

Some controls support the dynamic creation of children when their Items property is bound. These include `ListBox`, `ComboBox`, and `ItemsControl`. This page discusses how to dynamically create these items using the Items property.

## Items Binding Concepts

Controls which have an Items property automatically create children when an object is added to the Items property. This addition can be explicitly performed by calling `Items.Add` or can be performed by binding to an observable collection and changing that collection.

Although binding can be performed to any collection, `ObservableCollection` is commonly used so that the collection can change dynamically, automatically updating the displayed controls. All operations on `ObservableCollection` are supported including adding, removing, clearing, and reordering.

Binding can be performed to any type which implements `INotifyCollectionChanged`, so developers familiar with MVVM can use any view model implementation. This document uses Gum's `ViewModel` for simplicity.

## ItemsControl Binding

`ItemsControl` is similar to `ListBox`, but it does not have any restrictions on visual types since `ItemControl` does not support the concept of selection. This makes `ItemsControl` suitable for general usage.

The following code shows how to bind an `ItemsControl` to a `ViewModel`'s `ObservableCollection`:

```csharp
public class ExampleViewModel : ViewModel
{
    public ObservableCollection<DateTime> Dates
    {
        get;
        private set;
    } = new ObservableCollection<DateTime>();
}

//------------------------------------------

var viewModel = new ExampleViewModel();

var stackPanel = new StackPanel();
stackPanel.AddToRoot();
stackPanel.Anchor(Anchor.Center);

var itemsControl = new ItemsControl();
stackPanel.AddChild(itemsControl);
itemsControl.Width = 200;
itemsControl.BindingContext = viewModel;
itemsControl.SetBinding(
    nameof(itemsControl.Items),
    nameof(viewModel.Dates));

var addButton = new Button();
stackPanel.AddChild(addButton);
addButton.Text = "Add Date";
addButton.Click += (_, _) =>
{
    viewModel.Dates.Add(DateTime.Now);
};
```

<figure><img src="../../../../.gitbook/assets/19_06 21 40.gif" alt=""><figcaption><p>Items added through binding</p></figcaption></figure>

The visual template can be modified to support any visual. For example, we can add Button instances by using the ButtonVisual type as the template, as shown in the following code:

<pre class="language-csharp"><code class="lang-csharp">public class ExampleViewModel : ViewModel
{
    public ObservableCollection&#x3C;DateTime> Dates
    {
        get;
        private set;
    } = new ObservableCollection&#x3C;DateTime>();
}

//------------------------------------------

var viewModel = new ExampleViewModel();

var stackPanel = new StackPanel();
stackPanel.AddToRoot();
stackPanel.Anchor(Anchor.Center);

var itemsControl = new ItemsControl();
stackPanel.AddChild(itemsControl);
itemsControl.Width = 200;
itemsControl.BindingContext = viewModel;
itemsControl.SetBinding(
    nameof(itemsControl.Items),
    nameof(viewModel.Dates));

<strong>itemsControl.FrameworkElementTemplate = 
</strong><strong>    new Gum.Forms.FrameworkElementTemplate(typeof(Button));
</strong>
var addButton = new Button();
stackPanel.AddChild(addButton);
addButton.Text = "Add Date";
addButton.Click += (_, _) =>
{
    viewModel.Dates.Add(DateTime.Now);
};

</code></pre>

<figure><img src="../../../../.gitbook/assets/19_06 55 10.gif" alt=""><figcaption></figcaption></figure>
