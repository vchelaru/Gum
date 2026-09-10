# Phase 50 — Editor canvases in Avalonia

## Purpose

Host both live editor canvases in the Avalonia head on the cross-platform device phase 10 proved:
the wireframe canvas (`WireframeControl`, `Tool/EditorTabPlugin_XNA`) and the texture-coordinate
canvas (`ImageRegionSelectionControl`, `Gum/TextureCoordinateSelectionPlugin`). Route pointer and
keyboard input from Avalonia into the existing, already-headless editing logic without rewriting
the interaction model. This is the largest single body of work in the plan.

## Builds on

- Phase 10's go: backend package, device-creation path, present option, measured latency.
- Both canvases already derive from `WpfGraphicsDeviceControl`, render to a render target, and
  read back through `IRenderTargetPixelBufferWriter`; the WinForms path is deleted (#4166, #4226, #4227).
- Editing logic is headless in `Gum.Presentation`: `SelectionManager`, `CameraController`, the
  `IInputHandler` family (move/resize/rotate/polygon-point), `WireframeEditor`, drag payloads
  (#3843, #3844, #3852, #3854, #3855). `Cursor` reads an `IInputHostControl`; the WPF adapter is
  `WpfInputHostAdapter` (#3842).
- Scroll bars are already decoupled from WinForms with headless `ScrollBarLogic` and a WPF-native
  bar (#3959, #4156). Grid overlay and snapping are canvas features on `main` (#4381).
- Phase 4b lessons in `../ui-decoupling-plan.md`: device created against the main window handle and
  ref-counted; one unit system for surface, cursor, viewport; render trigger from the compositor;
  re-apply drag effect on every move.

## Decisions

- **One shared `GraphicsDevice` per process, both canvases on it.** Same as today via
  `GraphicsDeviceService`; a second device would split `LoaderManager`'s texture cache. Phase 10
  confirms two render targets on one device.
- **`XnaAndWinforms`, `InputLibrary`, and `FlatRedBall.SpecializedXnaControls` become `net8.0`.**
  The device service, the render-surface host base, `Cursor`, `IInputHostControl`, and
  `ImageRegionSelectionControl`'s logic are needed by the Avalonia head, so they cannot stay
  `net8.0-windows`. Split each into a neutral core (kept name) and a WPF adapter file that moves to
  `Gum/`. `Cursor` keeps `System.Drawing.Point` (an in-box primitive, fine cross-platform) but
  drops the `PointToClient` host coupling into the adapter.
- **`ScreenshotService` stops using `Microsoft.Win32.SaveFileDialog` directly.** It is the one
  canvas-side site that bypasses `IDialogService`; route it through the service WPF-side first
  (a phase-20 "not yet movable" item), then it works in the head for free.
- **Avalonia `IInputHostControl` adapter feeds the same `Cursor`.** Pointer position, focus, size,
  and capture come from Avalonia events; the handlers are untouched.
- **Render on the UI thread, driven by the render loop**, matching the WPF host. Off-thread only if
  phase 10's numbers demand it.
- **Drag-and-drop from the tree onto the canvas** uses the neutral drop payload with an Avalonia
  reader beside the WPF reader.
- **Canvas features stay feature-complete**: grid overlay, snapping, rulers, scroll bars, zoom,
  marquee, keyboard nudge, the alignment/hide-show tools that overlay the canvas.

## Scope

**In:** Avalonia render-surface host; Avalonia input host adapter; both canvases hosted as tabs via
the phase-40 contract; scroll bars in Avalonia bound to `ScrollBarLogic`; drop reader; DPI and
resize correctness; the two helper projects made `net8.0`; the second canvas's own overlays
(background, line grid, nine-slice guide, texture outline) rendering.

**Out:** rewriting any input handler; new canvas features; theming of the canvas chrome (90).

## Tasks

1. Split `XnaAndWinforms` → neutral `GraphicsDeviceService` + render-surface base (`net8.0`) and a
   WPF-only host file in `Gum/`; same for `InputLibrary` (`Cursor`, `IInputHostControl` neutral;
   `WpfInputHostAdapter` moves to `Gum/`). Keep the WPF tool identical.
2. Avalonia render-surface host: `WriteableBitmap` writer, render-loop trigger, resize, DPI.
3. Avalonia input host adapter; wire pointer/keyboard/capture/focus; verify `Cursor` math in one
   unit system at 100/150/200%.
4. Host `WireframeControl`'s draw/activity on the Avalonia surface as the CenterTop tab; hotkeys
   through `HotkeyManager` with `GumKeyEventArgs` from Avalonia keys.
5. Scroll bars, rulers, grid, snapping, marquee, zoom verified.
6. Tree → canvas drag-and-drop through the neutral payload and an Avalonia reader.
7. Second canvas: host `ImageRegionSelectionControl`'s render + overlays on a second render target;
   its own camera and scroll bars; verify texture-region selection round-trips to variables.
8. Manual parity checklist across both canvases on Windows, macOS, Linux; record any per-OS quirks
   in this doc.

## Key files

- `Tool/EditorTabPlugin_XNA/Views/WireframeControl.cs`, `Gum/TextureCoordinateSelectionPlugin/**`
- `XnaAndWinforms/GraphicsDeviceService.cs`, `WpfGraphicsDeviceControl.cs`, `WpfRenderSurfaceHost.cs`
- `InputLibrary/Cursor.cs`, `IInputHostControl.cs`, `WpfInputHostAdapter.cs`
- `Tools/Gum.Presentation/Wireframe/**`, `Plugins/InternalPlugins/EditorTab/**`, `Input/**`
- `.claude/skills/gum-tool-selection`, `gum-monogame-rendering`

## Dependencies

Needs phase 10 (go), phase 30 (head), phase 40 (tab contract). Blocks phase 100's canvas checklist
and the cutover bar. Phase 60's drag-drop needs the drop reader from here.

## Risks

- The biggest phase; split into PRs by task, each keeping WPF identical. Task 1 alone is a
  multi-project change and should land first and separately.
- Per-OS input differences (trackpad scroll, modifier keys, right-click on macOS) surface here;
  keep a per-OS quirk list in this doc.
- If phase 10 triggered the SkiaGum fallback, tasks 2–7 change shape: the canvas draws through
  SkiaGum on Avalonia's Skia surface, and a bitmap-font renderable is added to SkiaGum. Rewrite
  this doc before starting.

## Done when

- [ ] Both canvases render, select, move, resize, rotate, drop, zoom, scroll on all three OSes.
- [ ] `XnaAndWinforms` and `InputLibrary` are `net8.0`; the WPF tool is unchanged.
- [ ] Per-OS quirk list recorded; no feature-flagged-off canvas behavior.
