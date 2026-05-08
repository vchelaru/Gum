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

## The Property Promotion Gap (closed for logical + state-mapped)

The Gum tool operates at the visual layer — layout, colors, fonts, dimensions saved as `VariableSave` entries. The Forms behavioral layer (state, data, interaction) is added at runtime. Two mechanisms now bridge the save model into Forms semantics: **`FormsProperties`** for the value-on-the-control half, and **`ToolOnlyVariableReferences`** for the design-time visual-state preview half.

### `BehaviorSave.FormsProperties` (v1)

A `List<VariableSave>` declaring design-time properties that flow through to the wrapped `FrameworkElement` at runtime via reflection. Each entry's `Name` must match a property on the control (e.g. `ToolTip`, `IsEnabled`); `Value` is the **declared default** (used as a fallback tier — see below).

The variable grid synthesizes a "Behavior" category for these declarations on both the component definition and any instance (`ElementSaveDisplayer.AddBehaviorFormsPropertyMembers`). When a component sets a default on a promoted property under General, the displayer relocates it into "Behavior" to keep categorization consistent.

### `BehaviorSave.ToolOnlyVariableReferences` (v2)

A `List<string>` of variable-reference assignments evaluated **only at design time** to drive wireframe preview. Example on `ButtonBehavior`:

```
ButtonCategoryState = IsEnabled ? "Enabled" : "Disabled"
```

Tool-time apply (`BehaviorToolOnlyReferencesApplier`, in the variable-grid plugin) walks every linked behavior's `ToolOnlyVariableReferences`, evaluates the RHS via `EvaluatedSyntax` against the component's effective state, and writes the resolved value back into the state via `SetValue`. For instances, bare RHS identifiers are auto-qualified with the instance name. Hooked into `VariableReferenceLogic.DoVariableReferenceReaction` immediately after the existing state-level apply.

**The runtime apply path never traverses this list** — that's what the `ToolOnly` name encodes structurally. At runtime, the Forms control's own setter (e.g. `FrameworkElement.IsEnabled` → `UpdateState()`) owns the visual; applying the reference again would double-write.

### Three-tier default resolution

Both apply paths plus the variable-grid *display* getter consult the same priority order:

1. Parent-state instance override (e.g. screen state `"ButtonInstance.IsEnabled"`)
2. Component's own default state value
3. Behavior-declared `FormsProperty.Value` (the implicit default)

Tier 3 is **virtual** — never written to state, never appears in `.gusx`. State-empty stays state-empty. The fallback machinery:

- `RecursiveVariableFinder.Fallback` (`Func<string, object?>?`) — consulted when state lookup returns null.
- `EvaluatedSyntax.FromSyntaxNode` takes an optional `fallback` parameter threaded through every recursive evaluation.
- `BehaviorToolOnlyReferencesApplier` builds a per-element fallback that walks every linked behavior's `FormsProperties` and resolves bare or instance-qualified identifiers to declared `Value`.
- `BehaviorFormsPropertyApplier.Apply` adds the third tier inline: `ReadEffectiveValue(...) ?? declaration.Value`.
- `StateReferencingInstanceMember.DefaultValueFallback` lets the variable grid show the declared default in the checkbox/text without writing into state. `IsDefault` still returns true (state empty), preserving the grid's italic/grey styling.

### `VariableSave.Description` (v3)

Authored documentation persisted alongside the FormsProperty declaration in the `.behx`. `ElementSaveDisplayer.AddBehaviorFormsPropertyMembers` seeds each row's `srim.DetailText` from the declaration's `Description` when surfacing behavior-promoted variables, so the variable grid shows authored docs by default. Distinct from the `[XmlIgnore]` `DetailText` field, which holds transient state-dependent hints set by tool/plugin code at runtime; see the cross-referencing XML docs on both fields. Use `Description` only when a property's name is ambiguous, has overlapping siblings (e.g. `MaxLength` vs `MaxLettersToShow`), or assumes prior framework knowledge.

### Covered today

State-mapped (FormsProperty + ToolOnlyVariableReference, three-tier default resolution):
- `IsEnabled` — every standard Forms behavior. Behaviors with no Disabled visual still get the `FormsProperty` (runtime reflection works) but no reference (no visual state to drive).
- `IsChecked` — CheckBox, RadioButton, Toggle, with nested-ternary references combining IsEnabled and IsChecked into the appropriate `<X>CategoryState` slot.
- `VerticalScrollBarVisibility` — ScrollViewer, mapped to the existing `ScrollBarVisibility` category via `ScrollBarVisibilityState = VerticalScrollBarVisibility == "Hidden" ? "NoScrollBar" : "VerticalScrollVisible"`. The `==` comparison relies on the enum↔string bridge in `EvaluatedSyntax.AreEqual` — see the `gum-tool-save-classes` skill for the int-on-disk vs boxed-enum-in-memory roundtrip. Project-side state authoring decides what each state actually changes visually.

Logical-only (FormsProperty only — runtime reflects directly):
- `ToolTip` — every standard Forms behavior.
- TextBox/PasswordBox: `AcceptsReturn`, `AcceptsTab`, `IsReadOnly`, `Placeholder`, `MaxLength` (`int?`), `TextWrapping`.
- TextBox-only: `MaxLettersToShow` (`int?`), `MaxNumberOfLines` (`int?`).
- Slider/ScrollBar (RangeBase): `Minimum`, `Maximum`, `Value`, `SmallChange`, `LargeChange`.
- Slider-only: `TicksFrequency`, `IsSnapToTickEnabled`.
- ScrollViewer: `SmallChange`, `LargeChange` (fanned to inner scroll bars by ScrollViewer's setters), `HorizontalScrollBarVisibility`.
- ItemsControl/ListBox: `Orientation` (`Orientation?`).

**Nullable type declarations.** When a Forms control's property is `int?` (or `Orientation?`) and `null` is meaningful, declare the `?` in `FormsProperty.Type` (e.g. `Type="int?"`, `Type="Orientation?"`) so the variable grid renders the nullable editor and authors can clear back to null. The runtime applier reflects on `prop.PropertyType` regardless, so coercion works either way.

**Enum-typed FormsProperty declarations.** Author the default `Value` as a string (`<Value xsi:type="xsd:string">Auto</Value>`) — the applier's `IsEnum` branch parses it via `Enum.Parse`. The variable grid resolves the enum's name string (e.g. `Type="ScrollBarVisibility"`) via `TypeManager.GetTypeFromString`, so the type must live in or be linked into a TypeManager-scanned assembly (Gum.exe, GumCommon, or GumDataTypes). `TextWrapping`, `ScrollBarVisibility`, and `Orientation` are physically in `MonoGameGum/Forms/` but `<Compile Include>`-linked into GumCommon.

Reflection apply is generic, so any new logical-only `FormsProperty` declaration flows through without runtime changes. State-mapped properties beyond what's listed follow the same shape: declare the `FormsProperty`(/ies) plus a `ToolOnlyVariableReference` driving the appropriate `<X>CategoryState`.

## Key Files

| Path | Purpose |
|------|---------|
| `GumDataTypes/Behaviors/BehaviorSave.cs` | Behavior definition model (`.behx`) |
| `GumDataTypes/Behaviors/ElementBehaviorReference.cs` | Per-element reference holding only `BehaviorName` |
| `GumDataTypes/Behaviors/StandardFormsBehaviorNames.cs` | String constants for all standard behavior names |
| `GumDataTypes/ElementSave.cs` | `Behaviors` list on components/screens |
| `MonoGameGum/Forms/FormsUtilities.cs` | `RegisterFromFileFormRuntimeDefaults()` — drives the mapping; sets `InitialStateAppliedNotifier` to invoke `BehaviorFormsPropertyApplier` |
| `MonoGameGum/Forms/DefaultFromFileVisuals/` | `DefaultFromFileXxxRuntime` classes — `AfterFullCreation()` creates Forms objects |
| `MonoGameGum/Forms/BehaviorFormsPropertyApplier.cs` | Reflects `BehaviorSave.FormsProperties` values onto the wrapped `FrameworkElement` (with three-tier default resolution) |
| `Gum/Plugins/InternalPlugins/VariableGrid/BehaviorToolOnlyReferencesApplier.cs` | Tool-only design-time apply pass for `ToolOnlyVariableReferences`. Strictly tool-only — never invoked from runtime / generated code. |
| `Gum/Plugins/InternalPlugins/VariableGrid/ElementSaveDisplayer.cs` | `AddBehaviorFormsPropertyMembers` — surfaces FormsProperties in the variable grid; sets `DefaultValueFallback` on each SRIM so the grid shows the declared default; seeds `srim.DetailText` from the FormsProperty's `Description`. |
| `GumDataTypes/Variables/VariableSave.cs` | `Description` (persisted, XML-serialized) and `DetailText` (`[XmlIgnore]`, transient) fields with cross-referencing XML docs. |
| `Gum/Plugins/InternalPlugins/VariableGrid/StateReferencingInstanceMember.cs` | `DefaultValueFallback` final tier in the value getter for grid display. |
| `Runtimes/GumExpressions/EvaluatedSyntax.cs` | Optional `fallback` parameter on `FromSyntaxNode` threaded through evaluation. |
| `Gum/DataTypes/RecursiveVariableFinder.cs` | `Fallback` property consulted when state lookup returns null. |
| `GumRuntime/InteractiveGue.cs` | `FormsControlAsObject` back-link property |
