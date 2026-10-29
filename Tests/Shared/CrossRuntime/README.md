# Cross-runtime pixel comparison

Draws the same elements with MonoGame and with Skia and compares the pictures, so a runtime that
drifts from MonoGame shows up as a failing cell. MonoGame is the reference; Skia (which Unity uses) is
compared against it.

- `NineSliceCases.cs` (linked into both test projects): the cases, a deterministic texture (every texel
  its own color) and the contact-sheet layout (`CellSize` square cells, `Columns` per row). A new
  element type adds its own `*Cases.cs` beside it.
- `NineSliceBaselines/*.png`: the MonoGame render of each sheet. Written and kept true by
  `MonoGameGum.IntegrationTests` `NineSliceCrossRuntimeSheetTests`, which fails when MonoGame stops
  producing them. Regenerate only for an intended MonoGame change:
  `GUM_UPDATE_CROSS_RUNTIME_BASELINES=1 dotnet test Tests/MonoGameGum.IntegrationTests/MonoGameGum.IntegrationTests.csproj --filter "FullyQualifiedName~NineSliceCrossRuntimeSheetTests"`, then review the PNG diff.
- `Tests/SkiaGum.Tests/CrossRuntime/SheetComparer.cs`: loads a baseline, compares cell by cell (a pixel
  differs when an RGB channel is more than 2 apart; no anti-aliasing is involved, nearest sampling) and
  writes the pictures.
- `NineSliceCrossRuntimeTests` renders each case with Skia on a CPU surface and asserts the differing
  cells are exactly its `KnownDifferences` list, each with a reason. A fix removes its entry; a new
  difference fails the test.

Pictures: set `GUM_COMPARE_OUT=<folder>` on the Skia test run to get `overview.png`, `sheets/<sheet>.png`
(MonoGame | Skia | difference) and `differences/<sheet>/<case>.png` for every differing cell. The same
variable on the MonoGame run also writes its sheets to `<folder>/monogame`.
