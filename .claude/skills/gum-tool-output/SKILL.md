---
name: gum-tool-output
description: Gum Output tab. Triggers: IOutputManager, MainOutputViewModel, GuiCommands.PrintOutput, adding output/error messages.
---

# Gum Tool Output System Reference

## Architecture

**`IOutputManager`** (`Tools/Gum.Presentation/Managers/MainOutputViewModel.cs`):
- `AddOutput(string)` — appends a timestamped line
- `AddError(string)` — appends a timestamped line prefixed with `"ERROR:  "` (two spaces), then raises `ErrorAdded`
- `ErrorAdded` — event `MainOutputPlugin` subscribes to, selecting the Output tab so an error is seen without a dialog

**`MainOutputViewModel`** — implements `IOutputManager`. Stores all output as a single `OutputText` string (not a list). Registered as a singleton in DI, aliased as both `MainOutputViewModel` and `IOutputManager`.

**`MainOutputPlugin`** — `PriorityPlugin` that creates the Output tab at `TabLocation.RightBottom`.

## How to Write Output

Inject `IOutputManager` and call `AddOutput` or `AddError` directly — this is the standard approach. Callers must already be on the UI thread.

`IGuiCommands.PrintOutput` is a thin wrapper around `AddOutput` that marshals to the UI dispatcher — use this when calling from a background thread. There is **no `PrintError` equivalent** in `IGuiCommands`; callers needing `AddError` from a background thread must dispatch manually.

## Non-Obvious Behaviors

- **Buffer cap**: `OutputText` is capped at 50,000 chars. When exceeded, it is trimmed to the last 25,000 chars — oldest output is silently discarded.
- **No dispatcher in `IOutputManager`**: `AddOutput`/`AddError` write directly to the `OutputText` property with no thread marshaling. Calling from a background thread will throw.
- **Auto-scroll**: The `TextBox` in the view uses `TextBoxAutoScroll.AutoScrollToEnd="True"` — new output scrolls into view automatically.
- **`AddError` steals the tab**: it selects the Output tab, so it is the wrong call for a routine or repeated message. Use `AddOutput` unless the user genuinely needs to look now.
- **No per-line color**: severity is carried by the `"ERROR:  "` text prefix. The view binds one `TextBox` to one concatenated `OutputText` string, so coloring a single line needs the model changed to a line collection first.

## Key Files

| File | Purpose |
|------|---------|
| `Tools/Gum.Presentation/Managers/MainOutputViewModel.cs` | `IOutputManager` interface + `MainOutputViewModel` implementation |
| `Gum/Commands/GuiCommands.cs` | `PrintOutput` — dispatcher-safe wrapper |
| `Tools/Gum.Presentation/Commands/IGuiCommands.cs` | `PrintOutput` declaration |
| `Gum/Plugins/InternalPlugins/Output/MainOutputPlugin.cs` | Registers the Output tab |
| `Gum/Plugins/InternalPlugins/Output/MainOutputPluginView.xaml` | Output tab view (TextBox + clear button) |
| `Gum/Services/Builder.cs` | DI registration of `MainOutputViewModel` as `IOutputManager` |
