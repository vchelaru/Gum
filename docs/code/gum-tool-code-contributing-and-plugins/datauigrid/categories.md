---
title: Categories
---

# Categories

## Introduction

The Categories member provides more detailed control over a DataUiGrid. The easiest way to work with a DataUiGrid is to assign its Instance and let it automatically create UI for all public members. Working with Categories requires more code but gives the most flexibility in setting up a grid.

## Example

The following makes the grid only show a single value called "Some Value" which always has a value of 10. An actual implementation may modify some backing variable.

```csharp
// Initialize
var category = new WpfDataUi.DataTypes.MemberCategory("Test Category");

var instanceMember = new WpfDataUi.DataTypes.InstanceMember("Some value", this);
instanceMember.CustomSetEvent += (owner, value) =>
{
    // Assign value to your backing variable here
};

instanceMember.CustomGetEvent += (owner) =>
{
    return 10;
};

instanceMember.CustomGetTypeEvent += (owner) =>
{
    return typeof(int);
};

category.Members.Add(instanceMember);

grid.Categories.Add(category);
```
