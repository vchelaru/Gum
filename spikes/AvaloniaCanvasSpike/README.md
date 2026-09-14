# Avalonia canvas spike (phase 10)

Throwaway proof of concept for `Direction/avalonia-migration/phase-10-canvas-backend-spike.md`:
render a real Gum project through an XNA-family backend to an off-screen render target, read the
pixels back, and present them in an Avalonia window with correct pointer mapping, resize, DPI, and
zoom. Not in any solution; not shipped; delete when phase 50 lands.

Two heads share one source tree (`Shared/`), so the backends can be compared like for like:

| Head | Backend | Why |
|---|---|---|
| `AvaloniaCanvasSpike.MonoGame` | `MonoGame.Framework.DesktopGL` 3.8.4.1 + `MonoGameGum` | The path `gumcli screenshot` already proves cross-platform in CI (macOS, and Mesa on Windows). |
| `AvaloniaCanvasSpike.Kni` | KNI 4.2.9001 + `nkast.Kni.Platform.SDL2.GL` + `KniGum` | The tool ships on KNI today (`WinForms.DX11`); this is KNI's cross-platform desktop backend. |

## How it works

- `GumCanvasGame` is a `Game` with a 1x1 window moved off-screen (same trick as
  `MonoGameScreenshotService`). It never owns a loop: the host calls `RenderFrame()`, which calls
  `Game.Tick()` on the host's thread. `Draw` renders `GumService.Default.Draw()` into a
  `RenderTarget2D` and `GetData`s it into an RGBA buffer. Single-threaded on purpose: on macOS both
  SDL and the UI toolkit must live on the main thread. Three settings are required or `Tick`
  sleeps: `IsFixedTimeStep = false`, `SynchronizeWithVerticalRetrace = false`, and
  `InactiveSleepTime = TimeSpan.Zero` (the hidden window is never the active window).
- `CanvasView` (Avalonia, code-only) owns a `WriteableBitmap` in device pixels, copies the buffer
  in on a 60 Hz `DispatcherTimer`, shows it in an `Image` with nearest-neighbour sampling, maps
  pointer DIPs to world units (`dip * RenderScaling / zoom`), and draws the hit element's bounds
  back as an overlay (`world * zoom / RenderScaling`). Wheel zooms.
- Hit testing walks the loaded element's children last-to-first using `AbsoluteX/Y` and the
  absolute size.

## Run

From the repo root (the default project is the MonoGameGumFromFile sample):

```
dotnet run --project spikes/AvaloniaCanvasSpike/AvaloniaCanvasSpike.MonoGame
dotnet run --project spikes/AvaloniaCanvasSpike/AvaloniaCanvasSpike.Kni
```

Optional arguments: `<path to .gumx> [ElementName]`.

Headless check (no window; needs a GL-capable machine):

```
dotnet test spikes/AvaloniaCanvasSpike/AvaloniaCanvasSpike.MonoGame.Tests
```

## What to record (phase 10 deliverable)

Fill this in per OS and per head, then copy the numbers into the phase doc.

| | Windows (MonoGame / KNI) | macOS | Linux |
|---|---|---|---|
| Device created with off-screen window | yes / yes | | |
| First frame renders the sample screen | yes / yes (2026-09-10) | | |
| Click selects the element under the cursor at 100% / 150% / 200% | 100%: yes after the edge fix / same | | |
| Resize keeps mapping correct, no smearing | | | |
| frame ms avg at 1024x720 / at 4K (headless test, 60 frames) | 1.65 / 10.3 vs 1.26 / 7.3 | | |
| Wheel zoom keeps overlay aligned | | | |
| Off-screen SDL window stays invisible (macOS may clamp `Window.Position`) | yes / n/a (KNI cannot move it) | | |
| Notes | readback is ~90% of the frame on both; an early "KNI 13x slower" reading was `InactiveSleepTime`, now zeroed | | |

The `FrameTimingTests` test prints the draw / readback / present split for both sizes, with and
without presenting the hidden window, so the same comparison can be produced on any machine with
`dotnet test ... --filter FrameTimingTests --logger "console;verbosity=detailed"`. In the window,
press `P` to toggle presenting.

## Known limitations, deliberately

- `GumService.Update` is not called; input is the host's job. Forms controls therefore do not
  react. That is fine: the tool feeds `Cursor` from its own input host too (phase 50).
- KNI's `GameWindow` has no `Position`, so the KNI head's 1x1 SDL window is not moved off-screen
  and may be visible as a tiny window. MonoGame's head moves it to (-10000, -10000).
- The hidden SDL window's event queue is never pumped. On Windows an unpumped window can be
  flagged "not responding" after a few seconds; it is off-screen, so this is cosmetic for the
  spike, but the real host will pump or use a handle-less device (phase 50).
- Only the first screen (or the named element) is shown; there is no tree, no selection handles.
