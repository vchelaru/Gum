---
name: gum-tool-dialogs
description: Gum dialog/popup systems. Triggers: DialogService, DialogWindow, DeleteOptionsWindow, dialog scrolling/layout, adding new dialog types, ShowMessage/ShowYesNoMessage, mocking IDialogService in unit tests.
---

# Gum Dialog Systems Reference

## Two Separate Systems

Gum has **two independent dialog systems**. Knowing which one is involved is critical before making changes.

### 1. DialogService System (MVVM, most dialogs)

Used by: message popups, yes/no confirmations, text input, choice selection, plugin management, import dialogs.

**Flow**: `DialogService` creates a `DialogWindow`, sets its `DataContext` to a view model. The `Dialog` control inside uses a `DialogTemplateSelector` to resolve the view model type to a `UserControl` view. After the view loads, `Dialog.OnContentChanged` binds attached properties from the view up to the `Dialog` control via deferred dispatch (`DispatcherPriority.Loaded`).

**Key attached properties on views** (set in XAML on the UserControl):
- `Dialog.DialogTitle` — window title
- `Dialog.Actions` — custom button area (replaces default OK/Cancel)
- `Dialog.AuxiliaryActions` — extra buttons on the left side (e.g. Browse)
- `Dialog.ScrollContent` — `true` (default) enables outer ScrollViewer; `false` disables it so the view can manage its own scrolling (used by ImportFromGumxView)

**View resolution**: `DialogViewResolver` maps view model types to views by naming convention (`FooViewModel` -> `FooView`) or by `[Dialog(typeof(VM))]` attribute. Scans assemblies lazily and caches results.

**Window sizing**: `DialogWindow` starts with `SizeToContent="WidthAndHeight"`. After content loads, `Dialog.OnContentChanged` switches to `SizeToContent.Manual` and clears the view's fixed Width/Height (sets to NaN), allowing the window to be resizable. `DialogService.CreateDialogWindow` sets `MaxHeight` to the owner window's `ActualHeight`.

### 2. DeleteOptionsWindow (standalone, code-behind)

Used by: delete confirmation only (`DeleteLogic.ShowDeleteDialog`).

**Why it exists separately**: The delete dialog needs runtime UI composition — plugins inject checkboxes and options into a `StackPanel` (e.g., "Delete associated files?", "Remove child instances?"). This cannot be done through the MVVM template system.

**Flow**: `DeleteLogic` (headless, `Tools/Gum.Presentation/Managers/DeleteLogic.cs`) calls the WPF-shell `IDeleteDialogService`, whose implementation `DeleteDialogService` creates the `DeleteOptionsWindow`, sets `Message`/`Title`, calls the concrete `PluginManager.ShowDeleteDialog()` (which lets plugins add controls to `MainStackPanel`), then calls `ShowDialog()`. `DeleteDialogService` depends on the concrete `PluginManager`, not `IPluginManager` — that interface dropped `ShowDeleteDialog`/`DeleteConfirmed` entirely when it moved into headless `Gum.Presentation` (#3754); those two WPF-typed calls live only on the concrete class now.

**Not managed by DialogService** — no view model, no template selection, no attached property binding. Changes to `DialogWindow.xaml` or `Dialog.cs` have **zero effect** on this window.

## Key Files

| File | System | Purpose |
|------|--------|---------|
| `Gum/Services/Dialogs/DialogService.cs` | MVVM | Creates and shows DialogWindow instances |
| `Gum/Services/Dialogs/DialogWindow.xaml` | MVVM | Window chrome, layout template with ScrollViewer + button footer |
| `Gum/Services/Dialogs/Dialog.cs` | MVVM | ContentControl with attached properties and template selector |
| `Gum/Services/Dialogs/DialogViewResolver.cs` | MVVM | Maps view model types to view types; falls back to scanning its own (tool) assembly for a relocated VM's `[Dialog]`-attributed View |
| `Gum/Services/Dialogs/DialogViewModel.cs` | MVVM | Base class with affirm/negative commands and RequestClose event |
| `Gum/Gui/Windows/DeleteOptionsWindow.xaml` | Standalone | Delete confirmation window layout |
| `Gum/Gui/Windows/DeleteOptionsWindow.xaml.cs` | Standalone | Code-behind with plugin-accessible StackPanel |
| `Gum/Services/Dialogs/DeleteDialogService.cs` | Standalone | Creates and shows DeleteOptionsWindow; calls the concrete `PluginManager` |
| `Tools/Gum.Presentation/Managers/DeleteLogic.cs` | Standalone | Orchestrates the delete flow via `IDeleteDialogService` |

## Common Pitfalls

**Wrong system**: The most common mistake is modifying `DialogWindow.xaml` or `Dialog.cs` expecting it to affect the delete dialog. Always verify which system shows the dialog you're fixing.

**File copy prompt**: The "copy or reference?" dialog shown when a SourceFile/Font path outside the project folder is assigned lives in `SetVariableLogic.AskIfShouldCopy` (`Gum/Plugins/InternalPlugins/VariableGrid/SetVariableLogic.cs`), triggered via `ReactIfChangedMemberIsSourceFile` — not in the drag-drop layer.

**ScrollViewer behavior**: The `Dialog` template wraps content in a ScrollViewer. With `Auto` scrolling, child controls get infinite available height during WPF measure — so internal scroll viewers (like a TreeView) won't scroll. Set `Dialog.ScrollContent="False"` on views that need bounded height for internal scrolling.

**Deferred binding**: `Dialog.OnContentChanged` binds attached properties at `DispatcherPriority.Loaded`, not immediately. Code that reads these values before the dispatch fires will see defaults.

## Testing Dialog Interactions (mocking IDialogService)

`IDialogService` exposes one message primitive: `ShowMessage(message, title?, MessageDialogStyle?)`, returning a `MessageDialogResult` enum (`Affirmative` / `Negative` / `Canceled`; `Negative == 0` is the unmocked Moq default → a `Mock<IDialogService>` returns "No" unless you set it up). The friendly helpers — `ShowYesNoMessage`, `ShowChoices`, etc. — are **extension methods** in `IDialogServiceExt`, so a Moq mock can only `Setup`/`Verify` the underlying `ShowMessage`. A Yes/No prompt arrives as `ShowMessage(msg, title, MessageDialogStyle.YesNo)`.

Gotcha: `MessageDialogStyle` is a **class**, and `.Ok` / `.YesNo` / `.OkCancel` each `new` up a fresh instance with no equality override — you cannot match a specific style by reference or equality. Distinguish a styled prompt (Yes/No, OK/Cancel) from a plain informational popup by **`style != null` vs `style == null`** — a bare `ShowMessage(msg)` passes `null` — or by inspecting `AffirmativeText`.
