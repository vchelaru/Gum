# Children

## Introduction

The Children collection contains the direct descend children of the GraphicalUiElement. An instance's children will report the instance as their parent.

Note that Screen GraphicalUiElements have `null` for Children. The reason is because Screen GraphicalUiElements do not have a position or size - they are merely containers for children without providing any layout information. Therefore, to access the items that a Screen contains, see the `ContainedElements` property.

## Children in Gum

If a GraphicalUiElement is loaded in a game project, its Children property contains the direct children as established in Gum.

For example, consider a component with six children named ColoredRectangleInstance, ColoredRectangleInstance1, ... , ColoredRectangleInstance5:

<figure><img src="../../../.gitbook/assets/image (160).png" alt=""><figcaption><p>ExampleComponent with six stacked children</p></figcaption></figure>

If using code generation, these could be accessed by their names. If not using code generation, or if you need to access each item by index, then the component's Children property provides access.

For example, the following code could be used to set the children width:

```csharp
for(int i = 0; i < ExampleComponentInstance.Children.Count; i++)
{
    var child = (GraphicalUiElement)ExampleComponentInstance.Children[i];
    child.Width = 100;
}
```

Of course, if you know the type of the children, you can cast the child to its specific type. Be careful doing this, as you may end up with an invalid cast exception.

For example, the following code could be used to edit the children as ColoredRectangleRuntimes:

```csharp
for(int i = 0; i < ExampleComponentInstance.Children.Count; i++)
{
    var child = (ColoredRectangleRuntime)ExampleComponentInstance.Children[i];
    // change any property that is specific to ColoredRectangleRuntime
}
```
