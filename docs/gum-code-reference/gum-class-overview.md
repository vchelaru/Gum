---
title: Class Overview
---

# Gum Class Overview

## Introduction

This page discusses all of the classes in Gum at a high level to give you an understanding of how they relate to each other.

The following hierarchy shows a typical \(although small\) Gum project:

* Gum Project Save
  * ScreenSave
    * InstanceSave
    * InstanceSave
    * StateSave
      * VariableSave
      * VariableSave
      * VariableSave
  * ScreenSave \(etc...\)
  * ComponentSave
    * InstanceSave
    * StateSave
      * VariableSave
      * VariableSave
      * VariableSave
  * ComponentSave \(etc...\)
  * StandardElementSave
    * StateSave
      * VariableSave
      * VariableSave

## ElementSave

Notice that GumProjects contain ScreenSaves, ComponentSaves, and StandardElementSaves. ScreenSaves, ComponentSaves and StandardElementSaves all share the same base class: ElementSave. Therefore, a lot of Glue code will work with ElementSaves rather than the specific derived type.

ElementSaves can contain instances, and each instance is of type InstanceSave. Therefore, if you wanted to print out information about all of the instances in a given screen you might do something like this:

```text
var screen = SelectedState.Self.CurrentScreen;
foreach(var instance in screen.Instances)
{
    System.Console.WriteLine("The instance is named " + instance.Name + 
           "and its base type is " + instance.BaseType);
}
```

