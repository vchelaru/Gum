---
title: Categories
---

# Categories

## Introduction

The Categories member provides more detailed control over a DataUiGrid. The easiest way to work with a DataUiGrid is to assign its Instance and let it automatically create UI for all public members. Working with Categories requires more code but gives the most flexibility in setting up a grid.

## Example

The following makes the grid only show a single value called "Some Value" which always has a value of 10. An actual implementation may modify some backing variable.

```text
            var category = new MemberCategory("Test Category");

            var instanceMember = new InstanceMember("Some value", this);
            instanceMember.CustomSetEvent += (owner, value) =>
            {
                System.Console.WriteLine($"Setting the value of {owner} to {value}");
            };

            instanceMember.CustomGetEvent += (owner) =>
            {
                System.Console.WriteLine($"Returning the value for {owner}");
                return 10;
            };

            instanceMember.CustomGetTypeEvent += (owner) =>
            {
                System.Console.WriteLine($"Returning the type for {owner}");

                return typeof(int);
            };

            category.Members.Add(instanceMember);

            Grid.Categories.Add(category);
```

