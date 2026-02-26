---
name: gum-tool-errors
description: Reference guide for Gum's error detection and display system. Load this when working on the Errors tab, error icons ("!" mark) in the tree view, ErrorChecker, ErrorViewModel, IErrorChecker, AllErrorsViewModel, MainErrorsPlugin, RequestErrorRefreshMessage, or adding new error checks.
---

# Gum Tool Error System Reference

## Architecture

Two tiers of error detection, merged into one display.

**Tier 1 — Core checks** (`ErrorChecker`): Runs on a given `ElementSave`. Called by both the tree view (icon refresh) and the Errors tab (list refresh).

**Tier 2 — Plugin checks**: Plugins implement `GetAllErrors` event (declared on `PluginBase`) and return `IEnumerable<ErrorViewModel>`. Called via `PluginManager.FillWithErrors()`, which is invoked at the end of `ErrorChecker.GetErrorsFor()`.

## Error Pipeline

```
User action (e.g. InstanceAdd, VariableSet, Undo)
    ↓
MainTreeViewPlugin → RefreshErrorIndicatorsForElement(element)
    ↓
ErrorChecker.GetErrorsFor(element, project)
    ↓
ElementTreeViewManager.UpdateErrorIndicatorsForElement()
    └─ Swaps icon to ExclamationIndex (6) if errors exist

SEPARATELY — Errors tab:
MainErrorsPlugin → UpdateErrorsForElement() or HandleErrorRefreshRequest()
    ↓
ErrorChecker.GetErrorsFor(element, project)
    ↓
AllErrorsViewModel.Errors (ObservableCollection) → ErrorDisplay.xaml ListBox
```

The tree icon refresh and the Errors tab refresh are independent. Both call `ErrorChecker.GetErrorsFor` but are triggered separately.

## Adding New Error Checks

**Core check** (missing references, structural problems): Add a private method to `ErrorChecker` and call it from `GetErrorsFor`. Pattern: iterate states/instances, add `new ErrorViewModel { Message = "..." }`.

**Plugin check** (feature-specific): Subscribe to `GetAllErrors` in your plugin's `StartUp()`, return `IEnumerable<ErrorViewModel>`, and set `item.OwnerPlugin = this` on each.

**Triggering refresh**: Send `RequestErrorRefreshMessage` via messenger to refresh the Errors tab list. Tree icon refresh is driven by existing plugin event subscriptions in `MainTreeViewPlugin`.

## Current Core Checks (ErrorChecker)

| Method | What it detects |
|--------|----------------|
| `GetBehaviorErrorsFor` | Missing behavior references; missing/wrong-type required instances and variables |
| `GetMissingElementBaseTypeErrorFor` | Element's own base type points to a deleted/nonexistent element |
| `GetMissingBaseTypeErrorsFor` | Instance's base type points to a nonexistent element |
| `GetParentErrorsFor` | Parent variable references a nonexistent instance |
| `GetInvalidVariableTypeErrorsFor` | Custom variable uses an unknown or misnamed type (State suffix issues) |

## Key Files

| File | Purpose |
|------|---------|
| `Gum/Managers/ErrorChecker.cs` | All core error checks |
| `Gum/Managers/ErrorViewModel.cs` | Data model (`Message`, `OwnerPlugin`) |
| `Gum/Managers/IErrorChecker.cs` | Interface |
| `Gum/Plugins/InternalPlugins/Errors/MainErrorsPlugin.cs` | Errors tab plugin; handles `RequestErrorRefreshMessage` |
| `Gum/Plugins/InternalPlugins/Errors/AllErrorsViewModel.cs` | ObservableCollection of errors; `CountDescription` for tab header |
| `Gum/Plugins/InternalPlugins/TreeView/ElementTreeViewManager.cs` | `UpdateErrorIndicatorsForElement`; `ExclamationIndex = 6` |
| `Gum/Messages/RequestErrorRefreshMessage.cs` | Message to force Errors tab refresh |
| `Tool/Tests/GumToolUnitTests/Managers/ErrorCheckerTests.cs` | Unit tests for ErrorChecker |

## Non-Obvious Behaviors

**Two separate refreshes**: The "!" icon in the tree and the Errors tab list are populated independently. Changing `ErrorChecker` automatically affects both, but only if the right events trigger both refresh paths.

**Cache wrapping**: `ErrorChecker.GetErrorsFor` wraps its checks in `ObjectFinder.Self.EnableCache()` / `DisableCache()`. New checks added inside the method benefit from this automatically.

**`IsSourceFileMissing` is separate**: The tree view shows "!" if `element.IsSourceFileMissing || hasErrors`. Source file missing is not surfaced as an `ErrorViewModel` — it's a flag on the element itself, checked directly by `UpdateErrorIndicatorsForElement`.
