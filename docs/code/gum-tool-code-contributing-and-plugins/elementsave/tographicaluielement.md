# ToGraphicalUiElement

## Introduction

ToGraphicalUiElement creates a new GraphicalUiElement (visual Gum object) from the calling ElementSave. This method is typically used to create new Screen or Component instances from a Gum project.

The conversion of a GraphicalUiElement can optionally add the resulting GraphicalUiElement to manager, but this is considered an advance approach. Almost all cases should use the no-argument version of ToGraphicalUiElement.

## Code Example - ToGraphicalUiElement on Screen

The following code can be used to convert a Screen named "MainMenu" to a GraphicalUiElement. In this case, the MainMenu is obtained from a loaded Gum project:

```csharp
// assuming gumProject is a valid Gum project:

var screen = gumProject.Screens.Find(item => item.Name == "MainMenu");
// Calling ToGraphicalUiElement creates the visuals for the screen
var graphicalUiElement = screen.ToGraphicalUiElement();
graphicalUiElement.AddToRoot();
```

## Code Example - ToGraphicalUiElement on Component

The following code creates components and adds them to an existing ScrollViewer. The ScrollViewer could be created in code or also in Gum (as part of a screen).

```csharp
// assuming gumProject is a valid Gum project
// and that ScrollViewerInstance is also a valid ScrollViewer:

var component = gumProject.Components.Find(item => item.Name == "CustomButton");

// Calling ToGraphicalUiElement creates an instance of the compnent. This can be
// called multiple times:
for(int i = 0; i < 10; i++)
{
    var customButtonInstance = component.ToGraphicalUiElement();
    ScrollViewerInstance.AddChild(customButtonInstance);
}
```
