---
title: GumProjectSave
---

# GumProjectSave

## Introduction

The GumProjectSave object is a serializable object representing a Gum project. Specifically, the GumProjectSave object represents the data in the .gumx file. The GumProjectSave provides access to all Screens (ScreenSave), Components (ComponentSave), Standard Elements (StandardElementSave), and Behaviors (BehaviorSave).

The **Save** suffix is added to any model which can e serialized (saved) to disk.

Game projects can load a GumProjectSave through GumServices.Initialize.

The Gum tool loads GumProjectSave instances when the user opens a .gumx file.

Both games and the Gum tool can access the current GumProjectSave through the `ObjectFinder.Self.GumProjectSave` property.

## Project Structure

A fully-loaded Gum project is a hierarchical structure. GumProjectSave is the root of this hierarchy, providing access to all contained objects.&#x20;

Since everything in a GumProjectSave can be saved to disk, all types have the Save suffix. Some important types include:

* ScreenSave - a Screen in the Gum project, saved as a .gusx file
* ComponentSave - a Component in the Gum project, saved as a .gucx file
* StandardElementSave - a StandardElement in the Gum project, saved as .gutx
* BehaviorSave - a Behavior in the Gum project, saved as a .behx
* ElementSave - the base type for ScreenSave, ComponentSave, and StandardElementSave
* InstanceSave - an instance in an Element or Behavior. These instances include the name of the instance and its type. InstanceSaves do not include variables, and are not embedded within other InstanceSaves. The parent/child relationships are stored as variables.
* StateSave - a State in an Element or Behavior. These can be the default State, or can be contained within a CategorySave.
* CategorySave - a Category in an Element or Behavior. These can include any number of States.
* VariableSave - a variable including its name and value. If the variable applies to the element itself, then the variable name is _unqualified_, such as "X". If the variable applies to an instance, then the variable includes the name of the instance, such as "TitleScreen.XUnits".

The following shows how a typical Gum project might be structured. Keep in mind that Gum projects can be very large so the following is a simplified example.

* GumProjectSave
  * Screens (List\<ScreenSave>)
    * CreditsScreen
    * TitleScreen
    * ...
  * Components (List\<ComponentSave>)
    * Button
      * DefaultState
    * Label
    * ...
  * StandardElements (List\<StandardElementSave)
    * ColoredRectangle
    * Sprite
    * Text
    * ...
  * Behaviors (List\<BehaviorSave>)
    * ButtonBehavior
    * LabelBehavior
    * ...

Each ScreenSave, ComponentSave, StandardElementSave and BehaviorSave contains its own variables including the instances that each includes, states, categories, and variables.

Each ScreenSave, ComponentSave, and StandardElementSave includes a list of InstanceSaves, CategorySaves, and a Default state. A typical ScreenSave might have the following structure:



* Instances
  * CreditsTitle
  * LeftSideContainer
  * RightSideContainer
  * LeftCredits1
  * RightCredits1
  * LeftCredits2
  * RightCredits2
  * ...
* DefaultState
  * Variables
    * CreditsTitle.Text
    * CreditsTitle.X
    * CreditsTitle.XUnits
    * LeftSideContainer.X
    * RightSidContainer.X
    * ...
* Categories
  * FadeInOutCategory
    * FadeIn
      * Variables
        * Overlay.Alpha
    * FadeOut
      * Variables
        * Overlay.Alpha
  * ...

## Code Example

The following code loops through all screens, components, and standard elements in the current project and adds their names to a list of strings:

```csharp
List<string> allObjectsInGumProject = new List<string>();

foreach (var screen in ObjectFinder.Self.GumProjectSave.Screens)
{
    allObjectsInGumProject.Add(screen.Name);
}

foreach (var component in ObjectFinder.Self.GumProjectSave.Components)
{
    allObjectsInGumProject.Add(component.Name);
}

foreach (var standardElement in ObjectFinder.Self.GumProjectSave.StandardElements)
{
    allObjectsInGumProject.Add(standardElement.Name);
}
```
