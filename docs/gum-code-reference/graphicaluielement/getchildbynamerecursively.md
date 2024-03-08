# GetChildByNameRecursively

### Introduction

GetChildByNameRecursively returns a GraphicalUiElement by the argument name. This method is recursive so it searches the entire child hierarchy to find a child. If no match is found, null is returned.

Note that this performs a [depth-first search](https://en.wikipedia.org/wiki/Depth-first\_search), returning the first instance found. Therefore, if multiple items exist with the same name, this may result in confusion.

### Code Example - Finding TextInstance

The following code can be used to find a Text by the name TextInstance and set its Text property to "You found me":

```csharp
var textInstance = RuntimeInstance.GetChildByNameRecursively("TextInstance")
     as GraphicalUiElement;
textInstance.SetProperty("Text", "You found me");
```
