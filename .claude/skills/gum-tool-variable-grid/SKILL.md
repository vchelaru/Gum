---
name: gum-tool-variable-grid
description: Gum Variables tab and DataUiGrid. Triggers: Variables tab, DataUiGrid control, MemberCategory, InstanceMember, category population, property grid refresh, expansion state persistence.
---

# Gum Variables Tab & DataUiGrid Reference

## Overview

The **Variables tab** displays and edits properties of the selected element, instance, state, or behavior. Built on `DataUiGrid` (a WPF `ItemsControl` subclass) from the `WpfDataUi` library. Categories render as collapsible `Expander` sections.

> Icons rendered inside the Variables grid (unit selectors, alignment, dock/anchor, origin/sizing toggle-button option displays) come from the `GumIcon`/`PathGeometry` pipeline. For authoring or replacing them see [gum-icons](../gum-icons/SKILL.md).

---

## Architecture Layers

```
[User selects object]
        ↓
[MainVariableGridPlugin] (event subscription)
        ↓
[PropertyGridManager.RefreshDataGrid()]
        ↓
[ElementSaveDisplayer.GetCategories()]
        ↓ (produces List<MemberCategory>)
[DataUiGrid.SetCategories()]
        ↓
[WPF Expander per MemberCategory, rows per InstanceMember]
```

---

## Key Files

| Purpose | File Path |
|---------|-----------|
| DataUiGrid control | `WpfDataUi/DataUiGrid.cs` |
| DataUiGrid XAML template | `WpfDataUi/Themes/Generic.xaml` |
| MemberCategory / InstanceMember models | `WpfDataUi/DataTypes/` |
| Gum-specific member subclass | `Gum/Plugins/InternalPlugins/VariableGrid/StateReferencingInstanceMember.cs` |
| Plugin wiring selection events | `Gum/Plugins/InternalPlugins/VariableGrid/MainVariableGridPlugin.cs` |
| Category population manager | `Gum/Plugins/InternalPlugins/VariableGrid/PropertyGridManager.cs` |
| Category factory | `Gum/Plugins/InternalPlugins/VariableGrid/ElementSaveDisplayer.cs` |
| Behavior categories | `Gum/Plugins/InternalPlugins/VariableGrid/BehaviorShowingLogic.cs` |
| Host UserControl | `Gum/Plugins/InternalPlugins/VariableGrid/MainPropertyGrid.xaml(.cs)` |
| Composite member model | `WpfDataUi/DataTypes/CompositeInstanceMember.cs` |
| Composite descriptor registry | `Gum/Plugins/InternalPlugins/VariableGrid/CompositeMemberRegistry.cs`, `CompositeMemberDescriptor.cs`, `CompositeMemberLogic.cs` |

---

## Non-Obvious Behaviors

### SetCategories Expansion Preservation

`DataUiGrid.SetCategories()` captures `{name → IsExpanded}` from existing categories, replaces the list, then re-applies the saved values by name. Category collapse state persists across selection changes within a session. `IsExpanded` is `Mode=TwoWay` in the XAML template so user gestures write back to the model immediately.

### Structural Rebuild vs. Partial Refresh

`PropertyGridManager.RefreshDataGrid` tracks the previous display target (element, state, instances, behavior). If unchanged and `force=false`, it calls `Refresh()` to update values without recreating categories. If the target changed, it calls `SetCategories` with a fresh list from `ElementSaveDisplayer`. Pass `force: true` to always rebuild.

### Control Recycling (SingleDataUiContainer)

`SingleDataUiContainer` maintains a static `Dictionary<Type, Stack<UserControl>>` pool. When a container is removed from the visual tree (`Unloaded`), its inner displayer control is detached and pushed onto the type-keyed stack. When a new container needs a displayer, `CreateInternalControl` first checks if the existing control already matches the needed type (reuse in-place — preserves focus), then tries the pool via `TryGetFromPool`, and only falls back to `Activator.CreateInstance` if both miss. Pooled controls must clean up stale state when reassigned to a new `InstanceMember` (e.g., `TextBoxDisplay` detaches old event handlers, resets error/multiline state, and calls `Refresh`). `SetCategories` uses `BulkObservableCollection.ReplaceAll` (single `Reset` notification) which triggers WPF to unload old containers (returning controls to the pool) and create new ones (pulling from the pool).

### Multi-Select Path

When multiple instances are selected, `SetMultipleCategoryLists` is used instead of `SetCategories`. `MultiSelectInstanceMember` wrappers coordinate synchronized edits across all selected instances and record a single undo after all values are set.

### StateReferencingInstanceMember

All members in the Variables tab use `StateReferencingInstanceMember` (subclass of `InstanceMember`), not the generic reflection path. Its `IsReadOnly` returns `true` when `InstanceSave?.Locked == true`. Its `IsDefault` returns `true` when the value is absent from the selected state (not inherited from defaults).

---

## Custom Displayers (how a variable gets non-default UI)

Most variables render with a default control inferred from their type. A variable gets a *different* control — slider, angle dial, alignment/origin toggles, parent dropdown — through three knobs on `InstanceMember`:

- **`PreferredDisplayer`** (a `Type`) — selects which WPF control renders the row; `SingleDataUiContainer` instantiates it. This is what creates e.g. a `SliderDisplay`.
- **`PropertiesToSetOnDisplayer`** (a `Dictionary<string, object>`) — after the control exists, the container *reflectively* sets each named property on it. A slider's `MinValue`/`MaxValue` (the "range") are just two such pushes onto a control already chosen by `PreferredDisplayer` — they do **not** create the slider on their own.
- **`UiCreated` event** — for config that must be computed per-instance instead of a constant (see `MakeDegreesAngle`, and the WpfDataUi sample's per-character `MaxValue`).

Gum's built-in variables are wired in `StandardElementsManager.GumTool.cs` (slider for color channels, angle dial, alignment, origin, parent dropdown). That file is where to look or add.

> Naming trap: `MinWidth`/`MaxWidth`/`MinHeight`/`MaxHeight` in `StandardElementsManager.cs` are real runtime layout clamps — unrelated to a displayer's slider `MinValue`/`MaxValue`.

The icon-based displayers (origin/alignment/dock toggles) get their glyphs from the `GumIcon` pipeline — see [gum-icons](../gum-icons/SKILL.md).

**Landmine: a new custom `IDataUi` displayer must wire its own right-click menu and default-value highlighting — nothing does either for you.** `SingleDataUiContainer`/`DataUiGrid` never touch `ContextMenu` or background color. Every existing displayer (`ColorDisplay`, `SliderDisplay`, `TextBoxDisplay`, ...) attaches a WPF `ContextMenu` to its root element in XAML and calls `this.RefreshContextMenu(yourRoot.ContextMenu)` from `Refresh()` — skip it and `InstanceMember.ContextMenuEvents` (Make Default, Copy Qualified Variable Name, expose/un-expose) silently never reach an actual menu; right-click does nothing. Default-value background tinting (the green `TextBoxDisplayLogic.DefaultValueBackground` / gray `IndeterminateValueBackground`, driven by `InstanceMember.IsDefault`/`.IsIndeterminate`) is likewise opt-in per displayer — copy the pattern from `TextBoxDisplayLogic.RefreshBackgroundColor`, don't assume a new control gets it for free. Both gaps compile fine and only show up as "this row behaves differently from every other row" in manual testing.

Same opt-in-per-displayer trap for click-drag-over-label-to-scrub a numeric value: it's implemented only in `WpfDataUi/Controls/TextBoxDisplay.xaml.cs` (`Label_MouseMove`, `EnableLabelDragValueChange`, `LabelDragValueRounding`/`LabelDragChangeMultiplier`), not on `DataUiGrid`/`SingleDataUiContainer`. A custom or composite displayer (e.g. `CornerRadiusDisplay`) gets no drag-scrub on any of its fields unless it copies that logic itself.

---

## Composite Members (several variables, one row)

A separate mechanism from `PreferredDisplayer` above: `CompositeMemberLogic.Apply` runs after categories are built and collapses a fixed set of sibling "channel" `InstanceMember`s into a single `CompositeInstanceMember` row, driven by an `IDataUi` control bound to one composed value (not a raw variable). Today's only registered descriptor is color (`Red`/`Green`/`Blue` → one swatch row via `Gum/Controls/DataUi/ColorDisplay.xaml.cs`, registered in `CompositeMemberRegistry`); use that descriptor + displayer as the template for a new one.

Landmine: `CompositeMemberLogic.GroupTriples` matches channels by finding the **first channel's root name as a literal substring** inside a candidate member's root name, splitting off the prefix/suffix, then requiring every other `ChannelRootNames` entry to exist verbatim at that same prefix/suffix. **All channels must be present as real, non-excluded `VariableSave`s in the same category** (mind `MinimumGumxVersion` gating in `ShapeVariableVersionGate.cs`) — if even one is missing, no composite forms and the rest render as ordinary individual rows, silently, with no error.

Landmine: `CompositeInstanceMember.HandleCustomSet` writes **every** channel on every commit, but skips a channel whose decomposed value already equals its current value — this guard matters. It's harmless to omit for Color (an edit there is always "set all of R/G/B explicitly"), but a composite whose channels carry inherit-vs-explicit semantics via nullability (e.g. corner radius's per-corner overrides, where `null` means "inherit the uniform value") would otherwise force-write `null` onto every hidden/untouched channel on every commit, silently flipping it from inherited to explicitly-set and corrupting `IsDefault` bookkeeping for a value nobody touched. "Linked vs. unlinked" for such a composite isn't a stored flag either — it's derived purely from whether the override channels currently resolve to null, recomputed from `ChannelMembers` on every `Compose`.

---

## Refresh Trigger Flow

```
Selection changed
  → MainVariableGridPlugin.Handle*Selected()
  → PropertyGridManager.RefreshEntireGrid(force: true)
  → RefreshDataGrid(...)
     ├─ Target changed?
     │   yes → ElementSaveDisplayer.GetCategories()
     │          → DataUiGrid.SetCategories()     ← preserves IsExpanded by name
     └─ Target same?
             → DataUiGrid.Refresh()              ← only updates member values
```

### Double-Refresh Guard (Instance Selection)

When an instance is selected, two events fire in sequence: the default state is force-selected first (via `PerformAfterSelectInstanceLogic`), then the instance-selected event fires. Without a guard, the grid rebuilds twice.

```
Selection changed (instance)
  → HandleStateSelected()       (state force-selected first)
  → RefreshEntireGrid(force: true) + sets _stateJustRefreshedGrid
  → HandleInstanceSelected()    (fires second)
  → _stateJustRefreshedGrid is true → skip redundant refresh
```

`_stateJustRefreshedGrid` is cleared by `HandleElementSelected` and `HandleTreeNodeSelected` so it does not suppress legitimate refreshes during unrelated selections.

Variable set by UI:
```
InstanceMember.AfterSetByUi
  → StateReferencingInstanceMember.NotifyVariableLogic()
  → PropertyGridManager.RefreshEntireGrid(force: false)
  → DataUiGrid.Refresh()   (no structural rebuild needed)
```

### Post-set Selective Refresh (`SetVariableLogic`)

After a variable is set, `SetVariableLogic.RefreshInResponseToVariableChange` decides whether the grid needs a structural rebuild, a value-only refresh, or nothing. By default *nothing happens* — most variable edits only need the value-only refresh that already ran upstream. The decision rule is:

1. **Hardcoded name in `VariablesRequiringRefresh` dictionary** (e.g. `Parent`, `BaseType`, `Font`, `TextureAddress`) — these change which other variables exist on the grid, so they trigger a `FullGridRefresh` (rebuild + tree view) or `FullGridValueRefresh` per the dictionary entry.
2. **State variable on the element or an instance** (detected dynamically via `SetVariableLogic.IsStateVariable` → `VariableSave.IsState`) — assigning a categorized state doesn't add/remove variables, but it does change which rows are reference-driven, their `IsDefault` status, and their subtext. Those are computed at category-build time, so a value-only refresh isn't enough → `RefreshVariables(force: true)`.

State variables can't be in the hardcoded dictionary because their names are dynamic (`<CategoryName>State`). Use `IsStateVariable(unqualifiedMember, parentElement, instance)` rather than string-matching.

The trailing-else branch of the dictionary-handling block (`RefreshVariables(force: true)`) is the "rebuild grid, don't touch the tree" path. It's not named in the `VariableRefreshType` enum — it's the fallthrough behavior. Watch for it when reading the code.

---

## Common Patterns

### Making a category collapsed by default

Set `IsExpanded = false` on the `MemberCategory` before passing to `SetCategories`. The first time the category appears it uses the incoming value; subsequent appearances restore the user's last state.

### Forcing a full grid rebuild

Call `PropertyGridManager.RefreshEntireGrid(force: true)`. The `force` flag bypasses the same-target optimization and always recreates categories.

### Writing a variable's `DetailText` (the grid help blurb)

A `VariableSave.DetailText` renders as the help text under that variable in the Variables tab. It is **end-user authoring help shown inside the running tool**, so write it from the tool user's perspective and keep it to what they're choosing right now. Two recurring mistakes when adding a standard variable in `StandardElementsManager`:

- **Don't restate a visibility condition the exclusion logic already enforces.** If the variable is hidden unless some other variable is set (via `ExclusionsPlugin`), the user only ever sees it when that condition holds — "only used when X is checked" is redundant noise.
- **Don't leak runtime/integration/implementation details the tool user neither sees nor controls** — e.g. "requires a consumer-registered resolver," codegen specifics, or platform backends. Those belong in code-side XML docs or the runtime skill, not in tool UI help. (`SourceShaderFile` originally carried both mistakes because the text was lifted from the runtime/issue framing.)
