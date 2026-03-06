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

**Qualified variable names:** In `VariableSave.Name`, a dot separates an instance name from a property name (`"InstanceName.PropertyName"`). `VariableSave.SourceObject` and `VariableSave.RootName` are computed helpers that split this. Element-level variables have no dot and `SourceObject` is null. `EventSave.Name` follows the same convention; use `GetSourceObject()` / `GetRootName()`.

**Conditional serialization:** Many properties on save classes are omitted from XML when they hold default values, using `ShouldSerializeXxx()` methods. Don't assume a missing XML element means the property doesn't exist — it likely just holds its default value.

**`[XmlIgnore]` vs serialized:** Runtime-only data (parent references, UI hints, event callbacks) is tagged `[XmlIgnore]` and never written to disk. Only the structural/data properties are serialized.

**States and categories:** An element has both a flat `States` list (uncategorized) and a `Categories` list of `StateSaveCategory`, each of which has its own `States` list. `AllStates` (on `ElementSave`) enumerates both. The first uncategorized state is conventionally named `"Default"`.

**`VariableReferences` list:** Cross-element variable binding is stored as a `VariableListSave<string>` whose `Name` is `"VariableReferences"` or `"InstanceName.VariableReferences"`. Each string entry is `"LeftSide = RightSide"` where the right side is a qualified path like `"Components/MyComp.InstanceName.Width"`. An optional state prefix can appear before a colon: `"Highlighted:Components/MyComp.InstanceName.Width"`. Rename logic must update both sides.

**`GumProjectSave` reference lists vs. loaded lists:** The `.gumx` file serializes `ScreenReferences`, `ComponentReferences`, `StandardElementReferences`, `BehaviorReferences` (each a `List<ElementReference>` or `List<BehaviorReference>`). The `[XmlIgnore]` properties `Screens`, `Components`, `StandardElements`, `Behaviors` hold the loaded objects. Both must be updated on rename: the reference list (for `.gumx`) and the live objects (for in-memory state). `AllElements` is a computed `[XmlIgnore]` property that enumerates Screens + Components + Standards.

**Clone methods:** All save classes have a `Clone()` method that produces a deep copy via `FileManager.CloneSaveObject`. Cloned instances have different object references than the originals — relevant when cross-referencing with live editor state.

---

## BehaviorSave Structure

`BehaviorSave` is referenced from `ElementSave.Behaviors` (`List<ElementBehaviorReference>`). `ElementBehaviorReference.BehaviorName` is the plain string name that must be updated on behavior rename.

Key fields on `BehaviorSave`:
- `RequiredVariables` — a single `StateSave` listing variables that implementing components must expose
- `Categories` — `List<StateSaveCategory>`, each with its own `States`; `AllStates` enumerates them
- `RequiredInstances` — `List<BehaviorInstanceSave>` (instances the component must contain)
- `RequiredAnimations` — `List<string>` animation names the component must implement

---

## Rename Cross-Reference Map

When any object is renamed, scan these fields across all elements:

| Renamed Object | Fields to Update | Where to Scan |
|---|---|---|
| **Screen / Component / StandardElement** | `ElementSave.BaseType`, `InstanceSave.BaseType`, `VariableSave.Value` where `GetRootName()=="ContainedType"`, `VariableListSave` VariableReferences right-hand side | All Screens + Components |
| **Instance (within an element)** | `VariableSave.Name` (SourceObject prefix), `EventSave.Name` (SourceObject prefix), `VariableSave.Value` where `GetRootName()=="DefaultChildContainer"`, `VariableSave.Value` where `GetRootName()=="Parent"` (value after the dot), `VariableListSave` VariableReferences left and right sides | Containing element + inheriting elements + elements referencing the container |
| **State** | `VariableSave.Value` where `GetRootName()==categoryName+"State"` in elements that use the element as an instance | Elements referencing the owner element |
| **StateSaveCategory** | `VariableSave.Type` == old category name, `VariableSave.Name` root (e.g. `"OldCategoryState"`) | All Screens + Components |
| **Exposed variable / VariableSave root name** | `VariableSave.ExposedAsName`, `VariableSave.Name` root in inheriting elements and instances, `VariableListSave` VariableReferences left and right sides | All elements |
| **BehaviorSave** | `ElementBehaviorReference.BehaviorName` in `ElementSave.Behaviors`, `BehaviorReference.Name` in `GumProjectSave.BehaviorReferences` | All Screens + Components (for ElementBehaviorReference); GumProjectSave (for BehaviorReferences) |

**Note:** `GetReferencesToElement` in `ReferenceFinder` only scans `Screens` and `Components` — it does not scan `StandardElements`. If a standard element inherits from another standard element and the base is renamed, that reference won't be found.

**Note:** Behavior rename is not yet implemented in `ReferenceFinder`. The method `GetReferencesToBehavior` does not exist; `ElementBehaviorReference.BehaviorName` will become stale on behavior rename.

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
| `ElementBehaviorReference` | `GumDataTypes/Behaviors/ElementBehaviorReference.cs` |
| `CustomPropertySave` | `GumDataTypes/CustomPropertySave.cs` |
