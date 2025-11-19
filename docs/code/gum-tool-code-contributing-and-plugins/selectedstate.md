---
title: SelectedState
---

# SelectedState

## Introduction

The SelectedState class gives you information about what the user has selected in the app. This includes which ElementSave \(that is ScreenSave, ComponentSave, or StandardElementSave\)/InstanceSave/StateSave is selected.

## Simple example:

To get the current ScreenSave:

```text
var currentScreen = Gum.ToolStates.SelectedState.Self.SelectedScreen;
```

To get the current InstanceSave:

```text
var currentInstance = Gum.ToolStates.SelectedState.Self.SelectedInstance;
```

