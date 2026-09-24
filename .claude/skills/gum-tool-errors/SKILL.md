---
name: gum-tool-errors
description: Gum error detection/display. Triggers: Errors tab, '!' icons in tree view, ErrorChecker, ErrorViewModel, IErrorChecker, AllErrorsViewModel, MainErrorsPlugin, RequestErrorRefreshMessage, adding new error checks.
---

# Gum Tool Error System Reference

## Architecture

Two tiers of error detection, merged into one display.

**Tier 1 — Core checks** (`ErrorChecker`): Runs on a given `ElementSave`. Called by both the tree view (icon refresh) and the Errors tab (list refresh).

**Tier 2 — Plugin checks**: Plugins implement `GetAllErrors` event (declared on `PluginBase`) and return `IEnumerable<ErrorViewModel>`. Called via `PluginManager.FillWithErrors()`, which is invoked at the end of `ErrorChecker.GetErrorsFor()`.

## Error Pipeline

```
Plugin notification (VariableSet, InstanceAdd, StateAdd, Undo, ...)
    ↓
MainErrorsPlugin or MainTreeViewPlugin (never both for one notification)
    ↓
ErrorChecker.GetErrorsFor(element, project) → raises IErrorChecker.ErrorsChecked
    ├─ MainErrorsPlugin: fills AllErrorsViewModel.Errors (the Errors tab)
    └─ MainTreeViewPlugin.HandleErrorsChecked → ElementTreeViewManager.UpdateErrorIndicatorsForElement
```

The tree's "!" follows every full check through `ErrorsChecked`, including the Errors tab's.

## Adding New Error Checks

**Core check** (missing references, structural problems): Add it to `HeadlessErrorChecker.GetErrorsForInternal` in `Gum.ProjectServices`, **not** the tool's `Tools/Gum.Presentation/Managers/ErrorChecker.cs`. The tool's checker delegates to the headless one (and converts `ErrorResult` → `ErrorViewModel`); putting checks in the headless layer means both the tool's Errors tab (per-selected-element refresh) and `gumcli check` (whole-project pass via `GetAllErrors`) surface them automatically. Pattern: iterate states/instances, emit `new ErrorResult { ElementName = ..., Message = ..., Code = "GUM00XX", Severity = ... }`. Register the code in `ErrorDocsRegistry` to get a help URL.

**Plugin check** (feature-specific, tool-side only): Subscribe to `GetAllErrors` in your plugin's `StartUp()`, return `IEnumerable<ErrorViewModel>`, and set `item.OwnerPlugin = this` on each. Plugin checks only show in the tool — the CLI doesn't load plugins. If the check should fire in CI / pre-commit, use the headless path above instead.

**Fixable errors**: set `ActionName` and `ActionCommand` on the `ErrorViewModel` to render a button beside the row that resolves the error in place (`HasAction` drives its visibility). An action that destroys anything unrecoverable still owes the user a confirmation before it runs.

**Triggering refresh**: Send `RequestErrorRefreshMessage` via messenger; the Errors tab re-checks the selected element and the tree icon follows.

## Current Core Checks (ErrorChecker)

| Method | What it detects |
|--------|----------------|
| `GetBehaviorErrorsFor` | Missing behavior references; missing/wrong-type required instances and variables |
| `GetMissingElementBaseTypeErrorFor` | Element's own base type points to a deleted/nonexistent element |
| `GetMissingBaseTypeErrorsFor` | Instance's base type points to a nonexistent element |
| `GetParentErrorsFor` | Parent variable references a nonexistent instance |
| `GetInvalidVariableTypeErrorsFor` | Custom variable uses an unknown or misnamed type (State suffix issues) |
| `GetMissingSourceFileErrorsFor` | GUM0004: element file missing on disk; GUM0008 when it exists under a different case |
| `GetMissingExternalFileErrorsFor` | GUM0006: referenced texture/font file missing (via `GumProjectDependencyWalker`); GUM0008 for a case-only difference |

## Key Files

| File | Purpose |
|------|---------|
| `Tools/Gum.ProjectServices/HeadlessErrorChecker.cs` | All core error checks |
| `Tools/Gum.Presentation/Managers/ErrorChecker.cs` | Tool wrapper: headless checks + plugin checks; raises `ErrorsChecked` |
| `Tools/Gum.Presentation/Managers/ErrorViewModel.cs` | Data model (`Message`, `OwnerPlugin` — `object?`, headless `Gum.Presentation`, ADR-0005) |
| `Tools/Gum.Presentation/Plugins/InternalPlugins/Errors/MainErrorsPlugin.cs` | Errors tab plugin; handles `RequestErrorRefreshMessage` |
| `Tools/Gum.Presentation/Plugins/InternalPlugins/Errors/AllErrorsViewModel.cs` | ObservableCollection of errors; `CountDescription` for tab header (headless `Gum.Presentation`, ADR-0005) |
| `Tool/TreeViewPlugin.Core/MainTreeViewPlugin.cs` | Tree "!" indicator; checks only on notifications the Errors tab doesn't |
| `Tool/TreeViewPlugin.Core/ElementTreeViewManager.cs` | `UpdateErrorIndicatorsForElement` |
| `Tools/Gum.Presentation/Messages/RequestErrorRefreshMessage.cs` | Message to force an error refresh |
| `Tests/Gum.Presentation.Tests/Managers/ErrorCheckerTests.cs` | Unit tests for ErrorChecker |

## Element Reload and Errors

When an element file changes on disk, `FileChangeReactionLogic.ReactToElementSaveChanged` calls `_pluginManager.ElementReloaded(element)`. `MainErrorsPlugin` subscribes to `ElementReloaded` and calls `UpdateErrorsForElement` — this is the correct trigger for refreshing errors after a reload.

Do **not** rely on `ElementSelected` alone for error refresh after reload: the reload path temporarily sets `SelectedElement = null` (to force a UI reset), which clears errors, and the subsequent re-selection uses `file.StandardizedNoPathNoExtension` which fails to find elements in subfolders — so errors would never be repopulated.

## Non-Obvious Behaviors

**One check per notification**: a check walks the element's file references and reads the disk, so a notification triggers a check in only one plugin. A new refresh trigger for the selected element goes in `MainErrorsPlugin`; the tree picks up the result. The tree only checks where the Errors tab doesn't (states, categories, undo, behavior instances, all elements on project load).

**Case-check listings are cached**: `HeadlessErrorChecker` keeps one `FileNameCaseChecker` for its lifetime. It re-reads a directory only when that directory's last-write time changes, so a new GUM0008 check doesn't need its own `FileNameCaseChecker`.

**Cache wrapping**: `ErrorChecker.GetErrorsFor` wraps its checks in `ObjectFinder.Self.EnableCache()` / `DisableCache()`. New checks added inside the method benefit from this automatically.

**`IsSourceFileMissing` is separate**: The tree view shows "!" if `element.IsSourceFileMissing || hasErrors`. Source file missing is not surfaced as an `ErrorViewModel` — it's a flag on the element itself, checked directly by `UpdateErrorIndicatorsForElement`.
