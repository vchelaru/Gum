# Phase 12 — Canvas hosting spike (standalone, throwaway)

## Purpose

Retire the single highest risk in the whole migration — hosting the XNA/KNI editor canvas cross-platform without `WindowsFormsHost` — **before** the shell and the full canvas integration are committed. This phase is the standalone, throwaway de-risking spike that Phase 15 ("Editor canvas hosting") explicitly says must "run standalone, in parallel and ideally ahead" and "does not wait on Phases 5/10." Pulling the spike into its own early phase makes it a real, owned, gating deliverable instead of a bullet buried inside the highest-risk phase.

The spike is intentionally minimal: no plugins, no real Gum shell, no Phase 5 seams, no Phase 10 window. It exists only to answer one question with measured evidence: **can a KNI `GraphicsDevice` be created and render the Gum runtime into an Avalonia control, accept input, resize, and stay sharp on high-DPI — on macOS and/or Linux (not just Windows)?** Its output is a go/no-go recommendation (Option A vs Option B) with numbers, not a shippable feature.

**The cross-platform graphics backend is greenfield, not a swap of the present step.** Today the editor canvas's only desktop graphics backend is `nkast.Kni.Platform.WinForms.DX11` — Windows-only DirectX 11 (see `Tool/EditorTabPlugin_XNA/EditorTabPlugin_XNA.csproj` and `XnaAndWinforms`, which targets `net8.0-windows`). `GraphicsDeviceService.cs` creates the device with `DeviceWindowHandle = windowHandle` and `GraphicsProfile.FL10_0`, i.e. DX11. There is **no** KNI desktop-GL/SDL/DesktopGL backend anywhere in the tool graph (the `DesktopGL` references elsewhere in the repo belong to the runtime shape libraries, not the editor). So "render to an RT, read it back, and swap only the present step" is true **only** for Windows DX11. The hard, novel work — and the thing this spike exists to retire — is standing up a KNI desktop-GL `GraphicsDevice` on macOS/Linux **at all**, before any present/`WriteableBitmap` work matters.

**There is also a second live canvas.** `TextureCoordinateSelectionPlugin` hosts a second live XNA/`GraphicsDevice` surface (`ImageRegionSelectionControl` from `FlatRedBall.SpecializedXnaControls`). The canvas-hosting cost recurs; the approach this spike chooses must be reusable for it too (full integration is Phases 15/25), so the spike must not assume there is only one canvas.

**If this spike fails, the migration strategy must change** (e.g. a different hosting model, scoping the Avalonia head without the live canvas, or shipping the canvas Windows-only behind a feature flag — see the fallback below). That is why it is decoupled and early.

## Decisions & rationale

- **Decision:** Broken out as its own early, standalone phase rather than a bullet inside Phase 15. **Reason:** canvas hosting is the make-or-break risk of the whole migration. **Direction:** make it a gating deliverable so the project is forced into an honest early go/no-go before the shell and full integration are committed.
- **Decision:** The *first* thing to prove is a non-Windows `GraphicsDevice` at all — select and stand up a KNI desktop-GL backend (`nkast.Kni.Platform.*` GL/SDL/DesktopGL) and successfully create a `GraphicsDevice` on macOS and/or Linux — before any present/`WriteableBitmap` work. **Reason:** the editor's only existing desktop backend is Windows-only DX11; there is no GL/SDL backend in the tool graph today, so this is greenfield and is the real risk. The RT-readback-and-present approach is meaningless if no cross-platform device can be created. **Direction:** prove device creation on macOS/Linux first; everything else (present, input, latency) is downstream of it.
- **Decision:** Option A (`RenderTarget2D` → `WriteableBitmap`) is the present path to prove **once a GL device exists**. **Reason:** the existing `GraphicsDeviceControl` already renders to a `Bgra32` `RenderTarget2D` and reads it back, so Option A swaps only the final present step, and `Bgra8888` ↔ `Bgra32` is a 1:1 format match — but note that template is the Windows DX11 path; the spike replays the *technique* on the new GL device. **Direction:** prove Option A on the GL device; only evaluate Option B (`NativeControlHost`) if Option A's measured latency on macOS/Linux is unacceptable.
- **Decision:** The go/no-go latency gate runs on the GL backend on a non-Windows OS, not on Windows DX11. **Reason:** Windows DX11 already works under WinForms today and proves nothing about the cross-platform risk; a "go" must rest on macOS/Linux evidence. **Direction:** a "go" requires the GL device + readback + present proven on macOS or Linux. Proving it only on Windows DX11 is explicitly **not** sufficient.
- **Decision:** Explicit fallback if the GL path can't hit the latency bar. **Reason:** desktop-GL headless device creation + per-frame readback may not reach interactive latency on macOS/Linux, and that must be a planned-for outcome, not a project-ending surprise. **Direction:** if it can't hit the threshold, the editor canvas may ship **Windows-only behind a feature flag**, and the Phase 30 cutover acceptance bar is renegotiated. This is the genuine make-or-break.
- **Decision:** The deliverable is evidence + a recommendation, not shippable code. **Reason:** the spike's findings feed Phase 15, but its code is disposable. **Direction:** optimize for measured answers; let the spike code be throwaway. Keep the chosen approach reusable in principle for the second canvas (`TextureCoordinateSelectionPlugin`), even though that integration is Phases 15/25.

## Scope

### In scope

- Selecting a KNI desktop-GL backend (`nkast.Kni.Platform.*` GL/SDL/DesktopGL) and proving a `GraphicsDevice` can be created with it on macOS and/or Linux — the editor has no such backend today, so this is the first and largest unknown.
- A throwaway standalone Avalonia app (not part of `Gum.Avalonia`'s product graph) that proves canvas hosting end-to-end on the new GL device: render + input + resize + DPI, on macOS/Linux (Windows as a secondary cross-check only).
- Standing up the GL `GraphicsDevice` without a WinForms `Control` handle (the headless/offscreen device-creation path) on the GL backend.
- Presenting `RenderTarget2D` output into an Avalonia control via `WriteableBitmap` (Option A), or pivoting to a native GL surface via `NativeControlHost` (Option B) only if Option A's latency on macOS/Linux is unacceptable.
- Mapping Avalonia pointer position to the correct world coordinate (click selects the element under the cursor) — proving the input-mapping math, not wiring the real editor handlers.
- Measuring drag latency and validating DPI correctness at 100/150/200%.
- A written recommendation: Option A vs Option B, with measured latency as evidence.

### Out of scope

- Integrating the canvas into the real shell or behind the Phase 5 panel-content seam — that is the *integration* half of Phase 15.
- Re-routing the real editor input handlers (`MoveInputHandler`/`ResizeInputHandler`/etc.), full keyboard/hotkey mapping, marquee/zoom/pan — Phase 15.
- The Phase 5 seams and the Phase 10 shell — the spike deliberately bypasses both.
- Any production code in `Gum.Avalonia`. The spike is throwaway; its *findings* feed Phase 15, its code does not have to.
- Removing or refactoring static singletons (`Renderer.Self`, `Cursor.Self`, `ObjectFinder.Self`) — out of scope per repo guidelines.

## Tasks

The spike must prove **all seven** of the following, **in this order** — the first task is the gating unknown and everything else is downstream of it (these are the Phase 15 spike gates, owned and executed here):

1. **Cross-platform GL device creation (do this first).** Select a KNI desktop-GL backend (`nkast.Kni.Platform.*` — GL/SDL/DesktopGL) and successfully create a `GraphicsDevice` with it **on macOS and/or Linux at all**. This is greenfield: the editor's only desktop backend today is `nkast.Kni.Platform.WinForms.DX11` (Windows-only DX11; `GraphicsDeviceService.cs` uses `DeviceWindowHandle = windowHandle` + `GraphicsProfile.FL10_0`), and no GL/SDL backend exists in the tool graph. Until a non-Windows `GraphicsDevice` exists, nothing below can be evaluated. The headless attempt order below applies to **this new GL backend**, not the existing DX11 path.
2. **Headless device creation (on the GL backend).** Determine and prove the GL device-creation path that does not take a WinForms window `Handle` (the DX11 path's `GraphicsDeviceService.AddRef(Handle, ...)` requires one; that coupling does not exist for the new GL backend, which is exactly why a fresh path is needed). Attack it with a falsifiable starting hypothesis and a defined attempt order **against the GL device**:
   1. Create the GL `GraphicsDevice` with `DeviceWindowHandle = IntPtr.Zero` and render **only** to an offscreen `RenderTarget2D` — never present to a backbuffer. The editor path renders exclusively to the RT + `GetData` and never uses the swap-chain backbuffer, so a real window handle may be unnecessary.
   2. If the KNI GL device constructor rejects `IntPtr.Zero`, create one hidden 1×1 native window solely for its handle, and still render only to the RT (never to a backbuffer).
   3. Only if neither works, evaluate the KNI BlazorGL path (offscreen `RenderTarget`, proven via its BlazorGL/WASM target).
3. **Rendering.** The GL `GraphicsDevice` renders the Gum runtime to an offscreen `RenderTarget2D` (format `Bgra32`) and the pixels appear correctly inside an Avalonia control. Replay the technique from `XnaAndWinforms/GraphicsDeviceControl.cs` (which renders to an RT and reads it back to a `byte[]` on the Windows DX11 path): Option A swaps the final present step for an Avalonia `WriteableBitmap` (`Bgra8888` matches `Bgra32`).
4. **Input mapping.** Avalonia pointer position maps to the correct world coordinate; a click selects the element under the cursor. Validate the DPI/control-offset/render-target-vs-control-size math — the classic "selection off by a scale factor" bug lives here.
5. **Resize handling.** Resizing the host control resizes the `RenderTarget2D`, readback buffer, and `WriteableBitmap` together without crash, leak, or smearing; input mapping stays correct after resize. Mirror the existing `TryHandleDeviceReset` resize/dispose/recreate dance.
6. **Frame pacing / latency (measured on the GL backend, non-Windows).** Drive a frame loop from Avalonia (render-loop tick or `DispatcherTimer`, replacing the 30 FPS WinForms `Timer`+`Invalidate`) and **measure** drag latency **on macOS or Linux** — do not eyeball it, and do not substitute Windows DX11 numbers. The GPU→CPU readback (`GetData`) is the unavoidable Option A cost; quantify it on the GL device.
7. **DPI + cross-platform.** Sharp (not blurry-upscaled) render with nearest-neighbor present at 100/150/200%, and a click that lands on the right pixel. Must run on **at least one of macOS/Linux** (Windows DX11 is a secondary cross-check only — Windows-only success does **not** retire the risk).

Then: **record the recommendation** (Option A vs Option B) with the measured non-Windows latency, the GL backend chosen, and the headless-device-creation approach that worked, as this phase's deliverable for Phase 15 to build on. If the latency bar cannot be met on macOS/Linux, record the Windows-only-behind-a-feature-flag fallback and the renegotiated Phase 30 acceptance bar instead.

## Key files & projects

- `Tool/EditorTabPlugin_XNA/EditorTabPlugin_XNA.csproj` and `XnaAndWinforms` (targets `net8.0-windows`) — establish that the editor's only desktop backend today is `nkast.Kni.Platform.WinForms.DX11`; there is **no** GL/SDL backend here. This is the gap task 1 must fill.
- KNI desktop-GL package (`nkast.Kni.Platform.*` GL/SDL/DesktopGL) — the greenfield backend to select and stand up for macOS/Linux. (The `DesktopGL` references that exist in the repo today belong to the runtime shape libraries, not the editor, and are not this backend.)
- `XnaAndWinforms/GraphicsDeviceControl.cs` — renders to `RenderTarget2D` and reads back to `byte[]`/`Bitmap` on the **Windows DX11** path; the *technique* template for Option A's present path, to be replayed on the new GL device. `GraphicsDeviceService.AddRef(Handle, ...)` is the WinForms-handle coupling that the GL path must avoid.
- `XnaAndWinforms/GraphicsDeviceService.cs` — shared DX11 `GraphicsDevice` lifecycle (`AddRef`/`ResetDevice`, `DeviceWindowHandle = windowHandle`, `GraphicsProfile.FL10_0`); reference for what the new GL headless device-creation path must replace.
- `TextureCoordinateSelectionPlugin` / `ImageRegionSelectionControl` (`FlatRedBall.SpecializedXnaControls`) — the **second** live XNA/`GraphicsDevice` surface in the tool; the spike's chosen approach must be reusable for it (integration is Phases 15/25, but the spike must not assume a single canvas).
- `Tool/EditorTabPlugin_XNA/Views/WireframeControl.cs` — the real canvas (`Activity()`/`Draw()`, `Renderer.Self.Draw(...)`); referenced to mirror the render call, not ported here.
- `InputLibrary/Cursor.cs` — shows the control-space/world-space and `PointToClient` coupling the spike's input-mapping math must reproduce neutrally.
- `.claude/skills/gum-monogame-rendering` — rendering pipeline reference; KNI offscreen `RenderTarget` / BlazorGL precedent for the headless path.
- A new throwaway spike project (e.g. `spikes/AvaloniaCanvasSpike/`, not added to product solutions) — created and discarded within this phase.

## Dependencies

- **Needs nothing from Phases 5/10/15.** The spike is standalone by design; that decoupling is the whole point.
- **Benefits from Phase 7 (CI matrix):** the Windows + macOS/Linux runners established there make the cross-platform proof reproducible, but the spike can also be validated locally per OS.
- **Gates / unblocks Phase 15 (Editor canvas hosting):** Phase 15's *integration* work (canvas behind the Phase 5 seam, inside the Phase 10 shell, real input handlers) should not start until this spike returns go + a chosen option. A no-go here forces a strategy change before further investment.
- Can run **in parallel with Phases 5 and 10**, since it touches none of their code.

## Risks & notes

- **This is the make-or-break gate.** Treat a no-go as a real outcome that changes the plan, not a formality. The value of this phase is an early, honest answer.
- **The cross-platform backend is greenfield — this is the biggest risk.** The editor canvas is Windows-only DX11 (`nkast.Kni.Platform.WinForms.DX11`) today; no KNI desktop-GL/SDL backend exists in the tool graph. Standing up a GL `GraphicsDevice` on macOS/Linux *at all* is the first and largest unknown, ahead of present/latency.
- **Latency must be proven non-Windows.** Windows DX11 already works under WinForms and proves nothing about the cross-platform risk. A "go" requires GL device + readback + present measured on macOS or Linux; Windows DX11 numbers do not count.
- **Fallback if the GL path can't hit the latency bar.** If desktop-GL headless device creation + per-frame readback can't reach the interactive latency threshold on macOS/Linux, the editor canvas may ship **Windows-only behind a feature flag**, and the Phase 30 cutover acceptance bar is renegotiated. This is the genuine make-or-break; plan for it as a real branch, not a failure.
- **The canvas-hosting cost recurs (second canvas).** `TextureCoordinateSelectionPlugin` hosts a second live XNA/`GraphicsDevice` surface (`ImageRegionSelectionControl`). The spike's chosen approach must be reusable for it; don't pick something that only works for a single canvas (its integration is Phases 15/25).
- **Input latency (Option A).** Render → `GetData` readback → `WriteableBitmap` copy → present adds latency vs a direct swap-chain present. Measure on the GL backend; if drag feels laggy, that triggers Option B evaluation (and feeds the Windows-only fallback decision).
- **Headless device creation is novel here.** Creating a `GraphicsDevice` with no window handle on desktop is new territory for this codebase; KNI's offscreen path is the proven reference and the thing to validate first — on the new GL backend, not the existing DX11 path.
- **DPI math.** Avalonia works in DIPs with a render-scaling factor; the GraphicsDevice/RT works in pixels; `Cursor.X/Y` are pixels. Pick one canonical space and convert at the boundary, or selection/sharpness break on high-DPI.
- **Option B reintroduces an airspace-class problem.** `NativeControlHost` content doesn't composite with Avalonia's scene graph (z-order, overlays, popups, splitters) — the same category the WinForms airspace hack worked around. Only pivot to B if A's latency forces it, and note this cost.
- **Throwaway, not foundation.** Resist over-building. The deliverable is evidence + a recommendation; the code is allowed to be ugly and is not what Phase 15 ships.

## Done / verification checklist

- [ ] KNI desktop-GL backend (`nkast.Kni.Platform.*` GL/SDL/DesktopGL) selected and a `GraphicsDevice` successfully created with it **on macOS and/or Linux** — the gating first task; the chosen backend is recorded.
- [ ] Standalone throwaway spike app exists and runs (no Gum shell, no Phase 5 seams, no Phase 10 window).
- [ ] Headless GL `GraphicsDevice` creation (no WinForms `Handle`) proven on the GL backend and the approach recorded.
- [ ] Renders the Gum runtime to an offscreen `RenderTarget2D` on the GL device and presents it into an Avalonia control.
- [ ] Avalonia pointer position maps to the correct world coordinate (click selects the element under the cursor).
- [ ] Resize works without crash/leak/smearing; input mapping stays correct after resize.
- [ ] Drag latency measured **on the GL backend on macOS or Linux** (not Windows DX11) against a concrete threshold: end-to-end drag latency **≤ ~16 ms (≈1 frame) at 1080p** and **≤ ~33 ms at 4K**, averaged over a fixed sample of N frames. If the threshold is exceeded, evaluate Option B (`NativeControlHost`); if it still can't be met, record the Windows-only-behind-a-feature-flag fallback and renegotiated Phase 30 bar. This is the reproducible gating criterion — not an eyeballed judgement.
- [ ] Validated at 100%, 150%, and 200% DPI (sharp render, click lands on correct pixel).
- [ ] GL path proven on **at least one of macOS or Linux** — Windows DX11 is a secondary cross-check only and does not by itself satisfy the gate.
- [ ] Chosen approach confirmed reusable in principle for the second canvas (`TextureCoordinateSelectionPlugin` / `ImageRegionSelectionControl`).
- [ ] Recommendation recorded: Option A vs Option B (or Windows-only fallback), with measured non-Windows latency as evidence, handed to Phase 15.
