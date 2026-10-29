# Cross-runtime pixel comparison

Draws the same elements with MonoGame and with Skia and compares the pictures, so a runtime that
drifts from MonoGame shows up as a failing cell. MonoGame is the reference; Skia (which Unity uses) is
compared against it.

- `NineSliceCases.cs` (linked into both test projects; `Gum.Presentation.Tests` and `Gum.Avalonia.Tests` glob
  `Tests/Shared/**` and exclude this folder, since they render nothing): the cases, a deterministic texture (every texel
  its own color) and the contact-sheet layout (`CellSize` square cells, `Columns` per row). Case names
  are written with the invariant culture because `NineSliceKnownDifferences` keys on them. A new element type adds
  its own `*Cases.cs` beside it.
- `NineSliceBaselines/*.png`: the MonoGame render of each sheet. Written and kept true by
  `MonoGameGum.IntegrationTests` `NineSliceCrossRuntimeSheetTests`, which fails when MonoGame stops
  producing them. Regenerate only for an intended MonoGame change:
  `GUM_UPDATE_CROSS_RUNTIME_BASELINES=1 dotnet test Tests/MonoGameGum.IntegrationTests/MonoGameGum.IntegrationTests.csproj --filter "FullyQualifiedName~NineSliceCrossRuntimeSheetTests"`, then review the PNG diff.
- `Tests/SkiaGum.Tests/CrossRuntime/SheetComparer.cs`: loads a baseline, compares cell by cell (a pixel
  differs when an RGB channel is more than 2 apart; no anti-aliasing is involved, nearest sampling) and
  writes the pictures.
- `NineSliceCrossRuntimeTests` renders each case with Skia on a CPU surface and asserts the differing
  cells are exactly the `NineSliceKnownDifferences` list, each with a reason. A fix removes its entry; a new
  difference fails the test.

Pictures: set `GUM_COMPARE_OUT=<folder>` on the Skia test run to get `overview.png`, `sheets/<sheet>.png`
(MonoGame | Skia | difference) and `differences/<sheet>/<case>.png` for every differing cell. The same
variable on the MonoGame run also writes its sheets to `<folder>/monogame`.

MonoGame's odd-height rule (`NineSlice.cs`: `Height <= 2 * frame && Height % 2 == 1` shortens the bottom band by
one texel row) reaches more than the 1 high cells. Disabling it changed MonoGame's output for 7 `zoom` cells
and 10 `frame3-9x9` cells (odd heights 1, 3 and 5 up to twice the frame), and for 7 of the `frame3-9x9` cells
Skia matches only with the rule on. In the 4 height-1 `zoom` cells at zoom 2 and 3 the rule is the whole
difference: with it off MonoGame matches Skia. In `frame3-9x9` 5x3, 3x5 and 5x5 and `zoom` 6x6-f2-3x3 at
zoom 2 and 3 the cell differs from Skia with the rule on and off; those are listed as a sampling tie where
the rule also plays a part.

The MonoGame sheet test compares exact RGB on a real GL device. It runs in the `mesa-1` stream of the full CI
(Mesa llvmpipe, software OpenGL), not under the `fast-ci` label, and the baselines come from a Mac GPU. The first
full CI run showed the two rasterizers disagree on 25 cells, all of them texel-boundary ties or fractional
edges. So `NineSliceKnownDifferences` (shared, with the reasons) marks every tie and fractional-edge cell as
rasterizer dependent and the MonoGame sheet test skips those cells; every other cell, including the odd-height
ones, is compared exactly. Skia still compares all cells against the Mac baselines.
