# RemoveFromManagers

## Introduction

RemoveFromManagers removes the calling GraphicalUiElement from the SystemManagers. This method should be called on GraphicalUiElements which need to be destroyed but do not have parents. To remove a GraphicalUiElement which has parents, remove it from its Parent's Children.

## Code Example

The following code shows how to add and remove a GraphicalUiElement.

```csharp
// You can create a GrahicalUiElement from an ElementSave.
// Assume elementSave is valid, such as a Screen obtained from a Gum project
var graphicalUiElement = elementSave.ToGraphicalUiElement(
    RenderingLibrary.SystemManagers.Default, addToManagers: true);
// alternatively, could pass addToManagers:false, and explicitly call
// AddToManagers

// ...later...
graphicalUiElement.RemoveFromManagers();
```

Keep in mind that if a GraphicalUiElement is not added to managers, it does not need to be removed from managers. For more info see the Parent page.
