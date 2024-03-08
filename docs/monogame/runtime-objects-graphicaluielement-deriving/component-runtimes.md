# Component Runtimes

### Introduction

Components in a Gum project can be instantiated in custom code. Note that this is not a requirement to use a component since you can also add an instance of the component in your screen.

### Code Example - Instantiating a Component

Before instantiating a component, you must first load the project using the standard .gumx loading code as shown in the following code block:

```csharp
var gumProject = GumProjectSave.Load("GumProject.gumx", out _);
ObjectFinder.Self.GumProjectSave = gumProject;
gumProject.Initialize();
```

Once you have instantiated your project, you can create a component as shown in the following code:

```csharp
// This assumes that your project has at least 1 component
gumProject.Components.First()
    .ToGraphicalUiElement(SystemManagers.Default, addToManagers:true);
```

The Components property contains a list of all components, so you can also access components by name or other property. For example, the First method can be used to find a component by name as shown in the following code:

```csharp
var componentSave = ObjectFinder.Self.GumProjectSave.Components
    .First(item => item.Name == "ColoredRectangleComponent");

var componentRuntime = componentSave.ToGraphicalUiElement(SystemManagers.Default, addToManagers: true);
// the componentRuntime can be modified here
```

Note that the name passed to the First method should match the name given in Gum. For example, in this case the code searches for a component named ColoredRectangleComponent.

<figure><img src="../../.gitbook/assets/image (41).png" alt=""><figcaption><p>ColoredRectangleComponent in Gum</p></figcaption></figure>

If a component is in a folder, then its name is the qualified name relative to the Components folder. For example, the following component's name at runtime is `"Buttons/StandardButton"`

<figure><img src="../../.gitbook/assets/image (42).png" alt=""><figcaption><p>StandardButton component in the Buttons folder in Gum</p></figcaption></figure>

The ToGraphicalUiElement method can automatically add the component to the root for rendering, or alternatively it can be added to an existing container. If adding to an existing container, then the ToGraphicalUiElement's addToManagers parameter should be false as shown in the following code:

```csharp
var component = var componentSave = ObjectFinder.Self.GumProjectSave.Components
    .First(item => item.Name == "MyComponent");
    
var componentRuntime = componentSave.ToGraphicalUiElement(
    SystemManagers.Default, addToManagers: false);
// This assumes that container has been directly added to managers, or is a 
// child of a root container which has been added to managers:
container.Children.Add(componentRuntime);
```

### Troubleshooting Component Creation

If your component is not visible, this may be a file issue. By default Gum project loading will not throw exceptions on missing files, and it will attempt to re-create missing file components. For more information, see the [Troubleshooting section in the Loading .gumx page](../loading-.gumx-gum-project.md#troubleshooting-gum-project-loading).
