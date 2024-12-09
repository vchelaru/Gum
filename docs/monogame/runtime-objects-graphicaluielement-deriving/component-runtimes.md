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

### SetProperty

The SetProperty method can be used to set properties on components which are not natively part of GraphicalUiElement. This is useful in the following situations:

* Setting a property which may not exist on all GraphicalUiElements, such as the Text property on a GraphicalUiElement for a Text standard element
* Setting a property which has been exposed

If a GraphicalUiElement is a Text instance, the Text property can be assigned through SetProperty as shown in the following code:

```csharp
myTextInstance.SetProperty("Text", "I'm set in code");
```

If a component has an exposed variable, then this variable can be assigned through SetProperty. For example, consider the following component which exposes its Text variable:

<figure><img src="../../.gitbook/assets/image (2) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Component exposing a Text variable</p></figcaption></figure>

This can be assigned through the SetProperty. Be sure to use the name exactly as it appears in Gum:

```csharp
myExposedVariableComponentInstance.SetProperty("Text", "I'm set in code");
```

### Apply State

States can be applied in code by string (unqualified name), or by accessing the state on the backing ComponentSave. Note, this can also be performed on screens and standards.

```csharp
 var setMeInCode = currentScreenElement.GetGraphicalUiElementByName("SetMeInCode");

 // States can be found in the Gum element's Categories and applied:
 var stateToSet = setMeInCode.ElementSave.Categories
     .FirstOrDefault(item => item.Name == "RightSideCategory")
     .States.Find(item => item.Name == "Blue");
 setMeInCode.ApplyState(stateToSet);

 // Alternatively states can be set in an "unqualified" way, which can be easier, but can 
 // result in unexpected behavior if there are multiple states with the same name:
 setMeInCode.ApplyState("Green");

 // states can be constructed dynamically too. This state makes the SetMeInCode instance bigger:
 var dynamicState = new StateSave();
 dynamicState.Variables.Add(new VariableSave()
 {
     Value = 300f,
     Name = "Width",
     Type = "float",
     // values can exist on a state but be "disabled"
     SetsValue = true
 });
 dynamicState.Variables.Add(new VariableSave()
 {
     Value = 250f,
     Name = "Height",
     Type = "float",
     SetsValue = true
 });
 setMeInCode.ApplyState(dynamicState);

```
