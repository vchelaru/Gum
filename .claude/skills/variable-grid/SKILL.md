---
name: variable-grid
description: Reference guide for Gum's Variables tab and DataUiGrid system. Load this when working on the Variables tab, DataUiGrid control, MemberCategory, InstanceMember, category population, property grid refresh, or category expansion state persistence.
---

# Gum Variables Tab & DataUiGrid Reference

## Overview

The **Variables tab** in the Gum editor displays and edits the properties of the currently selected element, instance, state, or behavior. It is built on top of the `DataUiGrid` WPF control from the `WpfDataUi` library. Categories of properties are represented as collapsible `Expander` sections.

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
| MemberCategory model | `WpfDataUi/DataTypes/MemberCategory.cs` |
| InstanceMember model | `WpfDataUi/DataTypes/InstanceMember.cs` |
| Gum-specific member subclass | `Gum/Plugins/InternalPlugins/VariableGrid/StateReferencingInstanceMember.cs` |
| Plugin wiring selection events | `Gum/Plugins/InternalPlugins/VariableGrid/MainVariableGridPlugin.cs` |
| Category population manager | `Gum/Plugins/InternalPlugins/VariableGrid/PropertyGridManager.cs` |
| Category factory | `Gum/Plugins/InternalPlugins/VariableGrid/ElementSaveDisplayer.cs` |
| Behavior categories | `Gum/Plugins/InternalPlugins/VariableGrid/BehaviorShowingLogic.cs` |
| Host UserControl | `Gum/Plugins/InternalPlugins/VariableGrid/MainPropertyGrid.xaml(.cs)` |

---

## DataUiGrid Control (`WpfDataUi/DataUiGrid.cs`)

`DataUiGrid` is an `ItemsControl` subclass. Its items are `MemberCategory` objects.

### Key Properties

| Property | Type | Purpose |
|----------|------|---------|
| `Instance` | `object` (DependencyProperty) | Object being displayed; setting triggers `PopulateCategories()` |
| `Categories` | `BulkObservableCollection<MemberCategory>` | The displayed categories (bound to ItemsSource) |
| `TypesToIgnore` | collection | Types excluded from reflection-based population |
| `MembersToIgnore` | collection | Member names excluded |

### Key Methods

| Method | Description |
|--------|-------------|
| `SetCategories(IList<MemberCategory>)` | Batch-replaces all categories, fires a single Reset notification, and **preserves `IsExpanded` state** by matching category names |
| `SetMultipleCategoryLists(...)` | Multi-select editing path: merges multiple lists into `MultiSelectInstanceMember` wrappers |
| `PopulateCategories()` | Reflection-based auto-population when `Instance` is set directly |
| `RefreshDelegateBasedElementVisibility()` | Toggles member visibility based on conditional delegates |
| `Refresh()` | Re-reads values without rebuilding the full structure |
| `Apply(TypeMemberDisplayProperties)` | Applies display overrides (displayer type, read-only, etc.) to already-populated members |

### `SetCategories` Expansion Preservation

When `SetCategories` is called, it:
1. Captures `{name → IsExpanded}` from the old categories.
2. Replaces the category list.
3. Re-applies the captured `IsExpanded` values to matching new categories by name.

This ensures that collapsing a category persists across instance changes within a session.

### XAML Template (`WpfDataUi/Themes/Generic.xaml`)

```xml
<DataTemplate x:Key="DataUi.MemberCategoryTemplate" DataType="{x:Type dataTypes:MemberCategory}">
    <Expander Header="{Binding Name}" IsExpanded="{Binding IsExpanded, Mode=TwoWay}">
        <ItemsControl AlternationCount="2" ItemsSource="{Binding Members}" />
    </Expander>
</DataTemplate>
```

`IsExpanded` uses `Mode=TwoWay` so user gestures (clicking the expander header) write back to the model, and programmatic resets (via `SetCategories`) are reflected in the UI.

---

## MemberCategory (`WpfDataUi/DataTypes/MemberCategory.cs`)

Groups related `InstanceMember` objects under a named header.

### Key Properties

| Property | Type | Default | Notes |
|----------|------|---------|-------|
| `Name` | `string` | — | Category display name |
| `Members` | `ObservableCollection<InstanceMember>` | — | Members in this category |
| `IsExpanded` | `bool` | `true` | Raises `PropertyChanged`; bound to Expander |
| `HeaderColor` | `Brush?` | `null` | Optional header tint |
| `HideHeader` | `bool` | `false` | Hides the category header entirely |
| `Visibility` | computed | — | Collapsed when `Members.Count == 0` |

### Events

| Event | Fired When |
|-------|-----------|
| `PropertyChanged` | `IsExpanded`, `CategoryBorderThickness`, `Width`, `Visibility` change |
| `MemberValueChangedByUi` | Any member in this category is edited by the user |

---

## InstanceMember (`WpfDataUi/DataTypes/InstanceMember.cs`)

Represents one editable row in the grid.

### Key Properties

| Property | Purpose |
|----------|---------|
| `Name` | Property/field name (used for lookup) |
| `DisplayName` | Label shown in the UI |
| `Value` | Gets/sets value via reflection or custom delegates |
| `PropertyType` | The CLR type of the value |
| `PreferredDisplayer` | Which control type to use (TextBox, ComboBox, etc.) |
| `CustomOptions` | Options for ComboBox-style displayers |
| `IsReadOnly` | Whether the field is editable |
| `IsDefault` | Whether the value is the type default |
| `IsIndeterminate` | For multi-select when values differ |
| `Category` | Parent `MemberCategory` (set automatically on `Members.Add`) |

### Custom Get/Set Delegates

Instead of reflection, callers can assign:
- `CustomGetEvent` — `Func<object, object>` to read value
- `CustomSetEvent` — `Action<object, object>` to write value
- `CustomGetTypeEvent` — `Func<object, Type>` to get property type
- `CustomSetPropertyEvent` — for named property set with change-tracking

### Key Events

| Event | Purpose |
|-------|---------|
| `BeforeSetByUi` | Fired before user edits are committed |
| `AfterSetByUi` | Fired after user edits; triggers refresh/undo |
| `PropertyChanged` | Standard INotifyPropertyChanged |

---

## StateReferencingInstanceMember (`VariableGrid/StateReferencingInstanceMember.cs`)

Subclass of `InstanceMember` used for all members in the Variables tab (not the generic reflection path).

### Additional Properties

| Property | Purpose |
|----------|---------|
| `StateSave` | The state being edited |
| `StateSaveCategory` | Category within state |
| `InstanceSave` | The instance (null if editing the element itself) |
| `ElementSave` | The parent element |
| `RootVariableName` | Extracted from `Name`; handles nested paths like `"Child.Width"` |

### Overrides

- **`IsReadOnly`**: Returns `true` when `InstanceSave?.Locked == true` (and the variable is not `"Locked"` itself), preventing edits to locked instances.
- **`IsDefault`**: Returns `true` when the value is strictly absent from the selected state (not inherited from defaults).

---

## PropertyGridManager (`VariableGrid/PropertyGridManager.cs`)

Central coordinator between selection state and the DataUiGrid.

### Key Methods

| Method | Description |
|--------|-------------|
| `RefreshEntireGrid(force)` | Entry point from plugin events; delegates to `RefreshDataGrid` |
| `RefreshDataGrid(element, state, instances, behavior, category, force)` | Determines if a structural rebuild or partial refresh is needed, then calls `SetCategories` or `Refresh` |

### Structural Rebuild vs. Partial Refresh

`RefreshDataGrid` tracks the previous display target (element, state, instances, behavior). If unchanged and `force=false`, it only calls `mVariablesDataGrid.Refresh()` to update member values without recreating categories. If the target changed, it calls `SetCategories` with freshly created categories.

### Multi-Select Path

When multiple instances are selected, `SetMultipleCategoryLists` is used instead of `SetCategories`. `MultiSelectInstanceMember` wrappers coordinate synchronized edits across all instances and record a single undo after all values are set.

---

## ElementSaveDisplayer (`VariableGrid/ElementSaveDisplayer.cs`)

Factory that produces `List<MemberCategory>` for a given element/state/instance.

### Key Methods

| Method | Description |
|--------|-------------|
| `GetCategories(element, state, instance, ...)` | Main entry point; returns populated category list |
| `GetProperties(...)` | Assembles `PropertyData` list (handles variable inheritance, exposed variables, etc.) |
| `CreateSrimFromPropertyData(...)` | Creates a `StateReferencingInstanceMember` and wires up its delegates |

Categories are created anew on each call. The expansion state is preserved by `DataUiGrid.SetCategories`, not here.

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

### Adding a new variable to the Variables tab

Variables shown in the tab come from `ElementSave.DefaultState` variables. To add a new one:
1. Create a `VariableSave` and add it to the element's state in the data model.
2. `ElementSaveDisplayer.GetProperties()` will pick it up automatically on the next refresh.
3. To customize the displayer or category, handle the `GetDisplayer` plugin event.

### Making a category collapsed by default

Set `IsExpanded = false` on the `MemberCategory` object after creating it, before passing to `SetCategories`. Because `SetCategories` only restores expansion state for categories **already seen** by the user, the first time a category appears it will use whatever `IsExpanded` is set on the incoming category object.

### Forcing a full grid rebuild

Call `PropertyGridManager.RefreshEntireGrid(force: true)`. The `force` flag bypasses the same-target optimization and always recreates categories.
