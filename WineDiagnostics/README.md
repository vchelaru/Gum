# Gum macOS / Wine diagnostic probe suite

The Gum **tool** runs under Wine on Linux but currently **crashes on launch under Wine on macOS**
(documented in [`docs/gum-tool/setup/README.md`](../docs/gum-tool/setup/README.md) since the
November 2025 release that moved to .NET 8). Nobody has captured *why* yet - the setup docs say
"We are unable to determine why."

This suite exists to answer that. It is a set of tiny, single-purpose programs ("probes"), each of
which exercises **exactly one layer** of the Gum tool's startup stack. You publish them on Windows,
copy them to the Mac, and run them under the same Wine prefix Gum uses. The **first probe that fails**
tells you which layer is broken - turning "it crashes" into a specific, reportable root cause.

## What the Gum tool actually loads (why these probes)

The tool is `net8.0-windows` and stacks several native layers. Under Wine these are what can break:

| Layer | What it is in Gum | Probe |
| --- | --- | --- |
| .NET 8 desktop runtime | The runtime itself (`dotnetdesktop8` in the prefix) | `Probe0.Runtime` |
| WPF | The editor's windows, panels, menus, dialogs | `Probe1.Wpf` |
| WinForms + GDI+ | The tree view (`MultiSelectTreeView`) and its custom theming | `Probe2.WinForms` |
| WinForms-in-WPF (`WindowsFormsHost`) | How the rendering control is embedded in the WPF shell | `Probe3.WindowsFormsHost` |
| SkiaSharp (CPU raster) | SVG/Lottie rendering (CPU -> texture; the GL path is compiled out of the tool) | `Probe4.SkiaCpu` |
| Direct3D 11 (raw) | The graphics API KNI renders through - Wine must translate it to the host GPU | `Probe5.Direct3D11` |
| KNI on DX11 | The design-canvas `GraphicsDevice` the tool actually creates | `Probe6.KniDx11` |
| MonoGame OpenGL | Alternative backend (SDL2 / OpenGL) - does the GL path dodge the D3D limit? | `Probe7.MonoGameDesktopGL` |
| MonoGame DX11 | Alternative backend (Direct3D 11), tries HiDef then Reach | `Probe8.MonoGameWindowsDX` |

The prime suspect is the **Direct3D 11** path. On macOS, OpenGL is deprecated and capped at 4.1, so
Wine must translate D3D11 either through WineD3D-over-OpenGL (often insufficient for D3D11) or through
DXVK -> Vulkan -> MoltenVK -> Metal. `Probe5` tests that translation with no engine in the way; `Probe6`
tests the exact KNI call the tool makes (`new GraphicsDevice(GraphicsAdapter.DefaultAdapter,
GraphicsProfile.FL10_0, parameters)`, mirroring [`XnaAndWinforms/GraphicsDeviceService.cs`](../XnaAndWinforms/GraphicsDeviceService.cs)).

## Build & bundle (on Windows)

```powershell
pwsh ./publish.ps1
```

This publishes all probes as `win-x64` and produces `gum-mac-diagnostics.zip`. By default the probes
are **framework-dependent**, so under Wine they use the same .NET 8 desktop runtime Gum uses - the
most faithful test. Use `-SelfContained` to bundle the runtime instead (larger, but removes the
prefix's runtime as a variable):

```powershell
pwsh ./publish.ps1 -SelfContained
```

## Run (on the Mac)

Copy `gum-mac-diagnostics.zip` to the Mac, unzip it, then:

```sh
chmod +x run_mac_diagnostics.sh
./run_mac_diagnostics.sh
```

By default it uses the Wine prefix created by `setup_gum_mac.sh` (`~/.wine_gum_dotnet8`). Pass a
different prefix or dist folder as arguments:

```sh
./run_mac_diagnostics.sh "$HOME/.wine_gum_dotnet8" .
```

Useful overrides:

```sh
# keep each window open 10s so you can watch it render
PROBE_HOLD_SECONDS=10 ./run_mac_diagnostics.sh

# quieter Wine logs, or different channels
WINEDEBUG=-all ./run_mac_diagnostics.sh
```

The runner enables first-chance exception + DLL-load tracing (`WINEDEBUG=+seh,+loaddll`) and .NET
managed crash dumps (`DOTNET_DbgEnableMiniDump`), and unsets the `DOTNET_ROOT*` vars that break
dotnet apps under Wine (Gum issue #1957).

## Interpreting the results

The runner prints a summary and writes a `results_<timestamp>/` folder containing, per probe:

- `<probe>.log` - the structured step log (what succeeded, the first exception + stack if it failed)
- `<probe>.console.log` - raw stdout/stderr including the `WINEDEBUG` trace
- `<probe>.<pid>.dmp` - a .NET managed crash dump (only if the runtime crashed hard)

Read the summary top-to-bottom and find the **first non-PASS**:

- **`Probe0` fails** -> the .NET 8 desktop runtime isn't working in the prefix. Reinstall
  `dotnetdesktop8`; nothing else can pass until this does.
- **`Probe1`/`Probe2`/`Probe3` fail** -> a WPF / WinForms / interop problem, independent of graphics.
- **`Probe4` fails** -> SkiaSharp's native library can't load or run.
- **`Probe5` fails (raw D3D11)** -> the Direct3D 11 translation layer can't produce a device on this
  Mac/Wine/renderer combination. This is the most likely culprit. Re-run after switching the renderer
  (`~/bin/gum vulkan` vs `~/bin/gum opengl`) and confirming MoltenVK is installed - then compare.
- **`Probe5` PASSES but `Probe6` fails** -> D3D11 works, but not at the feature level the tool's KNI
  device asks for (`GraphicsProfile.FL10_0` = feature level 10.0). Compare `Probe5`'s reported
  `FeatureLevel` with `Probe6`'s `Reach (FL9.1)` line: if raw D3D11 maxes out at 9_x, the tool is
  simply requesting more than Wine-on-macOS provides.
- **`Probe7` PASSES** -> the OpenGL path works here. Moving the tool to an OpenGL backend would
  sidestep the Direct3D feature-level ceiling entirely.
- **`Probe8` reports `HighestWorkingProfile = Reach`** -> the Direct3D path only supports a low
  feature level on this Mac; lowering the tool's `GraphicsProfile` from `FL10_0` toward `Reach` is a
  candidate fix.

A probe whose result is `CRASH (no result line)` died before it could even write its result - the
`.console.log` (with the `WINEDEBUG` backtrace) and the `.dmp` are the evidence in that case.

## Reporting

Attach the generated `gum-mac-diag-results-<timestamp>.tar.gz` to a
[Gum GitHub issue](https://github.com/vchelaru/Gum/issues) or the Discord, noting your macOS version,
chip (Intel / Apple Silicon), Wine version (`wine --version`), and selected renderer.

## Notes for maintainers

- This suite is **standalone** - it has its own `WineDiagnostics.sln` and is intentionally *not* part
  of `GumFull.sln` / `AllLibraries.sln`, so it never affects the tool or runtime builds and pulls no
  submodules.
- Probe versions are pinned to match the tool: KNI `4.2.9001`, SkiaSharp `3.119.1`. If the tool bumps
  either, bump them here too so the probes keep testing what actually ships.
- `Probe.Common.ProbeLog` is a static logger by design (a throwaway diagnostic tool, outside Gum's DI
  architecture), which is why it does not follow the I-prefixed-service rule in `code-style.md`.
