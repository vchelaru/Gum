---
name: gum-tool-dialogs
description: Reference guide for Gum's dialog/popup systems. Load this when working on dialog windows, DialogService, DialogWindow, DeleteOptionsWindow, dialog scrolling, dialog layout, or adding new dialog types.
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

**Flow**: `DeleteLogic` creates a `DeleteOptionsWindow` directly, sets `Message` and `Title` properties, calls `PluginManager.ShowDeleteDialog()` (which lets plugins add controls to `MainStackPanel`), then calls `ShowDialog()`.

**Not managed by DialogService** — no view model, no template selection, no attached property binding. Changes to `DialogWindow.xaml` or `Dialog.cs` have **zero effect** on this window.

## Key Files

| File | System | Purpose |
|------|--------|---------|
| `Gum/Services/Dialogs/DialogService.cs` | MVVM | Creates and shows DialogWindow instances |
| `Gum/Services/Dialogs/DialogWindow.xaml` | MVVM | Window chrome, layout template with ScrollViewer + button footer |
| `Gum/Services/Dialogs/Dialog.cs` | MVVM | ContentControl with attached properties and template selector |
| `Gum/Services/Dialogs/DialogViewResolver.cs` | MVVM | Maps view model types to view types |
| `Gum/Services/Dialogs/DialogViewModel.cs` | MVVM | Base class with affirm/negative commands and RequestClose event |
| `Gum/Gui/Windows/DeleteOptionsWindow.xaml` | Standalone | Delete confirmation window layout |
| `Gum/Gui/Windows/DeleteOptionsWindow.xaml.cs` | Standalone | Code-behind with plugin-accessible StackPanel |
| `Gum/Managers/DeleteLogic.cs` | Standalone | Creates and shows DeleteOptionsWindow (line ~227) |

## Common Pitfalls

**Wrong system**: The most common mistake is modifying `DialogWindow.xaml` or `Dialog.cs` expecting it to affect the delete dialog. Always verify which system shows the dialog you're fixing.

**ScrollViewer behavior**: The `Dialog` template wraps content in a ScrollViewer. With `Auto` scrolling, child controls get infinite available height during WPF measure — so internal scroll viewers (like a TreeView) won't scroll. Set `Dialog.ScrollContent="False"` on views that need bounded height for internal scrolling.

**Deferred binding**: `Dialog.OnContentChanged` binds attached properties at `DispatcherPriority.Loaded`, not immediately. Code that reads these values before the dispatch fires will see defaults.
