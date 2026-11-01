---
name: gum-tool-screenshots
description: Screenshots of the Gum tool (Avalonia head) to check or show a UI change. Triggers: screenshot, --screenshot, --exit-after, PrScreenshot, before/after images, "show me the Variables tab", shader parameter rows.
---

# Screenshots of the Gum tool

Pick the route by what has to be on screen. Load this before the first attempt.

A `PrScreenshot` shows the real window, or the real panel inside the real theme and background, never a bare control in an isolated window: the colors then differ from the tool and read as a regression. `Screenshots/StandardsPaletteScreenshotTests.cs` is the pattern (the real `MainWindow`, canvas hidden, both themes).

| To see | Use |
|---|---|
| A tab, row, dialog or control | The headless harness and `PrScreenshot` (`Tests/Gum.Avalonia.Tests/Harness/README.md`, "PR screenshots"). `Screenshots/ShaderDropdownScreenshotTests.cs` types into the filter box. Run with `GUM_SCREENSHOT_DIR=<folder>`. |
| The real canvas, a graphics device or a compiled `Effect` (shader parameter rows) | The real head: build `Gum.slnx`, then `Tool/Gum.Avalonia/bin/Debug/net10.0/Gum --exit-after 90 --screenshot out.png --user-data <temp folder> --select "Element#Instance" <project.gumx>`. Flags are in `Tools/ParityShots/README.md`. The canvas harness scenarios skip on macOS, so there this is the only route. |

## Landmines in the real-head route

- The capture fires on the first drawn canvas frame, so the Variables and States panels can be empty, and the Variables grid is scrolled to the top. No flag waits for the panels. A temporary `Task.Delay` before `capture?.Invoke()` in `UnattendedRun.RunAsync`, plus setting the grid's `ScrollViewer.Offset` from the window's visual tree, gets the frame. Revert both before committing.
- Open a project at the current `.gumx` version: copy `Tools/Gum.ProjectServices/Templates/Default`, add `Components/X.gucx`, and add `<ComponentReference Name="X" />` to the `.gumx`. Version 1 projects (most of `Samples/` and the CodeGen fixtures) lack newer standard variables such as `ShaderParameters`, so those rows never appear.
- `GUM_ECHO_OUTPUT=1` writes the Output tab to stderr. "Compiled render-target shader" there means the container has an effect.
- Open the PNG and read it before saying anything worked, then send it with `SendUserFile`.

## Runtime Forms controls and theme components: GumPreview

The editor canvas does not show how a Forms control looks at runtime (templates, focus, scroll bars, text selection). `Tool/GumPreview` (the host the Preview button launches) loads a `.gumj` through the runtime with real Forms controls and saves the first drawn frame; use it for any change to a theme component, a Forms visual or runtime layout. Use the Avalonia head only for editor UI.

- Build only it: `dotnet build Tool/GumPreview/GumPreview.csproj` (about 1.5 min cold; it builds MonoGameGum and friends). Output: `Tool/GumPreview/bin/Debug/net10.0/GumPreview.exe`.
- Run: `GumPreview.exe --project <copy>/X.gumj --element <Screen or Component name> --exit-after 60 --screenshot <abs path>.png`. A run takes about 4 s and writes a 1024x768 PNG. `--element` includes its folder (`Meadow/DemoScreenGum`). It is a `WinExe`, so no console window opens, and `--exit-after` keeps it unattended: the window is placed off-screen (about -32000,-32000) and takes no focus, so nobody should look at the screen. MonoGame re-centers the window after construction, so the placement is re-applied in `Initialize` and every `Update`; the same position is requested on macOS/Linux, but only Windows is verified there (a window manager may clamp it).
- Diagnostic log: add `--log <abs path>.log` (off by default); it writes timestamped `reload`, `focus`, `key` and `error` lines you can read while Preview runs. From the tool, set env var `GUM_PREVIEW_LOG=<abs path>` before starting `gum` and the launcher passes `--log` to every Preview. Source: `Tool/GumPreview/PreviewDebugLog.cs`.
- Runtime-only states: `--focus <TextBox instance name> [--type <text>] [--select-all]`. `--type` and `--select-all` need `--focus`.
- Project: copy a `.gumj` project that has the theme (for example a Meadow project; the ListBox lives in `Components/Meadow/Controls/ListBox.gucj`) to the scratchpad and edit the copy, never the original. A project carries its own component files, so to test a template change edit the copy's `.gucj`, not `Tools/Gum.ProjectServices/Templates` (`.gucx` XML).
- To get a scroll bar, add more `ListBoxItem` instances (with `.Parent` = `<ListBox>.InnerPanelInstance`) to the copy's screen, or shrink the list.
- Before/after: build GumPreview in each worktree and run each exe on its own project copy. The theme files are data, so the same exe works for both if only a template changed.
- A thumb with no visible track makes a one-pixel change invisible by eye; size the list so the thumb nearly fills it, or diff the two PNGs.
- Exit 1 with "was not ready within --exit-after ... No screenshot was taken" means the project did not load or no frame drew; read the stderr text. Open the PNG and confirm the controls are drawn before reporting.
- A component alone (`--element Meadow/Controls/TextBox`) draws at the top-left of the 1024x768 canvas; `--focus` names an instance inside a screen, so use a screen for the focused/typed/selected look.
