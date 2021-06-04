---
title: GumProjectSave
---

# Gum.DataTypes.GumProjectSave

## Introduction

The GumProjectSave object is a serializable object representing a Gum project. Specifically, the GumProjectSave object can directly serialize to the .gumx file format. The GumProjectSave provides access to all Screens, Components, and Standard Elements. Typically the GumProjectSave is accessed through the ProjectState singleton object.

## Code Example

The following code loops through all screens, components, and standard elements in the current project and adds their names to a list of strings:

```text
List<string> allObjectsInGumProject = new List<string>();

foreach (var screen in Gum.ToolStates.ProjectState.Self.GumProjectSave.Screens)
{
    allObjectsInGumProject.Add(screen.Name);
}

foreach (var component in Gum.ToolStates.ProjectState.Self.GumProjectSave.Components)
{
    allObjectsInGumProject.Add(component.Name);
}

foreach (var standardElement in Gum.ToolStates.ProjectState.Self.GumProjectSave.StandardElements)
{
    allObjectsInGumProject.Add(standardElement.Name);
}
```

