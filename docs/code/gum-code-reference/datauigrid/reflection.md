# Reflection

The default behavior for DataUiGrid is to reflect properties from the assigned Instance. When the Instance is assigned, the Grid automatically populates its Categories with members based on reflection.&#x20;

```csharp
// Initially categories will be empty
DataGrid.Instance = someObject;
// Now, the Categories contains one instance, 
// and this category contains one InstanceMember for each reflected property
```

Properties which have the `System.ComponentModel.CategoryAttribute` are categorized according to the category assigned. Otherwise, properties are put in the Uncategorized category.

### Manually Selecting Properties

By default all public fields and properties are displayed in a DataUiGrid. To control which properties are shown, the categories can be cleared, and individual InstanceMembers can be added for the desired properties.

For example, the following code clears the properties, and shows only X, Y, and Z:

```csharp
var dataGrid = MainSpineControl.DataGrid;
dataGrid.Instance = viewModel;

var category = dataGrid.Categories[0];

category.Members.Clear();

category.Members.Add(new InstanceMember("X", viewModel));
category.Members.Add(new InstanceMember("Y", viewModel));
category.Members.Add(new InstanceMember("Z", viewModel));

```

