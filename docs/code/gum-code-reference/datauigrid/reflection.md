# Reflection

The default behavior for DataUiGrid is to reflect properties from the assigned Instance. When the Instance is assigned, the Grid automatically populates its Categories with members based on reflection.&#x20;

```csharp
// Initially categories will be empty
DataGrid.Instance = someObject;
// Now, the Categories contains one instance, 
// and this category contains one InstanceMember for each reflected property
```

Properties which have the `System.ComponentModel.CategoryAttribute` are categorized according to the category assigned. Otherwise, properties are put in the Uncategorized category.

## Manually Selecting Properties

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

## Customizing InstanceMembers

Reflection-based InstanceMembers can be customized by changing their properties. For example, the following can be used to display a Name property which is read-only:

```csharp
var nameMember = category.Members.First(item => item.Name == "Name");
nameMember.IsReadOnly = true;
```

Individual getters and setters can be modified. For example, to add validation to an assignment, the setter can be modified as shown in the following code:

```csharp
nameMember.CustomSetPropertyEvent += (assignedInstance, args) =>
{
    var asMyType = (MyType)assignedInstance;
    var newName = (string)args.Value;
    if (DoesNameAlreadyExist(newName))
    {
        // Show some form of validation like a popup
        
        // by marking this as cancelled, the view dispaying the property
        // will not perform additional processing of the assignment:
        args.IsAssignmentCancelled = true;
    }
    else
    {
        asMyType.Name = newName;
    }
};
```
