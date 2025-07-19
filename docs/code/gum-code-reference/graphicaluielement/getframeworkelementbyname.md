# GetFrameworkElementByName

## Introduction

`GetFrameworkElementByName` is a generic method which returns an instance of a framework element with the matching name. This method can be used if your project does not use generated code, or if the controls with in a `GraphicalUiElement` are dynamically created.

`GetFrameworkElementByName` is an extension method so it requires the following using statement:

```csharp
using MonoGameGum.Forms;
```

`GetFrameworkElementByName` searches recursively so it will return the first instance with a matching name even if that instance is nested within other components.

## Code Example - Getting a Button from a Screen

The following code shows how to get a Button instance from a screen which is loaded from a Gum project.

```csharp
var project = GumUI.Initialize(this, "GumProject/GumProject.gumx");

var screen = project.Screens.Find(item => item.Name == "MainMenu");
var screenRuntime = screen.ToGraphicalUiElement();
screenRuntime.AddToRoot();

var button = screenRuntime.GetFrameworkElementByName<Controls.Button>("PlayButton");
button.Click += (_, _) => System.Diagnostics.Debug.WriteLine("Play was clicked");
```

As mentioned above, Gum performs recursive searches for an item by name. If your project potentially includes multiple items with the same name, you can qualify the name of an object by including the name of its parents. For example, consider a situation where `PlayButton` is contained in a parent named `MenuContainer`.&#x20;

<figure><img src="../../../.gitbook/assets/01_06 33 15 (1).png" alt=""><figcaption><p>PlayButton as a child of MenuContainer</p></figcaption></figure>

The button can be obtained by qualified name as shown in the following code block:

```csharp
var button = screenRuntime.GetFrameworkElementByName<Controls.Button>("MenuContainer.PlayButton");
button.Click += (_, _) => System.Diagnostics.Debug.WriteLine("Play was clicked");
```

## Exceptions

GetFrameworkElementByName throws informative exceptions if it encounters an error when trying to find a `FrameworkElement`. Exceptions occur in the following situations:

* No item is found with a matching name
* An item is found, but it is a visual that does not have a FrameworkElement associated with it
* An item is found but the type does not match the generic type

If your code expects that these items may not exist, you can call `TryGetFrameworkElementByName` instead, which returns `null` instead of throwing exceptions when encountering any of the problems above.

