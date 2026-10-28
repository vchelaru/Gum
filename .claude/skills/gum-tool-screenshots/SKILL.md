---
name: gum-tool-screenshots
description: Screenshots of the Gum tool (Avalonia head) to check or show a UI change. Triggers: screenshot, --screenshot, --exit-after, PrScreenshot, before/after images, "show me the Variables tab", shader parameter rows.
---

# Screenshots of the Gum tool

Pick the route by what has to be on screen. Load this before the first attempt, not after the first empty image.

| To see | Use |
|---|---|
| A tab, row, dialog or control | The headless harness and `PrScreenshot` (`Tests/Gum.Avalonia.Tests/Harness/README.md`, "PR screenshots"). `Screenshots/ShaderDropdownScreenshotTests.cs` types into the filter box. Run with `GUM_SCREENSHOT_DIR=<folder>`. |
| The real canvas, a graphics device or a compiled `Effect` (shader parameter rows) | The real head: build `Gum.slnx`, then `Tool/Gum.Avalonia/bin/Debug/net10.0/Gum --exit-after 90 --screenshot out.png --user-data <temp folder> --select "Element#Instance" <project.gumx>`. Flags are in `Tools/ParityShots/README.md`. The canvas harness scenarios skip on macOS, so there this is the only route. |

## Landmines in the real-head route

- The capture fires on the first drawn canvas frame, so the Variables and States panels can be empty, and the Variables grid is scrolled to the top. No flag waits for the panels. A temporary `Task.Delay` before `capture?.Invoke()` in `UnattendedRun.RunAsync`, plus setting the grid's `ScrollViewer.Offset` from the window's visual tree, gets the frame. Revert both before committing.
- Open a project at the current `.gumx` version: copy `Tools/Gum.ProjectServices/Templates/Default`, add `Components/X.gucx`, and add `<ComponentReference Name="X" />` to the `.gumx`. Version 1 projects (most of `Samples/` and the CodeGen fixtures) lack newer standard variables such as `ShaderParameters`, so those rows never appear.
- `GUM_ECHO_OUTPUT=1` writes the Output tab to stderr. "Compiled render-target shader" there means the container has an effect.
- Open the PNG and read it before saying anything worked, then send it with `SendUserFile`.
