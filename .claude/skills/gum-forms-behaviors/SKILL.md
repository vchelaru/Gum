---
name: gum-forms-behaviors
description: Gum's behaviors system and the design-time → runtime Forms wrapping lifecycle. Triggers: BehaviorSave, ElementBehaviorReference, StandardFormsBehaviorNames, FormsUtilities.RegisterFromFileFormRuntimeDefaults, DefaultFromFile*Runtime classes, Forms properties not settable at design time.
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

## The Property Promotion Gap (partially closed)

The Gum tool operates at the visual layer — layout, colors, fonts, dimensions saved as `VariableSave` entries. The Forms behavioral layer (state, data, interaction) is added entirely at runtime. The save model historically had no bridge to Forms semantics.

### Behavior FormsProperties — the v1 bridge

`BehaviorSave.FormsProperties` (a `List<VariableSave>`) lets a behavior declare design-time properties that flow through to its wrapped `FrameworkElement` at runtime via reflection. Each entry's `Name` must match a property on the corresponding control (e.g. `ToolTip` on `FrameworkElement`); `Value` is the design-time default.

The variable grid in the tool synthesizes a "Behavior" category for these declarations on both the component definition and any instance. When a component sets a default on a promoted property, the standard properties pass picks it up under General first; `ElementSaveDisplayer.AddBehaviorFormsPropertyMembers` relocates it into "Behavior" so categorization stays consistent.

At runtime, `BehaviorFormsPropertyApplier.Apply` is invoked from `FormsUtilities`'s `InitialStateAppliedNotifier` (after the parent's `SetInitialState` completes). It walks the GUE's `ElementSave.Behaviors` (and inherited base-type behaviors), reads each `FormsProperty`'s effective value — preferring parent-state instance overrides like screen `"ButtonInstance.ToolTip"` over the element's own default — and reflects onto the wrapped `FrameworkElement`. The hook lives at the notifier (not the `Visual` setter) because instance-qualified overrides on the parent state aren't visible until after the parent's state has been applied.

### What's covered today

`ToolTip` on every standard Forms behavior. The reflection apply is generic, so any new logical-only `FormsProperty` declaration (e.g. `MaxCharacters`, `Slider.Minimum/Maximum/Value`) would flow through automatically without runtime changes.

### What's still pending (state-mapped properties)

`IsEnabled`, `IsChecked`, and similar properties that have a *visual* state consequence (the Disabled visual category) need a second mechanism: behavior-level tool-only variable references that drive `<Category>State` from the FormsProperty value. That depends on engine work in `#2638` (ternary expressions and category-state LHS in `EvaluatedSyntax` / `VariableReferenceLogic`) and is tracked under v2 of `#2637`.

Examples still requiring code-side initialization:
- `CheckBox.IsChecked` / `ToggleButton.IsChecked` — needs visual state binding
- `Button.IsEnabled` / `TextBox.IsEnabled` — needs visual state binding
- `Button.Text` / `Label.Text` — visual-only pass-through, authored on the child `TextInstance` directly

## Key Files

| Path | Purpose |
|------|---------|
| `GumDataTypes/Behaviors/BehaviorSave.cs` | Behavior definition model (`.behx`) |
| `GumDataTypes/Behaviors/ElementBehaviorReference.cs` | Per-element reference holding only `BehaviorName` |
| `GumDataTypes/Behaviors/StandardFormsBehaviorNames.cs` | String constants for all standard behavior names |
| `GumDataTypes/ElementSave.cs` | `Behaviors` list on components/screens |
| `MonoGameGum/Forms/FormsUtilities.cs` | `RegisterFromFileFormRuntimeDefaults()` — drives the mapping; sets `InitialStateAppliedNotifier` to invoke `BehaviorFormsPropertyApplier` |
| `MonoGameGum/Forms/DefaultFromFileVisuals/` | `DefaultFromFileXxxRuntime` classes — `AfterFullCreation()` creates Forms objects |
| `MonoGameGum/Forms/BehaviorFormsPropertyApplier.cs` | Reflects `BehaviorSave.FormsProperties` values onto the wrapped `FrameworkElement` |
| `Gum/Plugins/InternalPlugins/VariableGrid/ElementSaveDisplayer.cs` | `AddBehaviorFormsPropertyMembers` — surfaces FormsProperties in the variable grid |
| `GumRuntime/InteractiveGue.cs` | `FormsControlAsObject` back-link property |
