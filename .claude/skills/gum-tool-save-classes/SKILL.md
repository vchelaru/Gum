---
name: gum-tool-save-classes
description: Reference guide for Gum's save/load data model. Load this when working with GumProjectSave, ScreenSave, ComponentSave, StandardElementSave, ElementSave, StateSave, VariableSave, InstanceSave, BehaviorSave, or any serialization/deserialization of Gum project files.
---

# Gum Save/Load Data Model Reference

## Overview

Gum projects are serialized as XML files using .NET's `XmlSerializer`. Each logical type has its own file extension: `.gumx` (project), `.gusx` (screen), `.gucx` (component), `.gutx` (standard element), `.behx` (behavior).

All save classes live in `GumDataTypes/`.

---

## Save Classes vs Runtime Classes

The Gum tool's core responsibility is editing and serializing save classes (the data model) to XML — it operates purely on save classes. Visualization (the wireframe preview) requires runtime classes and a Gum runtime; the tool uses KNI for this via the `EditorTabPlugin_XNA` plugin, but other runtimes exist (MonoGame, FNA, Skia, Raylib). `Gum.csproj` should be save-class territory only. Runtime/rendering code that still lives there (e.g. `WireframeObjectManager`) is legacy being actively refactored out to plugins — do not add new runtime code to `Gum.csproj`.

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

**`VariableSave.SetsValue` and the three variable states:** A variable in a `StateSave` can be in one of three states:
1. **Not present** — the `VariableSave` does not exist in `StateSave.Variables`. The property uses its inherited/default value.
2. **Present with `SetsValue = false`** — the `VariableSave` exists but does not actively set a value. This state is required for **exposed variables**: when a component exposes an inner instance's property, the container's state must have a `VariableSave` entry with `SetsValue = false` so the exposed variable binding can resolve. Removing this variable would break the exposed variable chain. The edited icon does NOT show for these variables.
3. **Present with `SetsValue = true`** — the `VariableSave` actively sets its value. The edited icon shows in the tree view.

When reverting a variable after failed validation, you must restore the exact previous state — not just the value. If the variable didn't exist before, remove it from the list. If it existed with `SetsValue = false`, restore that. If it had a value, restore the value. Getting this wrong causes spurious undo entries or broken exposed variables.

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
