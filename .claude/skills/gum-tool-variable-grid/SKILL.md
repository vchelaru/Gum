---
name: gum-tool-variable-grid
description: Reference guide for Gum's Variables tab and DataUiGrid system. Load this when working on the Variables tab, DataUiGrid control, MemberCategory, InstanceMember, category population, property grid refresh, or category expansion state persistence.
---

# Gum Variables Tab & DataUiGrid Reference

## Overview

The **Variables tab** displays and edits properties of the selected element, instance, state, or behavior. Built on `DataUiGrid` (a WPF `ItemsControl` subclass) from the `WpfDataUi` library. Categories render as collapsible `Expander` sections.

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

---

## Non-Obvious Behaviors

### SetCategories Expansion Preservation

`DataUiGrid.SetCategories()` captures `{name → IsExpanded}` from existing categories, replaces the list, then re-applies the saved values by name. Category collapse state persists across selection changes within a session. `IsExpanded` is `Mode=TwoWay` in the XAML template so user gestures write back to the model immediately.

### Structural Rebuild vs. Partial Refresh

`PropertyGridManager.RefreshDataGrid` tracks the previous display target (element, state, instances, behavior). If unchanged and `force=false`, it calls `Refresh()` to update values without recreating categories. If the target changed, it calls `SetCategories` with a fresh list from `ElementSaveDisplayer`. Pass `force: true` to always rebuild.

### Multi-Select Path

When multiple instances are selected, `SetMultipleCategoryLists` is used instead of `SetCategories`. `MultiSelectInstanceMember` wrappers coordinate synchronized edits across all selected instances and record a single undo after all values are set.

### StateReferencingInstanceMember

All members in the Variables tab use `StateReferencingInstanceMember` (subclass of `InstanceMember`), not the generic reflection path. Its `IsReadOnly` returns `true` when `InstanceSave?.Locked == true`. Its `IsDefault` returns `true` when the value is absent from the selected state (not inherited from defaults).

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

Variable set by UI:
```
InstanceMember.AfterSetByUi
  → StateReferencingInstanceMember.NotifyVariableLogic()
  → PropertyGridManager.RefreshEntireGrid(force: false)
  → DataUiGrid.Refresh()   (no structural rebuild needed)
```

---

## Common Patterns

### Making a category collapsed by default

Set `IsExpanded = false` on the `MemberCategory` before passing to `SetCategories`. The first time the category appears it uses the incoming value; subsequent appearances restore the user's last state.

### Forcing a full grid rebuild

Call `PropertyGridManager.RefreshEntireGrid(force: true)`. The `force` flag bypasses the same-target optimization and always recreates categories.
