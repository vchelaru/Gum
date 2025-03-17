# ToGraphicalUiElement

## Introduction

ToGraphicalUiElement creates a new GraphicalUiElement (visual Gum object) from the calling ElementSave. This method is typically used to create new Screen or Component instances from a Gum project.

The conversion of a GraphicalUiElement can optionally add the resulting GraphicalUiElement to manager, but this is considered an advance approach. Almost all cases should use the no-argument version of ToGraphicalUiElement.

## Code Example

The following code can be used to convert a Screen named "MainMenu" to a GraphicalUiElement. In this case, the MainMenu is obtained from a loaded Gum project:

```csharp
// assuming gumProject is a valid Gum project:

var screen = gumProject.Screens.Find(item => item.Name == "MainMenu");
// Calling GraphicalUiElement creates the visuals for the screen
var graphicalUiElement = screen.ToGraphicalUiElement();
graphicalUiElement.AddToRoot();
```
