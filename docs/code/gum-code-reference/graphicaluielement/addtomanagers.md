# AddToManagers

### Introduction

AddToManagers is the older way to display a GraphicalUiElement as a root-most object, and it is marked obsolete. Call `AddToRoot()` instead, which parents the element to `GumService.Default.Root` so it is drawn, receives input, and is laid out.

```csharp
var graphicalUiElement = elementSave.ToGraphicalUiElement();
graphicalUiElement.AddToRoot();
```

To remove the element later, see [RemoveFromManagers](removefrommanagers.md).
