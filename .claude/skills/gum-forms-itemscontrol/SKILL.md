---
name: gum-forms-itemscontrol
description: Reference guide for ItemsControl and ListBox — the Items/ListBoxItems relationship, templates, InnerPanel sync, and gotchas. Load this when working on ItemsControl, ListBox, ListBoxItem, VisualTemplate, FrameworkElementTemplate, Items collection behavior, ListBoxItems desync, or adding/removing items from a list box.
---

# ItemsControl and ListBox Reference

## Key Files

| File | Purpose |
|------|---------|
| `MonoGameGum/Forms/Controls/ItemsControl.cs` | Base class: Items property, template resolution, InnerPanel sync |
| `MonoGameGum/Forms/Controls/ListBox.cs` | Adds ListBoxItems tracking, selection, and ListBoxItem creation |
| `MonoGameGum/Forms/Controls/ListBoxItem.cs` | Individual row control; holds IsSelected, IsHighlighted, events |
| `MonoGameGum/Forms/VisualTemplate.cs` | Creates `GraphicalUiElement` instances (visual-first) |
| `MonoGameGum/Forms/FrameworkElementTemplate.cs` | Creates `FrameworkElement` instances (forms-first) |

## The Two Collections

`Items` and `ListBoxItems` are separate and can get out of sync.

- **`Items`** (`IList`, default `ObservableCollection<object>`) — the logical data collection. Can hold anything: strings, view models, `ListBoxItem` instances, or any `FrameworkElement` / `GraphicalUiElement`.
- **`ListBoxItems`** (`ReadOnlyCollection<ListBoxItem>`) — the visual row controls actually shown. Wraps `ListBoxItemsInternal` (a `List<ListBoxItem>`).

In normal usage (adding data objects to `Items`) they stay in sync. They diverge in several cases — see **Desync Gotchas** below.

## Data Flow: Items → InnerPanel → ListBoxItems

Adding to `Items` triggers a two-stage pipeline:

1. **`HandleItemsCollectionChanged`** — responds to `Items`. Creates or locates a visual and inserts it into `InnerPanel.Children`.
2. **`HandleInnerPanelCollectionChanged`** — responds to `InnerPanel.Children`. Calls `HandleCollectionNewItemCreated(frameworkElement, index)`.
3. **`HandleCollectionNewItemCreated`** (ListBox override) — if the item is a `ListBoxItem`, inserts it into `ListBoxItemsInternal` and calls `AssignListBoxEvents`.

`HandleCollectionNewItemCreated` is NOT called directly from step 1. It is only triggered by InnerPanel firing its own `CollectionChanged`. This indirection is intentional.

## What Gets Created Per Item Type

`HandleItemsCollectionChanged` dispatches based on what was added to `Items`:

| Item type added to `Items` | What happens |
|---------------------------|-------------|
| `FrameworkElement` | Its `.Visual` is inserted into InnerPanel directly — no new wrapper created |
| `GraphicalUiElement` | Inserted into InnerPanel directly — no wrapper |
| Any other data object AND `VisualTemplate` is set | `VisualTemplate.CreateContent(item, createFormsInternally:false)` is called; result inserted |
| Any other data object, no `VisualTemplate` | `CreateNewItemFrameworkElement(item)` is called |

**ListBox overrides `CreateNewItemFrameworkElement`** with additional logic:

| Item type | ListBox behavior |
|-----------|----------------|
| `ListBoxItem` | Used as-is — no template, no wrapping |
| Anything else | Calls `CreateNewVisual(vm)` (uses `VisualTemplate` or `DefaultFormsTemplates[typeof(ListBoxItem)]`), then wraps result in a `ListBoxItem` via `CreateNewListBoxItem`. `BindingContext` and `UpdateToObject` are called on the result. |

## Templates

There are two template types with different roles:

**`VisualTemplate`** — produces a `GraphicalUiElement` (visual-first).
- Constructed with a `Type` (must have `(bool, bool)` or no-arg constructor), `Func<GraphicalUiElement>`, `Func<object, GraphicalUiElement>`, or `Func<object, bool, GraphicalUiElement>`.
- Used by `CreateNewVisual`. When constructed from a `Type`, calls it with `(true, false)` — `createFormsInternally:false` prevents the visual from creating its own Forms object, since the ListBox will wrap it.
- Set on `ItemsControl.VisualTemplate`. Changing it clears and rebuilds all visuals.

**`FrameworkElementTemplate`** — produces a `FrameworkElement` (forms-first).
- Constructed with a `Type` or `Func<FrameworkElement>`.
- Used by `CreateNewItemFrameworkElement`. For ListBox, the result must be a `ListBoxItem` subclass or an exception is thrown.
- Set on `ItemsControl.FrameworkElementTemplate`.

**Global fallback** — if neither template is set, `DefaultFormsTemplates[typeof(ListBoxItem)]` is used (set during app initialization). This is the normal path for default apps.

**Setting a template clears and rebuilds all existing items** — both `VisualTemplate` and `FrameworkElementTemplate` setters call `ClearVisualsInternal()` and replay the Items collection.

## Desync Gotchas

### 1. Adding a non-ListBoxItem FrameworkElement to Items

When a `Button`, `CheckBox`, or other `FrameworkElement` is added to `Items`, its Visual is inserted into InnerPanel. `HandleInnerPanelCollectionChanged` fires, but `asGue.FormsControlAsObject` is the `Button` (not a `ListBoxItem`), so `HandleCollectionNewItemCreated` is called with a `Button`. ListBox's override only inserts into `ListBoxItemsInternal` if the item `is ListBoxItem`, so the `Button` is silently skipped. **`Items.Count` increases but `ListBoxItems.Count` does not.**

### 2. Adding directly to InnerPanel.Children

`HandleInnerPanelCollectionChanged` fires and can populate `ListBoxItemsInternal` if the child's `FormsControlAsObject` is a `ListBoxItem`. But **`Items` is never updated** — it stays at 0 (or whatever it was). This is the case when a ListBox's visual is constructed by the Gum tool with pre-filled children.

### 3. Gum tool pre-filled ListBox (ReactToVisualChanged recovery)

If a ListBox Visual arrives with children already in InnerPanel and `Items.Count == 0`, `ReactToVisualChanged` in ListBox iterates `InnerPanel.Children`, adds `ListBoxItem` instances to both `Items` and `ListBoxItemsInternal`, and calls `AssignListBoxEvents`. This recovery only runs once at construction; it does not stay in sync afterward.

### 4. Index alignment assumption

`HandleItemSelected` resolves the data object via `Items[ListBoxItemsInternal.IndexOf(listBoxItem)]`. If `Items` and `ListBoxItems` have drifted (any of the above cases), **selection silently fails or selects the wrong item** — the check `clickedIndex >= Items.Count` causes an early return.

### 5. AssignListBoxEvents idempotency

`ListBoxItem.AssignListBoxEvents` is guarded by `hasHadListBoxEventsAssigned`. A `ListBoxItem` that bypasses normal item creation (e.g., added to InnerPanel directly without going through `Items`) may or may not have its events assigned. If events are missing, the item renders but clicking it produces no selection change.

## Selection

`SelectedItems` is an `ObservableCollection<object>` (not replaceable, only modified). `SelectedObject` and `SelectedIndex` are convenience properties that read/write the first entry in `SelectedItems`.

`SyncIsSelectedFromSelectedItems` walks `ListBoxItemsInternal` and reconciles `IsSelected` on each item. It runs whenever `SelectedItems` changes or `SelectedObject`/`SelectedIndex` are set.

`SelectionMode` controls click behavior: `Single` (default), `Multiple` (each click toggles), `Extended` (Ctrl/Shift modifier keys). Gamepad/keyboard input always uses single-selection behavior regardless of mode.

## DisplayMemberPath

When set, `DisplayMemberPath` causes `listBoxItem.UpdateToObject(property_value_as_string)` instead of `UpdateToObject(the_object_itself)`. This applies both on initial creation and when `DisplayMemberPath` is changed after items are already loaded.
