---
title: SelectedState
---

# SelectedState

## Introduction

The `ISelectedState` service gives you information about what the user has selected in the tool. This includes which ElementSave (that is ScreenSave, ComponentSave, or StandardElementSave), InstanceSave, and StateSave is selected.

## Getting ISelectedState in a plugin

A plugin receives `ISelectedState` through its constructor. Mark the constructor with `[ImportingConstructor]`:

```csharp
// Class scope
private readonly Gum.ToolStates.ISelectedState _selectedState;

[System.ComponentModel.Composition.ImportingConstructor]
public MyPlugin(Gum.ToolStates.ISelectedState selectedState)
{
    _selectedState = selectedState;
}
```

## Simple example

To get the current ScreenSave:

```csharp
// Initialize
var currentScreen = _selectedState.SelectedScreen;
```

To get the current InstanceSave:

```csharp
// Initialize
var currentInstance = _selectedState.SelectedInstance;
```

{% hint style="info" %}
Older plugins used the static `Gum.ToolStates.SelectedState.Self`. The current tool does not have it; use the injected `ISelectedState` instead.
{% endhint %}
