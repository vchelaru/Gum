# Gum on macOS/Wine - Fix Plan

Status: **Phase 1 in progress** (minimal fix being built for Mac testing).
Branch: `diagnostics/mac-wine-test-suite`.

## Problem

The Gum **tool** (a WPF/WinForms Windows app with an embedded KNI Direct3D 11
render window) runs under Wine on Linux but **crashes on launch under Wine on
macOS**, broken since the November 2025 .NET 8 release. The cause was never
captured ("we are unable to determine why" in the setup docs).

## Root cause (confirmed by the probe suite on a real test Mac)

The embedded KNI window is created with `GraphicsProfile.FL10_0` (Direct3D feature
level 10.0) in `XnaAndWinforms/GraphicsDeviceService.cs`. Under Wine on macOS the
Direct3D 11 path tops out at **feature level 9_3** (Apple capped OpenGL at 4.1; the
prefix is not routing D3D through DXVK/Metal). `FL10_0` has no fallback, so device
creation throws `NoSuitableGraphicsDeviceException` and the tool dies on launch.

Everything else works under Wine on the Mac: .NET 8 runtime, WPF, WinForms, GDI+,
the WinForms-in-WPF host, SkiaSharp (CPU + HarfBuzz), and raw `D3D11CreateDevice`.

### Evidence (probe results, macOS/Wine)

| Probe | Result | Takeaway |
| --- | --- | --- |
| 0-5 (runtime, WPF, WinForms, host, Skia, raw D3D11) | PASS | the app stack is fine |
| 5 Direct3D11 | max feature level **9_3** | the D3D ceiling under Wine on this Mac |
| 6 KNI `FL10_0` | FAIL; **Reach (9.1) = OK** | the exact crash; a lower profile creates a device |
| 7 MonoGame DesktopGL (OpenGL) | PASS on real GPU; **8192 textures OK** | GL gives full caps, no downgrade |
| 9 KNI `SDL2.GL` (OpenGL) | PASS on real "Vega 56" | **KNI itself runs over OpenGL** |
| 10 KNI DX profile scan | highest = **HiDef** (FL10_0/10_1/11_x fail) | HiDef negotiates down to 9_3; FL10_0 demands exactly 10.0 |
| 11 Skia HarfBuzz | PASS | extra Skia native lib loads |
| 12 WMI (System.Management) | FAIL (`wminet_utils.dll` missing) | WMI dead in prefix, but the tool does not call it directly |

## Fix options

### Fix B - minimal, ship first (this phase)

Pick the highest profile the adapter reports via `GraphicsAdapter.IsProfileSupported`
(`FL10_0 -> HiDef -> Reach`) and construct the device **exactly once**.

> Note: an earlier attempt used construct-and-catch (try `FL10_0`, fall back on the
> exception). That got the device created on the Mac but crashed the **.NET
> Finalizer** thread (NULL page fault) - the failed `FL10_0` device left a
> half-initialized, finalizable husk whose native D3D release crashes under Wine.
> Querying support up front avoids ever creating that husk. See the iteration log.

- **Windows:** `FL10_0` succeeds first - behavior is **unchanged** (8192 textures).
- **macOS/Wine:** `FL10_0` and `HiDef`(*) fall to whatever the adapter supports
  (here 9_3, max texture 4096) so the tool **launches** instead of crashing.
- One file, no architectural change, keeps the existing DX11 + WinForms hosting.
- Trade-off on Mac only: max texture 4096 instead of 8192 (a 4K canvas fits).

(*) Probe 10 showed `HiDef` creates a device on the Mac where `FL10_0` does not.

Site: `XnaAndWinforms/GraphicsDeviceService.cs` (the only tool occurrence of
`GraphicsProfile.FL10_0`).

### Fix A - no capability loss, follow-up

Switch the tool's KNI platform from `nkast.Kni.Platform.WinForms.DX11` to a GL
platform (`nkast.Kni.Platform.SDL2.GL`). Probe 9 proved KNI runs over OpenGL on the
real GPU and Probe 7 proved 8192 textures work on GL - full capability. Cost: the
tool embeds its render surface via a WinForms `GraphicsDeviceControl` (DX11), so a
GL surface must be re-hosted in the WPF/WinForms shell. Real engineering, tracked
as a follow-up.

## Phases

- **Phase 0 - Diagnose (DONE).** Probe suite in `WineDiagnostics/` localized the
  crash to KNI `FL10_0` vs the Wine 9_3 ceiling.
- **Phase 1 - Minimal fix (IN PROGRESS).** Implement the `FL10_0 -> HiDef -> Reach`
  fallback in `GraphicsDeviceService.cs`. Build the tool, package it for Mac.
- **Phase 2 - Verify on Mac.** Run the patched `Gum.exe` under the Wine prefix.
  Success = the editor window opens and renders.
- **Phase 3 - GL backend (optional, no-downgrade).** Pursue Fix A if 4096 textures
  prove limiting.
- **Phase 4 - Housekeeping (optional).** Confirm WMI/AppCenter device-info paths are
  guarded; consider lifting the D3D ceiling via DXVK in the Mac prefix.

## Testing

- **Empirical (primary):** run the packaged tool under Wine on the Mac (Phase 2).
  Graphics device creation is hardware/driver-bound and not unit-testable; the probe
  suite is the harness that validated `HiDef`/`Reach` work on the target.
- **Regression:** the tool must still build and launch on Windows (FL10_0 path
  unchanged there).

## Rollback

Single-file change; revert `GraphicsDeviceService.cs` to restore the prior behavior.

## Iteration log (Mac testing)

1. **Run 1 - baseline crash.** Stock tool: `NoSuitableGraphicsDeviceException` -
   adapter does not support `FL10_0`. Root cause confirmed.
2. **Run 2 - construct-and-catch.** `FL10_0 -> HiDef -> Reach` via try/catch. The
   device was created (`wined3d_cs` thread up; `d3d11`/`wined3d`/`libMoltenVK`/AMD
   driver all loaded) - the FL10_0 blocker is **passed**. But the process crashed
   with a NULL page fault on the **.NET Finalizer** thread, deep in `coreclr`: the
   discarded failed-`FL10_0` device gets finalized and its native release crashes
   under Wine.
3. **Run 3 - construct-once via `IsProfileSupported`.** Logged
   `Graphics profile selected: HiDef` and the device was created - **graphics is no
   longer the blocker.** Execution reached `WireframeControl.Initialize`, which then
   threw an exception; the app's `catch` calls `DialogService.ShowMessage("Error
   initializing the wireframe control...")`, and the modal dialog's repaint tripped a
   `Debug.Assert` in `BackgroundManager.Activity` ("Initialize must be called before
   Activity") -> `FailFast`. That assert is **Debug-build-only** (stripped in Release),
   i.e. a secondary artifact of testing a Debug build; the *real* failure is the caught
   exception inside `WireframeControl.Initialize`, whose text we do not yet have.
4. **Run 4 - capture the real wireframe exception (current).** Added a stdout log of
   the caught exception in `WireframeControl.Initialize` (fires before the dialog/assert)
   so the run log shows the actual failure. Still a Debug build for this diagnostic step;
   a Release build is the right config for the eventual launch test.

## Baseline (captured 2026-06-28 - `backtrace_orig.txt`)

The stock released `Gum.exe` (no changes) crashes on the Mac at the **identical**
signature as the husk crash: page fault on read to `0x0` at `kernelbase+0xd887`, same
`coreclr`/`ntdll` frames, on the **.NET Finalizer** thread. This **confirms** the
original crash is the FL10_0 finalizer-husk NULL-deref - NOT a clean main-thread
`NoSuitableGraphicsDeviceException` as first described. It also explains why Run 2
(construct-and-catch) reproduced it exactly (the husk is still created) and why Run 3
(construct-once via `IsProfileSupported`) is the correct fix (no husk -> got past graphics).

## Methodology note (course-correction)

The graphics root cause was first *inferred* from the probe suite and tested with **Debug**
builds; the original released-app baseline was captured only later. Lesson: capture the real
app's crash first. The probe inference (`FL10_0` fails) turned out correct on the cause, but
the failure *mechanism* (finalizer husk, not a main-thread throw) was only pinned down by the
baseline. Outstanding: read the real `WireframeControl.Initialize` exception from the Run 4
build (`patched-output.log`) - a separate downstream blocker the original app never reached.

## Run 4 result - THE REAL CEILING (revised conclusion)

`patched-output.log`: `Graphics profile selected: HiDef`, then
`System.NotSupportedException: Shader model 4.0 is not supported by the current graphics
profile 'HiDef'` from `Apos.Shapes.ShapeBatch..ctor` -> `ShapeRenderer.Initialize` ->
`WireframeControl.Initialize:188`.

The root constraint is a single thing: **WineD3D on this Mac caps Direct3D at feature
level 9_3, but the tool needs FL10.0** - for the device (`FL10_0`) AND for the
Apos.Shapes shaders (Shader Model 4.0). Lowering the device profile is therefore a
**dead end**: it gets past device creation but dies loading SM4.0 shaders, because
SM4.0 requires FL10.0 regardless of the chosen profile. The `GraphicsDeviceService`
profile-fallback change is at best a graceful-degrade safety net; it does NOT make
the Mac work.

### The real fix: lift the D3D feature level (no app code change)

Make Wine expose FL11 by routing D3D11 through **DXVK -> Vulkan -> MoltenVK -> Metal**
instead of WineD3D-over-OpenGL (which is stuck at 9_3 because macOS GL is capped at 4.1).
The **Linux** setup already installs DXVK; the **macOS** setup script does NOT (it only
installs MoltenVK and sets the WineD3D renderer to vulkan, which still caps at 9_3). So
the actual setup gap is: add `winetricks dxvk` to `setup_gum_mac.sh`. With DXVK active,
the UNMODIFIED tool should run (FL10_0 device + SM4.0 shaders both supported).

Confirm cheaply with the probe suite after `winetricks dxvk`: Probe5's reported
`FeatureLevel` should jump from `9_3` to `11_0` and Probe6 (KNI `FL10_0`) should flip to
PASS. Then run the stock tool.

### Alternative (code): the OpenGL backend

Switch the tool's KNI platform to `nkast.Kni.Platform.SDL2.GL`. Probe9 proved KNI runs
over OpenGL on the real GPU (no FL ceiling); GL shaders sidestep the SM4.0/FL10.0 D3D
limit. Bigger change (GL-compiled Apos.Shapes content + re-hosting the GL surface in the
WinForms shell). Pursue only if DXVK-on-macOS proves unworkable.
