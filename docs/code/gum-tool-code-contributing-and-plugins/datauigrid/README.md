# DataUiGrid

## Introduction

The DataUiGrid is similar to the Windows Forms PropertyGrid: a reflection-based control which displays the properties on an object in real time. It can also display properties through explicit get and set methods rather than reflection. Gum uses it for the **Variables** tab, but it is general purpose and can be used in any Avalonia application.

The grid is split into two projects in the Gum repository:

* `DataUi.Core` holds the data model (`MemberCategory`, `InstanceMember`, and the `IDataUi` interface). It has no UI framework dependency.
* `AvaloniaDataUi` holds the Avalonia `DataUiGrid` control and its editors.

The DataUiGrid can be used with reflection or its Categories can be manually populated. Using reflection is easier to set up, but does not provide as much flexibility. Manually building up Categories takes more work, but provides the most flexibility.

{% hint style="info" %}
The Gum tool used to be a WPF application, and older versions of this page described the WPF grid in `WpfDataUi`. The data model types keep their `WpfDataUi` and `WpfDataUi.DataTypes` namespaces, so code that builds categories and instance members does not change.
{% endhint %}

## Adding References

Add a project reference to `AvaloniaDataUi/AvaloniaDataUi.csproj`. It brings `DataUi.Core` with it.

## Adding a DataUiGrid in Code

Construct a grid in code like any other Avalonia control:

```csharp
// Initialize
var grid = new AvaloniaDataUi.DataUiGrid();
// add the grid to a layout control such as a Grid or StackPanel...
```

## Using the grid in code

To use the grid, set its Instance property to the object you want to view. For example:

```csharp
// Initialize
// A MemoryStream shows that the grid works with any object:
System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();

grid.Instance = memoryStream;
```

The grid displays one row for each public property on the object, grouped into categories.
