# IsAllLayoutSuspended

## Introduction

The static IsAllLayoutSuspended property controls whether GraphicalUiElement layout calls are skipped. If this value is true, all GraphicalUiElements return immediately when calling Layout rather than performing a full layout. This includes explicit calls to Layout as well as calls performed as a result of changing properties such as Width.

This property is false by default, which mean layout is not suspended.

## Common Usage

By default changes to Gum immediately result in a layout. For example, consider the following code:

```csharp
var child = new ColoredRectangleRuntime();
parent.Children.Add(child);

child.X = 100;
child.Y = 200;
child.Width = 50;
child.Height = 30;
```

The parent in the code above may be sized according to its children. If so, any change to the child (X, Y, Width, and Height) cause the parent to update its layout. In addition, adding the child to the parent also results in a layout call. Furthermore, the parent may contain other children which would be updated in response to the parent. This code could result in hundreds or even thousands of layout calls.

However, typically a layout is not needed until all objects have been added and after all of their variables have been modified. Therefore, the following modifications could be made:

```csharp
GraphicalUiElement.IsAllLayoutSuspended = true;

var child = new ColoredRectangleRuntime();
parent.Children.Add(child);

child.X = 100;
child.Y = 200;
child.Width = 50;
child.Height = 30;

GraphicalUiElement.IsAllLayoutSuspended = false;
// after resuming layouts, explicitly call layout at the top level:
parent.UpdateLayout();
```

## Code Example - Measuring Layout Calls

The UpdateLayoutCallCount property can be used to measure the number of UpdateLayout calls. It can be used to compare the number of layout calls reduced by using IsAllLayoutSuspended.

For example, the followign code can be used to display layout calls:

```csharp
var stackingContainer = new ContainerRuntime();

stackingContainer.Width = 200;
stackingContainer.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
stackingContainer.StackSpacing = 3;
stackingContainer.WrapsChildren = true;
stackingContainer.Y = 10;
stackingContainer.Height = 10;
stackingContainer.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

for (int i = 0; i < 70; i++)
{
    var rectangle = new ColoredRectangleRuntime();
    stackingContainer.Children.Add(rectangle);
    rectangle.Width = 7;
    rectangle.Height = 7;
}

Root.Children.Add(stackingContainer);
var layoutCountAfter = GraphicalUiElement.UpdateLayoutCallCount;
System.Diagnostics.Debug.WriteLine($"Number of layout calls: {layoutCountAfter - layoutCountBefore}");
```

This produes a large number of layouts:

```
Number of layout calls: 15510
```

We can add additional code to suppress layouts until we are finished adding all children:

```csharp
var stackingContainer = new ContainerRuntime();

GraphicalUiElement.IsAllLayoutSuspended = true;// <---------New
stackingContainer.Width = 200;
stackingContainer.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
stackingContainer.StackSpacing = 3;
stackingContainer.WrapsChildren = true;
stackingContainer.Y = 10;
stackingContainer.Height = 10;
stackingContainer.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

for (int i = 0; i < 70; i++)
{
    var rectangle = new ColoredRectangleRuntime();
    stackingContainer.Children.Add(rectangle);
    rectangle.Width = 7;
    rectangle.Height = 7;
}

Root.Children.Add(stackingContainer);
GraphicalUiElement.IsAllLayoutSuspended = false; // <---------New
container.UpdateLayout(); // <---------New
var layoutCountAfter = GraphicalUiElement.UpdateLayoutCallCount;
System.Diagnostics.Debug.WriteLine($"Number of layout calls: {layoutCountAfter - layoutCountBefore}");
```

This results in far fewer layout calls:

```
Number of layout calls: 174
```
