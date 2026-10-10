# RemoveFromManagers

## Introduction

RemoveFromManagers removes the calling GraphicalUiElement from the SystemManagers. It is the older counterpart to AddToManagers and is typically not called. Use `RemoveFromRoot()` instead, or clear the root.

## Code Example

The following code shows how to add and remove a GraphicalUiElement.

```csharp
// Assume elementSave is valid, such as a Screen obtained from a Gum project
var graphicalUiElement = elementSave.ToGraphicalUiElement();

graphicalUiElement.AddToRoot();

// ...later, removes only this element:
graphicalUiElement.RemoveFromRoot();
```

To remove everything under the root, such as when switching screens, clear its children:

```csharp
GumService.Default.Root.Children.Clear();
```

`PopupRoot` and `ModalRoot` are not affected. For a full example, see [Multiple Screens](../../getting-started/tutorials/gum-project-forms-tutorial/multiple-screens.md). For more info on parenting, see the [Parent](parent.md) page.
