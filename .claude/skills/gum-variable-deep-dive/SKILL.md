---
name: gum-variable-deep-dive
description: >
  Deep dive into the full variable lifecycle — from VariableSave on ElementSave through
  runtime application on GraphicalUiElement and Forms controls. Load this when working on
  styling, theming, RefreshStyles, or when you need to understand how variable values flow
  from save data to live visuals.
---

# Variable Lifecycle Deep Dive

For individual subsystem references, see: **gum-tool-save-classes** (save model),
**gum-property-assignment** (instantiation + SetProperty), **gum-forms-controls** (Forms
state machine), **gum-runtime-variable-references** (ApplyAllVariableReferences).

This skill connects them into one end-to-end picture.

## The Three Layers

| Layer | Classes | Role |
|-------|---------|------|
| Save data | `ElementSave`, `StateSave`, `VariableSave`, `StateSaveCategory` | Serialized project data (XML). Edited by the Gum tool. |
| Visual runtime | `GraphicalUiElement` / `InteractiveGue` | Live layout + rendering. Holds references to StateSave objects. |
| Forms behavioral | `FrameworkElement` subclasses (Button, CheckBox, etc.) | Logical controls. Drive categorical state changes (Highlighted, Disabled, etc.) on the Visual. |

## Key Insight: Shared References, Not Copies

During `ToGraphicalUiElement` → `SetGraphicalUiElement`, the method
`AddStatesAndCategoriesRecursivelyToGue` stores the **same `StateSave` and
`StateSaveCategory` instances** from the `ElementSave` into the GUE's `mStates` and
`mCategories` dictionaries. No cloning occurs.

This means: when `ApplyAllVariableReferences()` modifies `VariableSave.Value` entries on an
`ElementSave`'s states, those changes are **immediately visible** through the GUE's state
dictionaries. The data is already updated — but the live visuals haven't re-read it yet.

## Instantiation Flow (One-Time)

`ElementSave.ToGraphicalUiElement()` → `SetGraphicalUiElement()`:

1. **AddStatesAndCategoriesRecursivelyToGue** — walks inheritance chain, stores state/category
   references on the GUE
2. **CreateGraphicalComponent** — creates the underlying `IRenderable`
3. **AddExposedVariablesRecursively** — registers exposed variable bindings
4. **CreateChildrenRecursively** — for each `InstanceSave`, calls
   `instance.ToGraphicalUiElement()` recursively (each child gets its own full instantiation)
5. **SetInitialState** → `SetVariablesRecursively` — applies default state values including
   instance-qualified variables (e.g., `"MyButton.Width"`) that set properties on children
6. **AfterFullCreation** — triggers Forms wrapping (e.g., `DefaultFromFileButtonRuntime`
   sets `FormsControlAsObject = new Button(this)`)

After this, save data changes do NOT automatically propagate. The GUE must be told to re-read.

## Runtime Usage: With vs Without a Gum Project

- **With a loaded project:** `ElementSave`/`ScreenSave`/`ComponentSave` are present.
  `ToGraphicalUiElement` creates the full tree. States on the GUE reference the ElementSave's
  states (shared instances).
- **Without a project:** `ElementSave` classes are not used. But `StateSave`,
  `StateSaveCategory`, and `VariableSave` are still used directly — code can create states
  in code and call `ApplyState()` on any GUE.

## Applying States at Runtime

### Default (uncategorized) state

`SetInitialState()` calls `SetVariablesRecursively(elementSave, elementSave.DefaultState)`,
which walks the inheritance chain (base type defaults first), then applies the element's own
default state via `ApplyState`. This includes instance-qualified variables that set properties
on children.

### Categorical states (Forms)

Forms controls call `UpdateState()` in response to interactions (hover, click, disable).
`UpdateState()` determines the current state name (e.g., "Highlighted") and calls
`Visual.SetProperty("ButtonCategoryState", stateName)`, which triggers `ApplyState` on the
matching `StateSave` within the category. This applies on top of the default state.

### The ordering matters

During creation, the order is: children get their own defaults first, then the parent's
default state applies instance-qualified overrides on top. Categorical states (Forms) are
applied last.

## Refreshing After Style Changes

After calling `ApplyAllVariableReferences()` to propagate variable reference changes into
`VariableSave` values, call `RefreshStyles()` to push those values to live visuals:

- `GraphicalUiElement.RefreshStyles()` — recursively re-applies default states and current
  Forms categorical states on a subtree
- `GumService.Default.RefreshStyles()` — convenience that calls `RefreshStyles()` on all
  three roots (Root, PopupRoot, ModalRoot)

Typical workflow:
```csharp
// 1. Change a style variable
var colorState = ObjectFinder.Self.GetElementSave("Standards/ColoredRectangle")
    .DefaultState;
colorState.GetVariableSave("Red").Value = 255;

// 2. Propagate variable references across the project
ObjectFinder.Self.GumProjectSave.ApplyAllVariableReferences();

// 3. Push to all live visuals
GumService.Default.RefreshStyles();
```
