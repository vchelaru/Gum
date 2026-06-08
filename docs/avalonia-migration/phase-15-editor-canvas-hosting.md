# Phase 15 — Editor canvas hosting

> **This is the make-or-break phase of the entire Avalonia migration.** The live editing canvas is today a WinForms control hosting XNA/KNI rendering, embedded in WPF through `WindowsFormsHost`. That host is Windows-only and has **no Avalonia equivalent**. Everything else in the migration (shell, property grid, plugins) is conventional XAML porting; this phase is novel rendering/hosting work. **It is spiked first in its own phase — [Phase 12](phase-12-canvas-spike.md)** (see also [Spike first](#spike-first) below) — before the rest of the Avalonia shell is fully committed. If the Phase 12 spike fails, the migration strategy must change.

## Purpose

Get the live editing canvas rendering inside the Avalonia shell, cross-platform, behind the neutral "panel content" seam introduced in Phase 5 — so the editor plugin (`Tool/EditorTabPlugin_XNA`) does not know or care which UI head (WPF or Avalonia) is hosting it. The canvas must render a real element, accept mouse and keyboard input, and route that input into the existing selection / move / resize / rotate logic without rewriting the editor's interaction model.

This phase integrates the canvas **on top of the cross-platform KNI desktop-GL backend whose stand-up Phase 12 proves**. The editor today has only a Windows-only DirectX 11 backend (`nkast.Kni.Platform.WinForms.DX11`); the cross-platform GL backend does not exist yet and is greenfield, not a swap of the present step (see [Phase 12](phase-12-canvas-spike.md) for the backend stand-up and its go/no-go). Phase 15 consumes that backend and the chosen hosting option — it does not stand up the GL backend itself. There are also **two** live XNA/`GraphicsDevice` canvases in the tool, not one (see [Second live canvas](#second-live-canvas-texturecoordinateselectionplugin) below); this phase's hosting solution must apply to both.

## Decisions & rationale

- **Option A (offscreen `RenderTarget2D` → `WriteableBitmap`) is PREFERRED → Reason:** *on top of a working GL backend*, the present step is the easy part — it reuses the existing render-to-RT + `GetData` readback code (`GraphicsDeviceControl` already does that half on DX11). The "swap only the present step" framing applies **only to the present step on the existing Windows DX11 backend**; the cross-platform GL backend that the editor renders against does not exist yet and is greenfield (its stand-up + go/no-go is [Phase 12](phase-12-canvas-spike.md), not this phase). The "encouraging finding" below de-risks the present step, not the canvas as a whole — the canvas is *not* "mostly done." → **Direction:** integrate Option A behind the Phase 5 seam on top of the Phase 12 GL backend, replacing only the final present step with an Avalonia `WriteableBitmap`.
- **Option B (`NativeControlHost` + native GL) is a FALLBACK only if Option A's latency is unacceptable → Reason:** it reintroduces an airspace/compositing problem over a canvas that needs splitters, overlays, and popups, and it requires divergent Win32/macOS/Linux GL plumbing. → **Direction:** do not pursue B unless Phase 12 (or measured integration) shows A's readback-and-present latency is unworkable.
- **Re-route input into the existing `IInputHandler` chain rather than rewriting the interaction model → Reason:** the handlers already work in world coordinates and read frame deltas off `Cursor` (`GetCursorXChange()` = `Cursor.Self.XChange / Camera.Zoom`); only the input *source* needs abstracting, not the model. → **Direction:** abstract the cursor's input source and feed Avalonia pointer/key events into it; leave the handlers untouched.
- **Start with UI-thread rendering → Reason:** it mirrors today's single-threaded WinForms paint plus the existing reentrancy guard, which is the simplest correct baseline. → **Direction:** render on the UI thread first; move off-thread only if profiling demands it.
- **Gated by Phase 12 → Reason:** this is the highest-risk phase, and a no-go forces a strategy change. → **Direction:** do not start integration until Phase 12 returns a go plus a chosen option (A vs B).

## Scope

### In scope

- Hosting the live wireframe canvas inside an Avalonia control behind the Phase 5 panel-content seam.
- Presenting MonoGame/KNI offscreen render output into Avalonia (Option A) and/or a native GL surface (Option B).
- Re-routing mouse and keyboard input from Avalonia events into `InputLibrary.Cursor` / `SelectionManager` and the editor's `IInputHandler` implementations.
- Resize handling (canvas size change → render target / present surface resize) and DPI/scaling correctness.
- Frame pacing for the editor loop (today a 30 FPS WinForms `Timer` drives `Invalidate`).

### Out of scope

- Property grid, tree view, variable grid, menus — those are Phase 20.
- Plugin theming and visual polish — Phase 25.
- The Phase 5 seam definition itself and the Phase 10 shell skeleton — consumed here, not built here.
- Rewriting the editor's interaction logic (move/resize/rotate/polygon/marquee). We re-route input into it, not rewrite it.
- The standalone throwaway de-risking spike itself — that is **Phase 12**, which owns and executes the spike (render + input + resize + DPI + cross-platform) and hands its go/no-go recommendation here. Phase 15 *consumes* that result and does the real integration; it does not re-run the spike.
- Removing `ObjectFinder.Self` or other static singletons (explicitly out of scope per repo guidelines).

## Spike first

**The throwaway de-risking spike is now its own phase — [Phase 12 (Canvas hosting spike)](phase-12-canvas-spike.md).** It was originally described inline here; it has been broken out so it is a real, owned, gating deliverable that runs standalone, in parallel with / ahead of Phases 5 and 10, and does not wait on them. **Do not start Phase 15's integration until Phase 12 returns a go plus a chosen option (A vs B).** A no-go in Phase 12 forces a strategy change before this phase's investment.

Phase 12 proves all six gates (rendering, input mapping, resize handling, frame pacing/latency, DPI scaling, and cross-platform run on Windows + at least one of macOS/Linux) and hands Phase 15 its measured-latency recommendation. Phase 15 consumes that recommendation and integrates the chosen approach behind the Phase 5 panel-content seam inside the Phase 10 shell — it does not re-run the spike. See Phase 12 for the full gate definitions and their verification checklist.

## Tasks

### Encouraging finding from the existing code (present step only, on DX11)

**Scope this carefully.** The finding below de-risks **only the final present step** of Option A, and only as it exists today on the Windows DX11 backend (`nkast.Kni.Platform.WinForms.DX11`). It does **not** de-risk the cross-platform path: a KNI desktop-GL backend (with its own device-creation, surface formats, and `GetData` readback semantics, all different from DX11) does not exist in the tool graph yet and has never been built against. Standing that GL backend up is the make-or-break work, and it is [Phase 12](phase-12-canvas-spike.md)'s job, gated by its go/no-go — read this finding as "the present step is the easy part once the backend exists," not "the canvas is mostly done."

The current `GraphicsDeviceControl` (`XnaAndWinforms/GraphicsDeviceControl.cs`) **already does the present-step half of Option A on DX11** — confirmed by reading the source:

- The render target format constant is `const SurfaceFormat RtFormat = SurfaceFormat.Bgra32;` (line 38). `TryHandleDeviceReset` creates `renderTarget = new RenderTarget2D(..., RtFormat, DepthFormat.Depth24Stencil8, 1, RenderTargetUsage.PreserveContents)` (the `if (renderTarget == null)` guard is at line 414; the `new RenderTarget2D(...)` creation is at lines 416-421) and sizes the readback buffer `rawImage = new byte[w * h * 4]` (lines 425-430).
- `BeginDraw` calls `GraphicsDevice.SetRenderTarget(renderTarget)` and sets the viewport (lines 290-309).
- `EndDraw` does the GPU→CPU readback: `SetRenderTarget(null)` then `renderTarget.GetData(rawImage)` (lines 320-336).
- `PaintRendertarget` blits `rawImage` into a `System.Drawing.Bitmap` via `LockBits` and presents with `graphics.DrawImageUnscaled(bitmap, 0, 0)` under `CompositingMode.SourceCopy` + `InterpolationMode.NearestNeighbor` (lines 455-490). It has a `Parallel.For` `Marshal.Copy` fast path for the `SurfaceFormat.Bgra32` case (lines 466-477) and an `unsafe` RGBA→BGRA conversion path only for `SurfaceFormat.Color` (lines 462-465, 492-516) — which is dead code given the RT is `Bgra32`.

**On DX11, Option A is essentially: keep the render-to-RT + `GetData` readback, and replace only the final WinForms `Bitmap`/`Graphics.DrawImageUnscaled` present step (lines 455-490) with an Avalonia `WriteableBitmap`.** This materially de-risks the *present step* of the preferred path. It does **not** address the cross-platform backend: on the GL backend that Phase 12 stands up, device creation, surface format, and `GetData` readback may differ from DX11 and must be re-validated there — the present-step reuse only carries over once that backend renders correctly.

### Option A (PREFERRED): RenderTarget2D → Avalonia WriteableBitmap

- Stand up a MonoGame/KNI `GraphicsDevice` without a WinForms `Control` handle. Today `GraphicsDeviceService.AddRef(Handle, ...)` (called from the `GraphicsDeviceControl` ctor, line 121) takes a WinForms window `Handle`. Determine the headless / offscreen device-creation path for the chosen backend. KNI ships a BlazorGL/WASM backend that renders without a desktop window handle, which is the closest precedent for a handle-free device — confirm during the spike whether that creation path is reusable on desktop, or whether a hidden/dummy window is still required. This is the single biggest new piece of plumbing.
- Render the Gum runtime to a `RenderTarget2D` exactly as `WireframeControl.Draw()` does today (`Renderer.Self.Draw(...)`), with format `Bgra32` (already the case).
- Read back pixels (`renderTarget.GetData`) into a reused `byte[]` (reuse the existing buffer-sizing logic to avoid per-frame allocations).
- Create an Avalonia `WriteableBitmap` sized to the control's pixel size (account for DPI: pixel size = DIP size × render scaling). Copy the BGRA buffer into the locked `WriteableBitmap` framebuffer (`WriteableBitmap.Lock()` → copy → present). Avalonia's `Bgra8888` matches the existing `Bgra32` RT format, so the fast `Marshal.Copy`/row-stride path can be reused; the RGBA→BGRA conversion path becomes dead code for this head.
- Present the `WriteableBitmap` in a custom Avalonia `Control` override of `Render(DrawingContext)` (draw the bitmap with nearest-neighbor / no smoothing to preserve the pixel-exact look the tool uses today — note the existing code sets `InterpolationMode.NearestNeighbor`).
- Drive the frame loop from Avalonia: replace the 30 FPS WinForms `Timer`+`Invalidate` with an Avalonia render-loop tick (`TopLevel` frame callback or a `DispatcherTimer`) that runs the editor `Activity()` + `Draw()` + present. Keep the existing "skip if already painting" reentrancy guard.
- Handle resize: on the Avalonia control's size/scaling change, resize the `RenderTarget2D`, the readback buffer, and the `WriteableBitmap` together (mirror the existing `TryHandleDeviceReset` resize logic). Update the camera viewport.
- Validate threading: the GraphicsDevice/render work and the `WriteableBitmap` present must be coordinated. Decide between rendering on the UI thread (simplest, mirrors today's single-threaded WinForms paint) versus a render thread that hands a completed frame to the UI thread for present (lower UI-thread cost, more complexity). Start with UI-thread to match current behavior; only move off-thread if the spike shows it's needed.

### Option B (FALLBACK): Avalonia NativeControlHost + native GL surface

Use only if Option A's readback-and-present latency is unacceptable.

- Use Avalonia's `NativeControlHost` to embed a platform GL surface and present the GraphicsDevice output directly (no CPU readback).
- Accept significantly more platform-specific plumbing: separate handling for Win32 (WGL/ANGLE), macOS (Metal via MoltenVK/OpenGL deprecation), and Linux (GLX/EGL). Backend differences are the dominant cost here.
- Reintroduces an airspace-like problem class: `NativeControlHost` content sits in a separate native surface that does not composite with Avalonia's scene graph (z-order, transparency, overlays, popups over the canvas). This is the same category of problem the WinForms airspace hack worked around — verify splitters, overlays, and tab switching behave.
- Map the GraphicsDevice/backbuffer swap-chain to the native surface and handle device-lost / resize per platform.
- Input still routes through Avalonia (the native surface is only for output), so the Input routing tasks below apply unchanged.

### Input routing (both options)

The editor's input is currently 100% WinForms-event / XNA-`MouseState`-driven and must be re-fed from Avalonia:

- `InputLibrary.Cursor` (`InputLibrary/Cursor.cs`) is a static singleton initialized with a WinForms `Control` (`Cursor.Self.Initialize(this)` in `WireframeControl`). **It does far more than read a WinForms control's position.** Each frame, `Cursor.Activity(currentTime)` polls **XNA global input** — `Microsoft.Xna.Framework.Input.Mouse.GetState()` (and the keyboard path elsewhere uses `Keyboard.GetState()`) — captures `mLastFrameMouseState`/`mMouseState`, translates screen→client via `mControl.PointToClient(...)`, gates on `mControl.Focused`/`Width`/`Height`, and from those two frames **derives ~15 state properties** the handlers depend on: `PrimaryPush`/`PrimaryClick`/`PrimaryDown`/`PrimaryDownIgnoringIsInWindow`, `SecondaryPush`, `MiddleDown` (middle-button pan), `XChange`/`YChange` (per-frame pixel deltas), `IsInWindow`, and `PrimaryDoubleClick` with its own double-click timing (`MaximumSecondsBetweenClickForDoubleClick = .25f`, `mLastClickTime`). **Under an Avalonia/GL head there is no XNA input poll** (`Mouse.GetState()` returns nothing meaningful without the XNA window), so the neutral cursor input source must do more than supply raw position: it must reproduce this **derived edge/timing state** from Avalonia's discrete pointer/key events — synthesize previous-vs-current button transitions (push/click/release edges), maintain the double-click timer, compute `XChange`/`YChange` deltas, and drive `MiddleDown`/`SecondaryPush`. Feeding only position will silently break clicks, drags, double-click, and middle-button pan. The `PointToClient`/`Focused`/`Width`/`Height` dependencies are the WinForms coupling points; the per-frame `Mouse.GetState()`/`Keyboard.GetState()` poll and the derived edge/timing properties are the *behavioral* contract the source must re-implement.
- Keyboard: today `WireframeControl` wires `KeyDown`/`KeyUp` and overrides `ProcessCmdKey` to drive `IHotkeyManager` (`HandleEditorKeyDown`, `HandleKeyUpWireframe`, `ProcessCmdKeyWireframe`). These take WinForms `KeyEventArgs`/`Keys`/`Message`. Map Avalonia `KeyEventArgs`/`Key`/modifiers into the hotkey manager — either by translating to the existing WinForms types at the boundary, or by widening the hotkey manager's signatures to a neutral key enum. Note `IsInputKey` returns true for everything today so the canvas gets all keys; ensure the Avalonia control likewise receives arrow/Tab/command keys (Avalonia handles these via focus + `KeyDown` tunneling — make the canvas focusable and `e.Handled` appropriately).
- The interaction handlers (`Tool/EditorTabPlugin_XNA/Editors/Handlers/`: `IInputHandler`, `InputHandlerBase`, `MoveInputHandler`, `ResizeInputHandler`, `RotationInputHandler`, `PolygonPointInputHandler`) already operate on **world coordinates** — `IInputHandler` exposes `HandlePush(worldX, worldY)`, `HandleDrag()`, `HandleRelease()`, `UpdateHover(worldX, worldY)`, `HasCursorOver(worldX, worldY)`, `TryHandleDelete()`. They are invoked by `SelectionManager` with already-converted world coords, and `InputHandlerBase` additionally reads frame-delta state straight off the `Cursor` singleton (`GetCursorXChange()` = `Cursor.Self.XChange / Camera.Zoom`). **These do not need rewriting** — once `Cursor` is fed correct control-space coordinates (and its `XChange`/`YChange` deltas are correct) and the camera converts to world space as it does today, the handlers work unchanged. `IInputHandler.GetCursorToShow` returns a WinForms `System.Windows.Forms.Cursor`, which must be mapped to an Avalonia `Cursor` at the boundary.
- Marquee selection, mouse wheel zoom, and middle-button pan currently come through `CameraController.HandleMouseDown/HandleMouseMove/HandleMouseWheel` (wired in `WireframeControl.Initialize`) and `Cursor.MiddleDown`. Route Avalonia pointer/wheel events to the same entry points.
- DPI: `Cursor.X`/`Cursor.Y` return **pixel** coordinates today. Decide the canonical coordinate space (DIPs vs device pixels) and convert consistently so a click on a high-DPI display selects the correct element. Mismatch here is the classic "selection is off by a scale factor" bug — cover it in the spike.
- The WinForms airspace hack is **gone** under Avalonia. In the WPF head, `MainPanelViewModel.AddControl(FrameworkElement, ...)` (lines ~110-124) special-cases `element is WindowsFormsHost host && tabTitle == "Editor"` to set `host.Margin = new Thickness(4, 0, 4, 0)`, intended to free the grid splitter / window resize handle from airspace blocking. **Note:** the real Editor tab does **not** actually trigger this branch — `MainEditorTabPlugin.cs:1166` passes a `System.Windows.Controls.Grid` (with the `WindowsFormsHost` nested *inside* it, wrapping `gumEditorPanel`), not a bare `WindowsFormsHost`, so the `element is WindowsFormsHost` test is false for the Editor tab (this matches Phase 5's finding — the branch is effectively dead for the live editor). Either way, under Avalonia the editor is a normal `Control` in the visual tree, so this special case (and the `WindowsFormsHost` wrapping) simply does not exist on the Avalonia path; the splitter and resize handle work natively. (Watch for the Option B exception above, which can reintroduce a similar class of problem.)

### Drag-and-drop onto the canvas (both options)

The canvas is a **drop target** today, and that path is WinForms-`DataObject`/`DragDrop`-driven — it must be re-routed to Avalonia's `DragDrop`/`DataObject` API. Two distinct drops must keep working:

- **External file dropped from the OS file manager** (e.g. a `.png` dragged from Explorer/Finder onto the canvas) — currently a WinForms `DragEnter`/`DragDrop` with `DataFormats.FileDrop`. Map to Avalonia's `DragDrop.DropEvent` reading `DataFormats.Files` from the `DataObject`, hit-test the drop point into world coordinates (reuse the same screen→client→world conversion as pointer input), and run the existing "create from dropped file" logic.
- **Component dragged from the tree into a container on the canvas** — currently a WinForms drag originating in the tree view, dropped onto the wireframe. Map both the drag source (tree) and the drop target (canvas) to Avalonia `DragDrop`; carry the dragged element reference on the `DataObject` and resolve the target container by hit-testing the drop world coordinate.

Checklist items for this task:

- [ ] Drag a `.png` from the OS file manager onto the canvas → creates a sprite.
- [ ] Drag a component from the tree into a container on the canvas → instance is added to that container.

WPF/WinForms `DataObject`/`DragDrop` map to Avalonia `DragDrop`/`DataObject`; the drop-point hit-testing reuses the input-routing coordinate conversion, so it shares the DPI/world-space decisions made above.

### Second live canvas: TextureCoordinateSelectionPlugin

There is a **second** live XNA/`GraphicsDevice` canvas in the tool, separate from the wireframe editor, and porting it is **a second application of this phase's canvas-hosting solution to a different control** — not a free ride on the editor work. Its cost belongs in the canvas budget, not buried in a Phase 25 polish bullet.

- `Gum/TextureCoordinateSelectionPlugin/Views/MainControl.xaml` hosts `ImageRegionSelectionControl` (from `FlatRedBall.SpecializedXnaControls`, a **separate WinForms project**) inside a `WindowsFormsHost`, exactly the airspace-bound hosting model this phase removes for the editor. That `WindowsFormsHost` is Windows-only and has no Avalonia equivalent.
- It carries its own full canvas stack: its own `SystemManagers` (`mainControl.InnerControl.SystemManagers`), its own `Renderer`/`Renderer.Camera`, its own XNA-driven cursor, its own WPF `ScrollBar`s (`VerticalScrollBar`/`HorizontalScrollBar` via `ScrollBarLogicWpf`), and its own overlay managers (`BackgroundManager`, `LineGridManager`, `NineSliceGuideManager`, `TextureOutlineManager`, `IVisualOverlayManager`). It also wires its own keyboard path (`MainControl.HandleKeyDown` → `ImageRegionKeyDown`) and zoom/pan.
- Porting it means re-applying the chosen hosting option (render-to-RT → `WriteableBitmap`, or `NativeControlHost`), the input re-routing, the resize/DPI handling, **and** the scrollbar/overlay coordination — for `ImageRegionSelectionControl` rather than `WireframeControl`. `ImageRegionSelectionControl` lives in `FlatRedBall.SpecializedXnaControls`, so this may require either porting that control to a backend-neutral form or wrapping it the same way the editor canvas is wrapped.
- **Decision forced by Phase 30:** the Phase 30 cutover gate requires **zero** unresolved 'defer' rows in the tool-graph feature matrix. So this canvas must be **either** ported (the second application above) **or** signed off as an explicit, documented cutover blocker (e.g. shipped Windows-only behind a flag, or the texture-coordinate UI temporarily dropped with sign-off). It must **not** be silently deferred. Track it as a real row.

Checklist items for this task:

- [ ] `TextureCoordinateSelectionPlugin`'s `ImageRegionSelectionControl` canvas renders inside the Avalonia head (or is explicitly signed off as a documented cutover blocker per Phase 30).
- [ ] Its scrollbars, zoom/pan, overlays, and keyboard input work under the Avalonia hosting path.

## Key files & projects

- `Tool/EditorTabPlugin_XNA/Views/WireframeControl.cs` — the canvas control (`WireframeControl : GraphicsDeviceControl : System.Windows.Forms.Control`). `Initialize(...)`, `Activity()`, `Draw()`, input wiring, `Cursor.Self.Initialize(this)`. The primary thing being re-hosted.
- `XnaAndWinforms/GraphicsDeviceControl.cs` — the WinForms+XNA bridge base class. Already renders to `RenderTarget2D` and reads back to a `byte[]`/`Bitmap` — the existing template for Option A's present path. `GraphicsDeviceService.AddRef(Handle, ...)` is the WinForms-handle device-creation coupling to replace.
- `XnaAndWinforms/GraphicsDeviceService.cs` — shared `GraphicsDevice` lifecycle, `AddRef`/`ResetDevice`. Source of the headless device-creation work for Option A.
- `Gum/ViewModels/MainPanelViewModel.cs` — `AddControl(System.Windows.Forms.Control, ...)` → `WindowsFormsHost` wrapping, and the `WindowsFormsHost`+`tabTitle == "Editor"` airspace margin branch (~lines 110-124) that does not actually fire for the real Editor tab (it passes a `Grid`) and disappears entirely under Avalonia.
- `InputLibrary/Cursor.cs` — static cursor singleton with WinForms `Control` / XNA `MouseState` coupling; the focal point of input re-routing.
- `Tool/EditorTabPlugin_XNA/Editors/Handlers/` — `IInputHandler.cs`, `InputHandlerBase.cs` (reads `Cursor.Self.XChange`/`YChange` deltas directly), `MoveInputHandler.cs`, `ResizeInputHandler.cs`, `RotationInputHandler.cs`, `PolygonPointInputHandler.cs`. World-coordinate interaction logic that is reused, not rewritten.
- `Tool/EditorTabPlugin_XNA/` (~52 `.cs` files) — wireframe manager, selection, `Services/CameraController.cs` (zoom/pan/`HandleKeyPress`), rulers. Mostly backend-agnostic once the canvas and input seams exist.
- `Gum/TextureCoordinateSelectionPlugin/Views/MainControl.xaml` (+ `.xaml.cs`) — the **second** live canvas: a `WindowsFormsHost` wrapping `ImageRegionSelectionControl` (from `FlatRedBall.SpecializedXnaControls`), with `VerticalScrollBar`/`HorizontalScrollBar` and a `HandleKeyDown` → `ImageRegionKeyDown` path. Must be re-hosted the same way as the editor canvas (or signed off as a cutover blocker).
- `Gum/TextureCoordinateSelectionPlugin/Logic/ControlLogic.cs`, `ScrollBarLogicWpf.cs`, `BackgroundManager.cs`, `LineGridManager.cs`, `NineSliceGuideManager.cs`, `TextureOutlineManager.cs` — the second canvas's own `SystemManagers`/camera/scrollbar/overlay stack that has to be coordinated under the Avalonia hosting path.
- `Gum/Managers/IHotkeyManager.cs` / `HotkeyManager.cs` — the keyboard/hotkey entry points (`HandleEditorKeyDown`, `HandleKeyUpWireframe`, `ProcessCmdKeyWireframe`) that take WinForms `KeyEventArgs`/`Keys`/`Message` and must be fed from Avalonia key events.
- `.claude/skills/gum-monogame-rendering` — rendering pipeline reference; KNI offscreen `RenderTarget` / BlazorGL precedent for the headless render path.

## Dependencies

- **Phase 12 (Canvas hosting spike)** — gates this phase. Phase 12 must return a go plus a chosen hosting option (A vs B) before integration starts; a no-go forces a strategy change. This is the hard precondition for Phase 15.
- **Phase 5 (Extract UI seams)** — the neutral "panel content" abstraction the editor is hosted behind. Required so the editor plugin doesn't reference WPF/WinForms host types.
- **Phase 10 (Avalonia shell skeleton)** — the shell that actually contains the panel where the canvas docks, for the *integrated* deliverable.
- **HIGHEST RISK in the whole migration.** Because of that, the spike (now [Phase 12](phase-12-canvas-spike.md)) does **not** wait on Phases 5/10 — it runs standalone, in parallel and ideally ahead, to retire the risk before those phases are fully committed. Only the final *integration* of the canvas into the real shell (this phase) depends on 5, 10, and the Phase 12 result.

## Risks & notes

- **Input latency (Option A):** the render → `GetData` readback → `WriteableBitmap` copy → present chain adds latency vs a direct swap-chain present. Measure during the spike; if drag feels laggy, that's the trigger to evaluate Option B. The GPU→CPU readback (`GetData`) is the most expensive step and is unavoidable in Option A.
- **Resize:** render target, readback buffer, and `WriteableBitmap` must resize together and atomically with respect to the present; the existing `TryHandleDeviceReset` shows the resize/dispose/recreate dance to mirror. Stale-size frames cause smearing or crashes.
- **DPI / scaling:** Avalonia works in DIPs with a render scaling factor; the GraphicsDevice/render target works in pixels; `Cursor.X/Y` are pixels today. Pick one canonical space and convert at the boundary, or selection and rendering sharpness break on high-DPI displays. Validate at 100/150/200%.
- **GL backend differences (Option B):** Win32 vs macOS (Metal) vs Linux GL plumbing diverges sharply; this is the main reason Option A is preferred.
- **Threading:** today rendering is single-threaded on the WinForms paint message with a reentrancy guard (`simultaneousPaints`) and a `lock(this)` around draw. Preserve single-threaded UI-thread rendering initially; only introduce a render thread if profiling demands it, and then guard the `WriteableBitmap` handoff carefully.
- **Cross-platform GL backend is greenfield (consistency with Phase 12):** the editor's only desktop graphics backend today is Windows-only DirectX 11 (`nkast.Kni.Platform.WinForms.DX11`); there is **no** KNI desktop-GL/SDL backend in the tool graph. The "swap only the present step" reuse is true *only* for DX11. Standing up the GL backend (different device creation, surface formats, and `GetData` readback than DX11) is the make-or-break work and is owned by [Phase 12](phase-12-canvas-spike.md) and its go/no-go — Phase 15 builds on that result, it does not stand the backend up. Do not read the "encouraging finding" as the canvas being mostly done.
- **Second live canvas (`TextureCoordinateSelectionPlugin`):** the texture-coordinate UI hosts a *second* live XNA/`GraphicsDevice` surface (`ImageRegionSelectionControl` in a `WindowsFormsHost`, with its own `SystemManagers`, camera, cursor, scrollbars, and overlays). Porting it is a **second** application of this phase's hosting solution and its cost belongs in the canvas budget. The Phase 30 "zero deferred rows" gate forces a binary decision: port it, or sign it off as an explicit documented cutover blocker — it must not be silently deferred.
- **Cursor input source must reproduce derived edge/timing state, not raw position:** `InputLibrary/Cursor.cs` polls XNA global input every frame (`Mouse.GetState()`/`Keyboard.GetState()`) and derives ~15 properties (push/click/release edges, double-click timing, `XChange`/`YChange` deltas, middle-button pan). That XNA poll does not exist under an Avalonia/GL head. The neutral input source must synthesize the previous-vs-current button transitions, double-click timer, and per-frame deltas from Avalonia's discrete events — abstracting only the WinForms control (position/focus) and leaving the derived state out will silently break clicks, drags, double-click, and pan.
- **Headless device creation:** creating a `GraphicsDevice` without a WinForms window handle is new territory for this codebase on desktop — today the device is created from `GraphicsDeviceService.AddRef(Handle, ...)`. KNI's BlazorGL/WASM backend is the closest handle-free precedent, but it may not transfer cleanly to desktop GL/ANGLE; a hidden offscreen window may still be needed. This is the riskiest single piece of Option A plumbing after latency — prove it in the spike before committing.
- **Airspace hack removed:** the `host.Margin` workaround in `MainPanelViewModel` is deleted under Avalonia (canvas is a real visual-tree control). Option B's `NativeControlHost` can reintroduce a similar non-compositing/z-order problem class — verify overlays, popups, splitters, and tab switching over the canvas.
- **Static singletons:** `InputLibrary.Cursor.Self`, `Renderer.Self`, `SystemManagers.Default`, `PluginManager.Self`, and `ObjectFinder.Self` remain static. Per repo guidelines, do not refactor `ObjectFinder.Self`; the cursor singleton's *input source* is what gets abstracted, not the singleton pattern itself.
- **Respect the repo's refactoring-direction rules:** per `.claude/skills/refactoring-direction`, the input-source abstraction must move *toward* instances — introduce a new small instance class (a single-responsibility cursor input source) and inject it; do **not** promote anything to `static` or demote an interface to a concrete type along the way. Critically, do **not** alter or demote the `Cursor.Self` / `Renderer.Self` singleton pattern — these are intentional singletons in the same category as `ObjectFinder.Self`. Abstract only the *source* of cursor input feeding `Cursor`, leaving the singleton itself in place.

## Done / verification checklist

- [ ] **Phase 12 spike returned a go** with a chosen option (A vs B) and measured-latency evidence — this is the gate; do not start the items below until it is satisfied. (The spike's own render/input/resize/DPI/cross-platform proofs are checked in [Phase 12](phase-12-canvas-spike.md), not re-run here.)
- [ ] Integrated: editor canvas hosted behind the Phase 5 panel-content seam inside the Phase 10 Avalonia shell (editor plugin has no WPF/WinForms host reference), using the option Phase 12 recommended.
- [ ] Load a real `.gumx` project and render an actual element in the Avalonia-hosted canvas.
- [ ] Select an element with the mouse in the Avalonia-hosted canvas.
- [ ] Move an element with the mouse (drag) in the Avalonia-hosted canvas.
- [ ] Resize an element via resize handles in the Avalonia-hosted canvas.
- [ ] Rotate an element (rotation handle) in the Avalonia-hosted canvas.
- [ ] Keyboard input (hotkeys + delete) reaches the editor through Avalonia input.
- [ ] The cursor input source reproduces derived edge/timing state from Avalonia events (push/click/release edges, double-click timing, `XChange`/`YChange` deltas, middle-button pan) — clicks, drags, double-click, and pan all behave, not just raw position.
- [ ] Marquee selection, mouse-wheel zoom, and middle-button pan work.
- [ ] **Second canvas resolved:** `TextureCoordinateSelectionPlugin`'s `ImageRegionSelectionControl` canvas is either hosted in the Avalonia head (render + input + scrollbars + overlays working) **or** signed off as an explicit documented cutover blocker per the Phase 30 zero-deferred-rows gate — not silently deferred.
- [ ] `MainPanelViewModel` airspace `host.Margin` hack and `WindowsFormsHost` wrapping removed from the Avalonia path; splitter and window resize handle work natively.
- [ ] Full select/move/resize round trip verified on **Windows** and on **at least one of macOS/Linux**.
