# Phase 12 — Canvas hosting spike (standalone, throwaway)

## Purpose

Retire the single highest risk in the whole migration — hosting the XNA/KNI editor canvas cross-platform without `WindowsFormsHost` — **before** the shell and the full canvas integration are committed. This phase is the standalone, throwaway de-risking spike that Phase 15 ("Editor canvas hosting") explicitly says must "run standalone, in parallel and ideally ahead" and "does not wait on Phases 5/10." Pulling the spike into its own early phase makes it a real, owned, gating deliverable instead of a bullet buried inside the highest-risk phase.

The spike is intentionally minimal: no plugins, no real Gum shell, no Phase 5 seams, no Phase 10 window. It exists only to answer one question with measured evidence: **can a MonoGame/KNI `GraphicsDevice` render the Gum runtime into an Avalonia control, accept input, resize, and stay sharp on high-DPI — on Windows and at least one of macOS/Linux?** Its output is a go/no-go recommendation (Option A vs Option B) with numbers, not a shippable feature.

**If this spike fails, the migration strategy must change** (e.g. a different hosting model, or scoping the Avalonia head without the live canvas). That is why it is decoupled and early.

## Decisions & rationale

- **Decision:** Broken out as its own early, standalone phase rather than a bullet inside Phase 15. **Reason:** canvas hosting is the make-or-break risk of the whole migration. **Direction:** make it a gating deliverable so the project is forced into an honest early go/no-go before the shell and full integration are committed.
- **Decision:** Option A (`RenderTarget2D` → `WriteableBitmap`) is the path to prove first. **Reason:** `GraphicsDeviceControl` already renders to a `Bgra32` `RenderTarget2D` and reads it back, so Option A swaps only the final present step, and `Bgra8888` ↔ `Bgra32` is a 1:1 format match. **Direction:** prove Option A first; only evaluate Option B (`NativeControlHost`) if Option A's measured latency is unacceptable.
- **Decision:** The deliverable is evidence + a recommendation, not shippable code. **Reason:** the spike's findings feed Phase 15, but its code is disposable. **Direction:** optimize for measured answers; let the spike code be throwaway.

## Scope

### In scope

- A throwaway standalone Avalonia app (not part of `Gum.Avalonia`'s product graph) that proves canvas hosting end-to-end: render + input + resize + DPI, cross-platform.
- Standing up a MonoGame/KNI `GraphicsDevice` without a WinForms `Control` handle (the headless/offscreen device-creation path).
- Presenting `RenderTarget2D` output into an Avalonia control via `WriteableBitmap` (Option A), or pivoting to a native GL surface via `NativeControlHost` (Option B) only if Option A's latency is unacceptable.
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

The spike must prove **all six** of the following (these are the Phase 15 spike gates, owned and executed here):

1. **Rendering.** A MonoGame/KNI `GraphicsDevice` renders the Gum runtime to an offscreen `RenderTarget2D` (format `Bgra32`, as `GraphicsDeviceControl` already produces today) and the pixels appear correctly inside an Avalonia control. Reuse the encouraging existing finding: `XnaAndWinforms/GraphicsDeviceControl.cs` already renders to an RT and reads it back to a `byte[]` — Option A swaps only the final WinForms `Bitmap`/`DrawImageUnscaled` present step for an Avalonia `WriteableBitmap` (`Bgra8888` matches `Bgra32`).
2. **Headless device creation.** Determine and prove the device-creation path that does not take a WinForms window `Handle` (today `GraphicsDeviceService.AddRef(Handle, ...)` requires one, creating the device with `DeviceWindowHandle = windowHandle` and `GraphicsProfile.FL10_0` in `GraphicsDeviceService.cs`). This is the single biggest new piece of plumbing and the riskiest part of Option A after latency, so attack it with a falsifiable starting hypothesis and a defined attempt order:
   1. Create the `GraphicsDevice` with `DeviceWindowHandle = IntPtr.Zero` and render **only** to an offscreen `RenderTarget2D` — never present to a backbuffer. The editor path already renders exclusively to the RT + `GetData` and never uses the swap-chain backbuffer, so a real window handle may be unnecessary.
   2. If the KNI/MonoGame device constructor rejects `IntPtr.Zero`, create one hidden 1×1 native window solely for its handle, and still render only to the RT (never to a backbuffer).
   3. Only if neither works, evaluate the KNI BlazorGL path (offscreen `RenderTarget`, proven via its BlazorGL/WASM target).
3. **Input mapping.** Avalonia pointer position maps to the correct world coordinate; a click selects the element under the cursor. Validate the DPI/control-offset/render-target-vs-control-size math — the classic "selection off by a scale factor" bug lives here.
4. **Resize handling.** Resizing the host control resizes the `RenderTarget2D`, readback buffer, and `WriteableBitmap` together without crash, leak, or smearing; input mapping stays correct after resize. Mirror the existing `TryHandleDeviceReset` resize/dispose/recreate dance.
5. **Frame pacing / latency.** Drive a frame loop from Avalonia (render-loop tick or `DispatcherTimer`, replacing the 30 FPS WinForms `Timer`+`Invalidate`) and **measure** drag latency — do not eyeball it. The GPU→CPU readback (`GetData`) is the unavoidable Option A cost; quantify it.
6. **DPI + cross-platform.** Sharp (not blurry-upscaled) render with nearest-neighbor present at 100/150/200%, and a click that lands on the right pixel. Runs on **Windows and at least one of macOS/Linux** — Windows-only success does not retire the risk.

Then: **record the recommendation** (Option A vs Option B) with the measured latency, and the headless-device-creation approach that worked, as this phase's deliverable for Phase 15 to build on.

## Key files & projects

- `XnaAndWinforms/GraphicsDeviceControl.cs` — already renders to `RenderTarget2D` and reads back to `byte[]`/`Bitmap`; the template for Option A's present path. `GraphicsDeviceService.AddRef(Handle, ...)` is the WinForms-handle coupling to replace.
- `XnaAndWinforms/GraphicsDeviceService.cs` — shared `GraphicsDevice` lifecycle (`AddRef`/`ResetDevice`); source of the headless device-creation work.
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
- **Input latency (Option A).** Render → `GetData` readback → `WriteableBitmap` copy → present adds latency vs a direct swap-chain present. Measure; if drag feels laggy, that triggers Option B evaluation.
- **Headless device creation is novel here.** Creating a `GraphicsDevice` with no window handle on desktop is new territory for this codebase; KNI's offscreen path is the proven reference and the thing to validate first.
- **DPI math.** Avalonia works in DIPs with a render-scaling factor; the GraphicsDevice/RT works in pixels; `Cursor.X/Y` are pixels. Pick one canonical space and convert at the boundary, or selection/sharpness break on high-DPI.
- **Option B reintroduces an airspace-class problem.** `NativeControlHost` content doesn't composite with Avalonia's scene graph (z-order, overlays, popups, splitters) — the same category the WinForms airspace hack worked around. Only pivot to B if A's latency forces it, and note this cost.
- **Throwaway, not foundation.** Resist over-building. The deliverable is evidence + a recommendation; the code is allowed to be ugly and is not what Phase 15 ships.

## Done / verification checklist

- [ ] Standalone throwaway spike app exists and runs (no Gum shell, no Phase 5 seams, no Phase 10 window).
- [ ] Renders the Gum runtime to an offscreen `RenderTarget2D` and presents it into an Avalonia control.
- [ ] Headless `GraphicsDevice` creation (no WinForms `Handle`) proven and the approach recorded.
- [ ] Avalonia pointer position maps to the correct world coordinate (click selects the element under the cursor).
- [ ] Resize works without crash/leak/smearing; input mapping stays correct after resize.
- [ ] Drag latency measured and recorded against a concrete threshold: end-to-end drag latency **≤ ~16 ms (≈1 frame) at 1080p** and **≤ ~33 ms at 4K**, averaged over a fixed sample of N frames. If the threshold is exceeded, evaluate Option B (`NativeControlHost`) and record the choice with rationale. This is the reproducible gating criterion — not an eyeballed judgement.
- [ ] Validated at 100%, 150%, and 200% DPI (sharp render, click lands on correct pixel).
- [ ] Runs on **Windows** and on **at least one of macOS or Linux**.
- [ ] Recommendation recorded: Option A vs Option B, with measured latency as evidence, handed to Phase 15.
