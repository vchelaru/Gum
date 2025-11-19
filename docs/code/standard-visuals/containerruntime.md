# ContainerRuntime

### Introduction

ContainerRuntime is a GraphicalUiElement-inheriting object used to organize and perform layout on a specific group of objects. Examples of when to use a container include:

* Providing margins inside the screen or another container
* Aligning or orienting children along a common position
* Changing children layout types, such as stacking children horizontally inside a parent container which stacks its children vertically
* To inject spacing between objects when using ratio width or height

ContainerRuntime instances have no visuals, so they cannot be directly observed in game.

### Example - Creating a ContainerRuntime

To create a ContainerRuntime, instantiate it and add it to the managers as shown in the following code:

```csharp
var container = new ContainerRuntime();
container.Width = 150; // by default, containers use absolute width...
container.Height = 150; // ...and height.
container.AddToManagers(SystemManagers.Default, null);
```

### Children (Containers as Parents)

Containers are usually used as parents for other runtime objects. To add another runtime instance to a container, add it to the Children list as shown in the following code:

```csharp
var parentContainer = new ContainerRuntime();
parentContainer.AddToManagers(SystemManagers.Default, null);

var childText = new TextRuntime();
childText.Text = "I am a child TextRuntime";
parentContainer.Children.Add(childText);
```

Notice that only the parent object needs to have its AddToManagers method called. Any child added to a parent which has been added to managers is automatically added as well. This membership is cascaded through all children, so if your project has a single root object, then only that root object needs to be added (or removed) from managers.

The following code shows a parent container added to managers. The child container and child text do not need to be added to managers:

```csharp
var parentContainer = new ContainerRuntime();
parentContainer.AddToManagers(SystemManagers.Default, null);

var childContainer = new ContainerRuntime();
// By adding the childContainer to the parentContainer, we do not need
// to call childContainer.AddToManagers
parentContainer.Children.Add(childContainer);

var textRuntime = new TextRuntime();
textRuntime.Text = "I do not need my AddToManagers method called either.";
childContainer.Children.Add(textRuntime);
```
