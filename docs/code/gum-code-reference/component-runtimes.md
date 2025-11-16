# Component Runtimes

## Introduction

Components in a Gum project can be instantiated in custom code. Custom components can be created in code for a number of reasons:

1. To create a variable number of items, such as items in an inventory component
2. To create popups which may appear at any point in the app

## Code Example - Instantiating a Component

Before instantiating a component from a Gum project, you must first have your Gum project loaded. For more information see the [Loading a Gum Project page](broken-reference).

Once you have instantiated your project, you can create a component as shown in the following code:

```csharp
// This assumes that your project has at least 1 component
var componentRuntime = ObjectFinder.Self.GumProjectSave.Components.First()
    .ToGraphicalUiElement();

// Add this component to the desired container
```

The Components property contains a list of all components, so you can also access components by name or other property. For example, the First method can be used to find a component by name as shown in the following code:

```csharp
var componentSave = ObjectFinder.Self.GumProjectSave.Components
    .First(item => item.Name == "ColoredRectangleComponent");

var componentRuntime = componentSave.ToGraphicalUiElement();
// the componentRuntime can be modified here, and added to the desired container
```

Note that the name passed to the First method should match the name given in Gum. For example, in this case the code searches for a component named ColoredRectangleComponent.

<figure><img src="../../.gitbook/assets/image (41).png" alt=""><figcaption><p>ColoredRectangleComponent in Gum</p></figcaption></figure>

If a component is in a folder, then use the qualified name relative to the Components folder. For example, the following component's name at runtime is `"Buttons/StandardButton"`

<figure><img src="../../.gitbook/assets/image (42).png" alt=""><figcaption><p>StandardButton component in the Buttons folder in Gum</p></figcaption></figure>

## Adding a Component Runtime to a Parent

A newly-created component must be added directly or indirectly to managers. The following are the possible ways to add to managers:

### Adding to a Container

The newly instantiated component can be added to a container in the screen. This container can be an actual ContainerRuntime instance, or to a control such as a ListBox.

```csharp
// do not add to managers, since it will be added to a container
var newComponentRuntime = componentSave.ToGraphicalUiElement();
// assuming ScreenRoot is a valid root
var container = ScreenRoot.GetGraphicalUiElementByName("DesiredContainer");
container.Children.Add(newComponentRuntime);
```

To destroy the component, remove it from its parent, or set its parent to null:

```csharp
newComponentRuntime.Parent = null;
```

### Adding to PopupRoot/ModalRoot

The newly instantiated component can be added either the PopupRoot or ModalRoot. PopupRoot is used for popups which should appear on top of other controls, but which should not steal input. ModalRoot is used for popups which should steal focus from all other controls.

The following code shows how to create a Toast instance to display a message to a user. The code assumes your code has a valid Toast component.

```csharp
// assumes toastComponent is a valid component
var newToastRuntime = toastComponent.ToGraphicalUiElement();
FrameworkElement.PopupRoot.Children.Add(newToastRuntime);
// the newToastRuntime needs to be removed from PopupRoot later
```

The following code shows how to create a MessageBox instance and add it to the ModalRoot so it receives exclusive input.

```csharp
// assumes toastComponent is a valid component
var newMessageBoxRuntime = messageBoxComponent.ToGraphicalUiElement();
FrameworkElement.ModalRoot.Children.Add(newMessageBoxRuntime);
// the newMessageBoxRuntime needs to be removed from ModalRoot later
```

To destroy the component, remove it from its parent, or set its parent to null:

```csharp
newMessageBoxRuntime.Parent = null;
```

### Adding to GumService Root

Components can be added directly to the GumService Root. Usually items are only added directly to GumService Root in the following situations:

* If they are a Screen, or will not be contained by any other object, such as if your project is not using screens.
* For testing/debugging.
* If your game has multiple roots, you can add instances to a list of GraphicalUiElements which are passed to Update. This is considered an advanced scenario.

```csharp
var newComponentRuntime = componentSave.ToGraphicalUiElement();
newComponentRuntime.AddToRoot();
```

To destroy the component, call RemoveFromRoot:

```csharp
newComponentRuntime.RemoveFromRoot();
```

## Troubleshooting Component Creation

If your component is not visible, this may be a file issue. By default Gum project loading will not throw exceptions on missing files, and it will attempt to re-create missing file components. For more information, see the [Troubleshooting section in the Loading .gumx page](broken-reference).

## SetProperty

The SetProperty method can be used to set properties on components which are not natively part of GraphicalUiElement. This is useful in the following situations:

* Setting a property which may not exist on all GraphicalUiElements, such as the Text property on a GraphicalUiElement for a Text standard element
* Setting a property which has been exposed

If a GraphicalUiElement is a Text instance, the Text property can be assigned through SetProperty as shown in the following code:

```csharp
myTextInstance.SetProperty("Text", "I'm set in code");
```

If a component has an exposed variable, then this variable can be assigned through SetProperty. For example, consider the following component which exposes its Text variable:

<figure><img src="../../.gitbook/assets/image (2) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Component exposing a Text variable</p></figcaption></figure>

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
