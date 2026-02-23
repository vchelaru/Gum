---
name: gum-tool-save-classes
description: Reference guide for Gum's save/load data model. Load this when working with GumProjectSave, ScreenSave, ComponentSave, StandardElementSave, ElementSave, StateSave, VariableSave, InstanceSave, BehaviorSave, or any serialization/deserialization of Gum project files.
---

# Gum Save/Load Data Model Reference

## Overview

Gum projects are serialized as XML files using .NET's `XmlSerializer`. Each logical type has its own file extension: `.gumx` (project), `.gusx` (screen), `.gucx` (component), `.gutx` (standard element), `.behx` (behavior).

All save classes live in `GumDataTypes/`.

---

## Class Relationships

`GumProjectSave` is the root. It stores only **references** to elements (screens, components, standards, behaviors) — not the element data itself. The actual element data lives in separate files and is loaded into `[XmlIgnore]` collections after deserialization. This is a deliberate two-phase loading pattern.

`ElementSave` is the abstract base for `ScreenSave`, `ComponentSave`, and `StandardElementSave`. All three are structurally identical — they differ only in subfolder and file extension. Each element owns a list of `StateSave`, `StateSaveCategory`, `InstanceSave`, and `EventSave`.

`StateSave` holds a list of `VariableSave` (and `VariableListSave`). A `VariableSave` stores a name/value pair. Variable names can be qualified with an instance name (e.g. `"MyButton.X"`) or unqualified for element-level values (e.g. `"Width"`).

`BehaviorSave` is independent of `ElementSave` but follows the same save/load pattern.

---

## Important Concepts

**Two-phase loading:** The `.gumx` file only records element references. After deserializing the project, a second pass loads each referenced element file from disk. Missing files are recorded in `GumLoadResult` rather than throwing — callers should check this object.

**Qualified variable names:** In `VariableSave.Name`, a dot separates an instance name from a property name (`"InstanceName.PropertyName"`). `VariableSave.SourceObject` and `VariableSave.RootName` are computed helpers that split this. Element-level variables have no dot and `SourceObject` is null.

**Conditional serialization:** Many properties on save classes are omitted from XML when they hold default values, using `ShouldSerializeXxx()` methods. Don't assume a missing XML element means the property doesn't exist — it likely just holds its default value.

**`[XmlIgnore]` vs serialized:** Runtime-only data (parent references, UI hints, event callbacks) is tagged `[XmlIgnore]` and never written to disk. Only the structural/data properties are serialized.

**States and categories:** An element has both a flat `States` list (uncategorized) and a `Categories` list of `StateSaveCategory`, each of which has its own `States` list. `AllStates` (on `ElementSave`) enumerates both. The first uncategorized state is conventionally named `"Default"`.

**Clone methods:** All save classes have a `Clone()` method that produces a deep copy via `FileManager.CloneSaveObject`. Cloned instances have different object references than the originals — relevant when cross-referencing with live editor state.

---

## File Locations

| Class | File |
|-------|------|
| `GumProjectSave` | `GumDataTypes/GumProjectSave.cs` |
| `ElementSave` (abstract) | `GumDataTypes/ElementSave.cs` |
| `ScreenSave`, `ComponentSave`, `StandardElementSave` | `GumDataTypes/` (one file each) |
| `StateSave`, `StateSaveCategory` | `GumDataTypes/Variables/` |
| `VariableSave`, `VariableListSave` | `GumDataTypes/Variables/` |
| `InstanceSave` | `GumDataTypes/InstanceSave.cs` |
| `EventSave` | `GumDataTypes/EventSave.cs` |
| `ElementReference` | `GumDataTypes/ElementReference.cs` |
| `BehaviorSave`, `BehaviorReference`, `BehaviorInstanceSave` | `GumDataTypes/Behaviors/` |
| `CustomPropertySave` | `GumDataTypes/CustomPropertySave.cs` |
