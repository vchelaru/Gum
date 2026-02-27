---
name: gum-tool-output
description: Reference guide for Gum's Output tab system. Load this when working on the Output tab, IOutputManager, MainOutputViewModel, GuiCommands.PrintOutput, or adding output/error messages to the tool.
---

# Gum Tool Output System Reference

## Architecture

**`IOutputManager`** (`Gum/Managers/MainOutputViewModel.cs`) — interface with two methods:
- `AddOutput(string)` — appends a timestamped line
- `AddError(string)` — appends a timestamped line prefixed with `"ERROR:  "` (two spaces)

**`MainOutputViewModel`** — implements `IOutputManager`. Stores all output as a single `OutputText` string (not a list). Registered as a singleton in DI, aliased as both `MainOutputViewModel` and `IOutputManager`.

**`MainOutputPlugin`** — `InternalPlugin` that creates the Output tab at `TabLocation.RightBottom`.

## How to Write Output

Inject `IOutputManager` and call `AddOutput` or `AddError` directly — this is the standard approach. Callers must already be on the UI thread.

`IGuiCommands.PrintOutput` is a thin wrapper around `AddOutput` that marshals to the UI dispatcher — use this when calling from a background thread. There is **no `PrintError` equivalent** in `IGuiCommands`; callers needing `AddError` from a background thread must dispatch manually.

## Non-Obvious Behaviors

- **Buffer cap**: `OutputText` is capped at 50,000 chars. When exceeded, it is trimmed to the last 25,000 chars — oldest output is silently discarded.
- **No dispatcher in `IOutputManager`**: `AddOutput`/`AddError` write directly to the `OutputText` property with no thread marshaling. Calling from a background thread will throw.
- **Auto-scroll**: The `TextBox` in the view uses `TextBoxAutoScroll.AutoScrollToEnd="True"` — new output scrolls into view automatically.

## Key Files

| File | Purpose |
|------|---------|
| `Gum/Managers/MainOutputViewModel.cs` | `IOutputManager` interface + `MainOutputViewModel` implementation |
| `Gum/Commands/GuiCommands.cs` | `PrintOutput` — dispatcher-safe wrapper |
| `Gum/Commands/IGuiCommands.cs` | `PrintOutput` declaration |
| `Gum/Plugins/InternalPlugins/Output/MainOutputPlugin.cs` | Registers the Output tab |
| `Gum/Plugins/InternalPlugins/Output/MainOutputPluginView.xaml` | Output tab view (TextBox + clear button) |
| `Gum/Services/Builder.cs` | DI registration of `MainOutputViewModel` as `IOutputManager` |
