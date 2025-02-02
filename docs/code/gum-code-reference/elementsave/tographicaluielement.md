# ToGraphicalUiElement

## Introduction

ToGraphicalUiElement creates a new GraphicalUiElement (visual Gum object) from the calling ElementSave. This method is typically used to create new Screen or Component instances from a Gum project.

The conversion of a GraphicalUiElement can optionally add the resulting GraphicalUiElement to managers. This value should be true if the object is not going to be added as a child of any other GraphicalUiElement.

## Code Example

The following code can be used to convert a Screen named "MainMenu" to a GraphicalUiElement. In this case, the MainMenu is obtained from a loaded Gum project:

```csharp
// assuming gumProject is a valid Gum project:

var screen = gumProject.Screens.Find(item => item.Name == "MainMenu");
// Calling GraphicalUiElement creates the visuals for the screen
var graphicalUiElement = screen.ToGraphicalUiElement(
    RenderingLibrary.SystemManagers.Default, addToManagers: true);
```

### addToManagers Parameter

The `addToManagers` parameter determined whether the GraphicalUiElement is automatically added to managers so that it is directly drawn. This can be called after the element is created, so the following code shows two ways of adding the element to managers:

```csharp
screen.ToGraphicalUiElement(
    RenderingLibrary.SystemManagers.Default, addToManagers: true);
// is the same as:
var graphicalUiElement = screen.ToGraphicalUiElement(
    RenderingLibrary.SystemManagers.Default, addToManagers: false);
graphicalUiElement.AddToManagers();
```

All GraphicalUiElements need to either be added to managers directly or indirectly. Elements added directly to managers are drawn by `GumService.Default.Draw();` . Any item that is added to managers automatically draws its children, so only root-most objects should be added to managers.&#x20;

Let's look at common scenarios and and discuss when to set `addToManagers` to true.

#### Setting addToManagers to true

Elements should have `addToManagers` set to `true` if any of the following are true:

1. The GraphicalUiElement serves as the root for all other elements. Typically this would be when a Gum project is loaded, the Screen is obtained from the Gum project's `Screens` property and its `ToGraphicalUiElement` method is called.
2. You are performing simple tests such as adding a Gum component to screen for testing, you may want to set addToManagers to true so that it shows up on screen. Note that if this is an object with events (such as a Gum Forms object), it must either be passed to the `GumService.Default.Update` call or it will not respond to the mouse, keyboard, or gamepads.

The most common case for setting AddToManagers to true is if you are creating a Gum screen from a loaded Gum project.

#### Setting addToManagers to false

Elements should have `addToManagers` set to `false` if any of the following are true:

1. If the GraphicalUiElement is being added as a child of another element which has already been added to managers. For example, if a new element is being created and added to an existing Screen or one of its children.
2. If the GraphicalUiElement is being added as a child to existing _root_ containers, such as `FrameworkElement.PopupRoot` or `FrameworkElement.ModalRoot` .
3. If the GraphicalUiElement is being added to managers on a dedicated layer. For example, a Screen could be created and added to a dedicated UI layer to control sorting of all elements in the Screen relative to other manually-created GraphicalUiElements.
