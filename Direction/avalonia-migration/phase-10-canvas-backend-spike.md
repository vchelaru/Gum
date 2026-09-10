# Phase 10 — Canvas backend spike (standalone, throwaway, gating)

## Purpose

Retire the single novel risk in the migration before anything else is committed: **can the Gum
editor canvas be rendered by a KNI `GraphicsDevice` on macOS and Linux at all, without a window
handle, and presented in an Avalonia control at interactive latency?** Everything else in this plan
is conventional XAML porting. This is not.

The output is a go/no-go with measured numbers and a chosen approach, not shippable code.

## Builds on

- The canvas already renders to an offscreen `RenderTarget2D` (`Bgra32`), reads the pixels back to
  CPU, and hands them to a host through `IRenderTargetPixelBufferWriter` (#3829). The WPF host
  (`WpfRenderSurfaceHost`, `WpfGraphicsDeviceControl` in `XnaAndWinforms`) blits into a
  `WriteableBitmap` at 45–60 fps including 4K (#3837, #4166). **The present step is solved in
  principle; only the target bitmap type changes.**
- The device lifecycle is centralized in `XnaAndWinforms/GraphicsDeviceService.cs` (ref-counted,
  created against the main window handle, `GraphicsProfile.FL10_0`).
- Lessons from the WPF host in `../ui-decoupling-plan.md` Phase 4b (unit systems, render trigger,
  shared device across both canvases).
- The macOS Wine diagnostics (`diagnostics/mac-wine-test-suite`) proved the DX11 FL10_0 device is
  the failure point and that no Wine-side path exists.

## Decisions

- **Prove a non-Windows device first, everything else second.** The tool's only backend is
  `nkast.Kni.Platform.WinForms.DX11`. There is no GL/SDL backend anywhere in the *tool* graph. Until a
  `GraphicsDevice` exists on macOS or Linux, latency and DPI are unmeasurable. The KNI desktop-GL
  platform package name is confirmed against the KNI repo, not guessed.
- **Two candidate backends, evaluated side by side; the in-repo precedent is MonoGame, not KNI.**
  `Gum.ProjectServices.MonoGame` (used by `gumcli screenshot`) already creates a device
  cross-platform with `MonoGame.Framework.DesktopGL`: a `Game` subclass with a
  `GraphicsDeviceManager`, SDL2 window, render to `RenderTarget2D`, read back. CI runs it on
  `macos-15` and, on Windows, under a Mesa software-GL override. The tool compiles
  `RenderingLibrary` as linked sources against the KNI packages with `GUM; MONOGAME` defines, and
  KNI and MonoGame share the `Microsoft.Xna.Framework` API surface, so the spike must answer:
  (a) KNI desktop-GL, headless, or (b) switch the tool's linked `RenderingLibrary` compile to
  MonoGame DesktopGL and reuse the CLI's proven device path (hidden SDL window). (b) has the
  precedent and the CI story; (a) keeps the tool on the runtime it ships to KNI users. Measure
  both if (a) works at all; record the choice as an ADR because it changes what the tool's
  renderer is built on.
- **Prefer KNI over SkiaGum for the editor canvas.** The WYSIWYG canvas must match what a MonoGame
  game renders, including bitmap fonts and sprite sampling, and the editor's wireframe objects,
  selection handles, and `Renderer.Self` pipeline are built on `RenderingLibrary`. Switching to
  SkiaGum would be a second, larger migration inside this one. SkiaGum is the fallback, not the plan.
- **Option A (render target → CPU readback → Avalonia `WriteableBitmap`) is the present path.**
  It is what the WPF host does today. Option B (`NativeControlHost` with a native GL surface) is
  evaluated only if A's measured latency on macOS/Linux is unacceptable, because B reintroduces
  airspace problems over a canvas that needs overlays and popups, and needs per-OS GL plumbing.
- **A "go" requires macOS or Linux evidence.** Windows DX11 already works. Proving A on Windows
  proves nothing about the risk this phase exists to retire.
- **Explicit fallback ladder** if the KNI GL device cannot be created or cannot hit the bar:
  (1) hidden 1×1 native window solely for a handle; (2) KNI's offscreen/Blazor-GL device path;
  (3) render the editor canvas through SkiaGum on Avalonia's native Skia surface, and add a
  bitmap-font text renderable to SkiaGum to close the fidelity gap. Fallback (3) changes phase 50's
  shape and must be recorded as a plan edit, not a footnote.

## Scope

**In:** a throwaway Avalonia app outside the product solutions (e.g. `spikes/AvaloniaCanvasSpike/`)
that creates a KNI desktop-GL device with no WinForms handle, renders a real Gum screen through
`RenderingLibrary` to a render target, reads back, presents via `WriteableBitmap`, maps pointer
position to world coordinates (click selects the element under the cursor), resizes without
leaking, stays sharp at 100/150/200% DPI, and reports measured drag latency on macOS and/or Linux.
Windows is a secondary cross-check.

**Out:** the real `WireframeControl`, the real input handlers, marquee/zoom/pan, plugins, the
shell, anything in `Gum.Avalonia`. Removing any sanctioned singleton.

## Tasks

1. Identify and reference the KNI desktop-GL platform package; create a `GraphicsDevice` on macOS
   or Linux. Attempt order: `DeviceWindowHandle = IntPtr.Zero` rendering only to a render target;
   then a hidden 1×1 native window for a handle; then KNI's offscreen device path.
2. Render a Gum screen (`.gumx` loaded through `Gum.ProjectServices`) into the render target using
   the same calls `WireframeControl` makes; read back through `IRenderTargetPixelBufferWriter`.
3. Present into an Avalonia `Image` backed by a `WriteableBitmap`; drive frames from Avalonia's
   render loop, not a timer (mirror the `CompositionTarget.Rendering` lesson).
4. Pointer → world mapping; validate against the same DPI/offset math `Cursor` and
   `WpfInputHostAdapter` use, in one unit system.
5. Resize: render target, readback buffer, bitmap resized together; no smearing; mapping still right.
6. Measure drag latency and frame time on macOS and/or Linux at 1080p and 4K. Record numbers.
7. Try the same device with a second render target to confirm two canvases can share one device
   (the texture-coordinate canvas needs this, and a second device would split `LoaderManager`'s
   texture cache).
8. Write the recommendation: backend package, device-creation path that worked, Option A vs B,
   latency numbers, fallback triggered or not. Update phase 50 accordingly.

## Spike status (2026-09-09, Windows only so far)

The spike exists at `spikes/AvaloniaCanvasSpike/` (README there). Two heads share one source:
MonoGame DesktopGL and KNI `nkast.Kni.Platform.SDL2.GL` 4.2.9001.1 (the package name is now
confirmed; it is the same KNI version the tool ships). Verified on Windows by `dotnet build` of
both heads and `dotnet test` of a headless test per backend that creates the device with no host
window, renders the MonoGameGumFromFile sample screen to a render target, reads it back, and
hit-tests the centre pixel. **Both backends pass.** On 2026-09-10 both Avalonia heads were run on
Windows and showed the sample screen, status line, click selection, and zoom. macOS and Linux
runs are still open.

Findings so far:

- **The backends perform the same; an earlier "KNI is 13x slower" reading was a configuration
  trap, now fixed.** The first Windows run showed KNI at 23.9 ms per frame against MonoGame's
  1.8 ms. Splitting the frame into draw / readback / present showed KNI's three parts summing to
  about 3 ms, with 20 ms unaccounted for inside `Game.Tick()`: the XNA-family default
  `InactiveSleepTime` of 20 ms, applied because the backend's hidden window is never the active
  window. Setting it to zero removes it. **Any host that steps a `Game` from its own UI thread
  must set `InactiveSleepTime = TimeSpan.Zero`**, or the canvas silently runs at ~40 fps.
  Measured after the fix, 60-frame averages on Windows (headless test, same scene):

  | Size | MonoGame DesktopGL | KNI SDL2.GL |
  |---|---|---|
  | 1024x720 | 1.65 ms (readback 1.23) | 1.26 ms (readback 0.77) |
  | 3840x2160 | 10.3 ms (readback 9.7) | 7.3 ms (readback 6.7) |

  Both desktop GL backends read back with `glGetTexImage` (checked in both repos), presenting
  the hidden window costs under 0.1 ms, and draw is under 0.5 ms. **Readback is the whole cost
  and it scales with pixel count**, so 4K at 60 Hz is feasible but not free; the real host
  should read back only when the scene changed, as the WPF host already honours a requested
  frame rate by skipping passes.

- **Single-threaded is workable.** The `Game` never owns a loop; the host calls `Game.Tick()` from
  its UI thread after one `RunOneFrame()`. This keeps SDL and the UI toolkit on the main thread,
  which macOS requires. `IsFixedTimeStep = false`, vsync off, and `InactiveSleepTime = TimeSpan.Zero`
  are all required or `Tick` sleeps.
- **KNI's `GameWindow` has no `Position`**, so its 1x1 window cannot be parked off-screen the way
  the CLI parks MonoGame's. A KNI-based host needs another way to hide it (borderless + hidden,
  or a handle-less device). MonoGame's `Window.Position` works.
- **`GumService.Initialize(GraphicsDevice, projectFile)` exists** as a `Game`-free overload. If
  a `GraphicsDevice` can be created without a `Game` on SDL2.GL, the spike's `Game` wrapper goes
  away entirely. Not tried yet.
- Both backends need `HiDef` for Apos.Shapes (#4403), same as the CLI.

## Key files

- `spikes/AvaloniaCanvasSpike/` — the spike (shared source, two heads, two headless test projects)
- `Tools/Gum.ProjectServices.MonoGame/MonoGameScreenshotService.cs` — the proven cross-platform device path
- `.github/workflows/build-and-test.yaml` — the Mesa software-GL override used for GL tests on Windows runners
- `XnaAndWinforms/GraphicsDeviceService.cs`, `WpfGraphicsDeviceControl.cs`, `WpfRenderSurfaceHost.cs`
- `IRenderTargetPixelBufferWriter` in `Gum.Presentation` and the WPF `WriteableBitmap` writer (#3834)
- `InputLibrary/Cursor.cs`, `InputLibrary/IInputHostControl.cs`, `InputLibrary/WpfInputHostAdapter.cs`
- `Tool/EditorTabPlugin_XNA/Views/WireframeControl.cs` (mirror `Draw`, do not port)
- `.claude/skills/gum-monogame-rendering` for the render pipeline; the KNI repo for platform packages

## Dependencies

Needs nothing. Runs in parallel with phase 20. **Gates phase 50** and, through it, the cutover
acceptance bar. A no-go with fallback (3) triggered also changes phase 30's Skia dependency list.

## Risks

- Apple has deprecated OpenGL on macOS. It still ships (4.1) and is what most KNI/MonoGame desktop-GL
  apps use there, but a Metal-backed KNI path, if one exists, should be noted as a future hedge.
- GPU→CPU readback cost on GL may differ from DX11; 4K at 60 fps may not hold. The bar is
  "interactive dragging feels immediate," measured, not "60 fps."
- Two canvases on one device, with per-canvas cameras, must not fight over device state.

## Done when

- [x] A `GraphicsDevice` is created on **Windows** with no WinForms handle on both backends (headless tests, 2026-09-09).
- [ ] A `GraphicsDevice` is created on macOS and/or Linux with no WinForms handle, and the path is written down.
- [x] A real Gum screen renders in an Avalonia window on **Windows** on both backends (2026-09-10).
- [ ] A real Gum screen renders in an Avalonia window on that OS; click selects the right element at 100/150/200%.
- [ ] Drag latency and frame time are recorded for 1080p and 4K on that OS.
- [ ] Two render targets on one device work.
- [ ] Recommendation written into this doc; phase 50 updated; fallback recorded if triggered.
