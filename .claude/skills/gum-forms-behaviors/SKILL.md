---
name: gum-forms-behaviors
description: Covers Gum's behaviors system and the design-time → runtime Forms wrapping lifecycle. Load this when working on BehaviorSave, ElementBehaviorReference, StandardFormsBehaviorNames, FormsUtilities.RegisterFromFileFormRuntimeDefaults, DefaultFromFileXxxRuntime classes, or when investigating why Forms properties cannot be set at design time in the Gum tool.
---

# Gum Forms Behaviors

## What Behaviors Are

Behaviors are named capability contracts stored as `.behx` XML files in the project's `Behaviors/` folder. Each behavior (`BehaviorSave`) declares:
- A `Name` (e.g. `"ButtonBehavior"`)
- Required visual state `Categories` (with state names like Enabled/Disabled/Highlighted/Pushed)
- Optional `RequiredVariables` and `RequiredInstances`
- A `DefaultImplementation` path pointing to the default visual (e.g. `"Controls/ButtonStandard"`)

An `ElementSave` (component or screen) opts into a behavior via a `List<ElementBehaviorReference>`, where each reference holds only a `BehaviorName` string. This is the signal used at runtime to select which Forms control wraps the visual.

## The Wrapping Lifecycle

At project load time, call order is:

1. **`FormsUtilities.RegisterFromFileFormRuntimeDefaults()`** iterates every component in the loaded `GumProjectSave`, checks each component's `Behaviors` list against constants in `StandardFormsBehaviorNames`, and calls `ElementSaveExtensions.RegisterGueInstantiationType(component.Name, typeof(DefaultFromFileXxxRuntime))` for each match.

2. **`DefaultFromFileXxxRuntime`** (in `MonoGameGum/Forms/DefaultFromFileVisuals/`) is an `InteractiveGue` subclass selected as the runtime type. Its **`AfterFullCreation()`** override fires after the full visual tree is instantiated. Inside `AfterFullCreation()`, the runtime sets `FormsControlAsObject = new Button(this)` (passing itself as the visual), completing the pairing.

3. **`ReactToVisualChanged()`** fires on the Forms control when its `Visual` is assigned. The control caches named child references (`Visual.GetGraphicalUiElementByName(...)`) and `base.ReactToVisualChanged()` subscribes to input events and calls `UpdateState()`.

The `DefaultFromFileXxxRuntime` classes exist solely to bridge the file-loading path into the Forms object model. They are distinct from the `DefaultVisuals` classes (which serve the code-only construction path).

## Behavior → Forms Control Mapping

`StandardFormsBehaviorNames` constants → `DefaultFromFile` runtime registered:

| Behavior name constant | Runtime type |
|------------------------|-------------|
| `ButtonBehaviorName` | `DefaultFromFileButtonRuntime` |
| `CheckBoxBehaviorName` | `DefaultFromFileCheckBoxRuntime` |
| `ComboBoxBehaviorName` | `DefaultFromFileComboBoxRuntime` |
| `ListBoxBehaviorName` | `DefaultFromFileListBoxRuntime` |
| `TextBoxBehaviorName` | `DefaultFromFileTextBoxRuntime` |
| `LabelBehaviorName` | `DefaultFromFileLabelRuntime` / `DefaultFromFileLabelTextRuntime` |
| `ItemsControlBehaviorName` | `DefaultFromFileItemsControlRuntime` |
| `RadioButtonBehaviorName` | `DefaultFromFileRadioButtonRuntime` |
| `SliderBehaviorName` | `DefaultFromFileSliderRuntime` |
| `ScrollBarBehaviorName` | `DefaultFromFileScrollBarRuntime` |
| `ScrollViewerBehaviorName` | `DefaultFromFileScrollViewerRuntime` |
| `MenuBehaviorName` | `DefaultFromFileMenuRuntime` |
| `MenuItemBehaviorName` | `DefaultFromFileMenuItemRuntime` |
| `PasswordBoxBehaviorName` | `DefaultFromFilePasswordBoxRuntime` |
| `PanelBehaviorName` | `DefaultFromFilePanelRuntime` |
| `StackPanelBehaviorName` | `DefaultFromFileStackPanelRuntime` |
| `WindowBehaviorName` | `DefaultFromFileWindowRuntime` |

## The Property Promotion Gap

The Gum tool operates at the visual layer — layout, colors, fonts, dimensions saved as `VariableSave` entries. The Forms behavioral layer (state, data, interaction) is added entirely at runtime. No bridge exists between them.

**Concrete examples of Forms properties with no Gum variable equivalent:**
- `Button.Text` / `Label.Text` — visual has a `TextInstance` child, but no top-level "Text" variable on the component
- `CheckBox.IsChecked` / `ToggleButton.IsChecked` — runtime-only boolean, not representable in the save file
- `TextBox.Text` — initial text cannot be authored in the tool
- `Slider.Minimum`, `Slider.Maximum`, `Slider.Value` — numeric range exists only on the Forms control
- `ListBox.SelectionMode`, `ListBox.DisplayMemberPath`, `ItemsControl.Items` — entirely runtime constructs

**Why this gap exists:** The visual save model (`VariableSave`) has no notion of Forms semantics. The Gum tool has no awareness of which Forms properties correspond to which visual structure. The wrapping is a pure runtime event.

**Impact:** Game code must set all behavioral defaults in code after loading, even properties that logically belong to component design.

## Key Files

| Path | Purpose |
|------|---------|
| `GumDataTypes/Behaviors/BehaviorSave.cs` | Behavior definition model (`.behx`) |
| `GumDataTypes/Behaviors/ElementBehaviorReference.cs` | Per-element reference holding only `BehaviorName` |
| `GumDataTypes/Behaviors/StandardFormsBehaviorNames.cs` | String constants for all standard behavior names |
| `GumDataTypes/ElementSave.cs` | `Behaviors` list on components/screens |
| `MonoGameGum/Forms/FormsUtilities.cs` | `RegisterFromFileFormRuntimeDefaults()` — drives the mapping |
| `MonoGameGum/Forms/DefaultFromFileVisuals/` | `DefaultFromFileXxxRuntime` classes — `AfterFullCreation()` creates Forms objects |
| `GumRuntime/InteractiveGue.cs` | `FormsControlAsObject` back-link property |
