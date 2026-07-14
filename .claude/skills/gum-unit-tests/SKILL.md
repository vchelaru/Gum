---
name: gum-unit-tests
description: Writing unit tests in the Gum repo. Triggers: tests in Gum.ProjectServices.Tests, Gum.Cli.Tests, or any other Gum test project.
---

# Gum Unit Test Reference

## Test Projects

| Project | Location | What it tests |
|---------|----------|---------------|
| **`MonoGameGum.Tests`** | `MonoGameGum.Tests/` | **Default project for new tests.** MonoGame runtime, Forms controls, rendering, localization, data types — anything not specific to V2/V3 visuals or integration |
| `Gum.ProjectServices.Tests` | `Tests/Gum.ProjectServices.Tests/` | Headless services: error checking, codegen, font generation, project loading |
| `Gum.Cli.Tests` | `Tests/Gum.Cli.Tests/` | CLI command exit codes and output |
| `MonoGameGum.Tests.V2` | `Tests/MonoGameGum.Tests.V2/` | Tests specific to V2 default visuals |
| `MonoGameGum.Tests.V3` | `Tests/MonoGameGum.Tests.V3/` | Tests specific to V3 default visuals |
| `MonoGameGum.IntegrationTests` | `Tests/MonoGameGum.IntegrationTests/` | Requires a real `GraphicsDevice`: content loading, renderer teardown, full `GumService` lifecycle |
| `RaylibGum.Tests` | `Tests/RaylibGum.Tests/` | raylib runtime (incl. the `#if RAYLIB` branches of the source-shared `GueDeriving/*Runtime.cs`) |
| `SkiaGum.Tests` | `Tests/SkiaGum.Tests/` | Skia runtime and shape runtimes |
| `Gum.ProjectServices.SkiaGum.Tests` | `Tests/Gum.ProjectServices.SkiaGum.Tests/` | SkiaGum-backed SVG export (`SkiaGumSvgExportService`, the service behind `gumcli svg` / tool File ▸ Export); drives `SKSvgCanvas` headlessly |

**When in doubt, put tests in `MonoGameGum.Tests/`.** Only use V2/V3 projects for tests that exercise visual-version-specific behavior.

**`SkiaGum.Tests` runs in CI as a blocking suite** (#3233) — it renders into an in-memory CPU raster `SKSurface`, so it is fully headless despite the name. A red Skia test now fails the job like any other Bucket-A suite.

**`RaylibGum.Tests` runs in CI as a blocking Windows suite** (#3250). raylib's `InitWindow` needs an OpenGL 3.3 context the GPU-less runners lack; the Windows job supplies it via Mesa's `llvmpipe` software GL (dropped in next to the test binaries before the suite runs), so the tests run headless and a red raylib test now fails the job like any other Bucket-A suite. (#3233's earlier macOS probe *hung* at GLFW/Cocoa window creation — Win32 window creation is not main-thread-coupled, which is why Windows works.) A green CI run now **does** cover raylib — including the `#if RAYLIB` branches of a source-shared `GueDeriving/*Runtime.cs` — so the old mandatory local pre-merge run is no longer required; still update any assertion that pins old behavior when you change raylib-covered code. (Issue #3234: #3183 changed the raylib stroke-width PreRender but left the suite asserting the pre-#3183 value; back then CI didn't run raylib, so it shipped red — now it would be caught.)

## Key Rules

- Always use **Shouldly** — never xUnit `Assert`. Alphabetize test methods within a class.
- Disable parallel execution in every test project (`[assembly: CollectionBehavior(DisableTestParallelization = true)]`) — Gum uses global singletons.
- Use named parameters for boolean literals.

## Test at production defaults

When a feature's tests disable a production default for isolation (e.g. `LoaderManager.Self.CacheTextures = false`), remember that default is **on** in real apps — so any code path that only runs with it on is left untested. Treat "this test turns a production default off" as a smell: keep at least one test that exercises the path at the production default. (A raylib font regression survived review because every font test ran with caching off, which made a new cache-hit branch dead code.)

## Headless Tests (ProjectServices, MonoGameGum.Tests.V2)

Read `BaseTestClass` before adding setup — it handles singleton init, a ready-made `GumProjectSave`, and `Dispose` cleanup. Don't repeat that in subclasses.

Every `StateSave` must have `ParentContainer` set — `GetValueRecursive` traverses via that field and silently misbehaves or throws when it is null. Use `ScreenSave` for standalone state tests (no base type, no StandardElementsManager fallback).

`InternalsVisibleTo` is set up in `Gum.ProjectServices.csproj` for `Gum.ProjectServices.Tests` — internal members are directly accessible.

## WPF-touching tool code (GumToolUnitTests) needs an STA thread

xUnit's runner is **MTA**, but WPF `FrameworkElement`s (`MenuItem`, `Menu`, `ComboBox`, …) throw `InvalidOperationException: The calling thread must be STA` when constructed. If a tool class news up a WPF control — often a ViewModel building right-click `MenuItem`s in its constructor — build it inside a `RunOnSta(() => { ... })` helper that runs the body on an `ApartmentState.STA` thread and rethrows. Copy the helper from `MenuStripManagerTests` / `RenameManagerTests`; there is no shared base for it.

**Verify the real construction blocker empirically before designing around an assumed one.** A quick throwaway probe (construct the object, see what actually throws) beats reasoning: e.g. `BitmapFrame` PNG decode and most non-control VM constructors run fine on MTA, so the blocker is usually the WPF control, not the singleton/resource you suspected.

## Plugin/DI composition tests (GumToolUnitTests)

`AllPluginsCompositionTests` composes **every** tool plugin through MEF the way `PluginManager.LoadPlugins` does, and `ServiceProviderCompositionSpikeTests` resolves the bridged services from the real `Builder.cs` container. Two reusable techniques live there:

- **Stub anything headlessly without running its constructor.** `RuntimeHelpers.GetUninitializedObject(type)` fabricates a concrete instance (even a heavy WinForms/WPF host singleton) with no ctor call; a Moq proxy covers interfaces/abstract types. Composition/DI only needs the dependency to *exist* as the right type, so this avoids STA/graphics setup entirely. (Run the composition on `RunOnSta` regardless — some plugin *constructors* still touch WPF.)
- **Satisfy not-yet-drained plugins' direct `Locator.GetRequiredService<T>()` ctor calls** with a catch-all `IServiceProvider` registered via `Locator.Register(...)`; remove it in `Dispose` (Locator has no `Unregister` — `RenameManagerTests` shows the reflection teardown).

Keep the MEF batch an **explicit** mirror of `LoadPlugins` (not a catch-all export provider) so a plugin gaining an unbridged `[ImportingConstructor]` dependency turns the test red — that regression signal is the whole point. See the `gum-tool-plugins` skill for keeping `PluginBridgedServiceTypes.All` in sync during drains.

## Golden-image pixel-diff tests (SkiaGum.Tests)

`Tests/SkiaGum.Tests/GoldenImages/` covers rendering behavior that has no `Style`/paint-parameter to assert on (e.g. geometric per-glyph transforms) — the rest of `SkiaGum.Tests`' visual tests assert on paint/style objects instead of pixels. `PixelComparer` is a pure per-pixel/per-channel diff with tolerance (unit-tested in-memory, no files); `GoldenImageAssert.Matches(surface, name)` loads a checked-in baseline PNG from `GoldenImages/Baselines/<name>.png` and diffs it against a rendered `SKSurface`.

Baselines are **approved snapshots, not derived from spec** — same convention as Jest's `--updateSnapshot`. If the baseline is missing or the render regresses, the assertion fails and writes the actual render to `GoldenImages/Actual/<name>.actual.png`; review that PNG, then copy it into the source `GoldenImages/Baselines/` folder to approve it. Add the new `<None Include="GoldenImages\Baselines\**\*.png">` csproj entry's `CopyToOutputDirectory` pattern for any new baseline subfolder.

**Golden-image tests are not currently viable for text.** Pixel-exact comparison assumes identical rasterization on every CI runner, which text breaks even with every obvious source of drift eliminated. Attempted on #3692 (`TextCustomizationGoldenImageTests`, since removed): (1) a system font family (`FontName = "Arial"`) — the macOS Actions image has no Arial and silently substitutes a different typeface, blowing the pixel tolerance; fixed by loading a bundled TTF directly via `SKTypeface.FromFile` and a custom `Topten.RichTextKit.FontMapper` assigned to the static `FontMapper.Default`. (2) `MathF.Sin`-derived glyph offsets/colors — `Math.Sin`/`MathF.Sin` call into the OS math library (ucrt/libSystem/glibc), not guaranteed bit-identical cross-platform, so a last-bit difference nudges a glyph by a sub-pixel amount and flips an antialiased edge pixel; fixed by replacing them with a fixed lookup table of exact integers/bytes. Windows still passed and macOS still failed after **both** fixes — with the identical bundled font and zero floating-point math, Skia itself rasterizes/hints the same glyph outline differently per platform. There is no known fix within this harness's current design (`PixelComparer`'s strict per-pixel-position/channel diff). Until a per-OS-baseline or fuzzy/structural comparison strategy exists, keep golden-image tests restricted to non-text, geometric content (shapes, colors, alpha — see `RectangleGoldenImageTests`) and cover per-glyph geometry (position/color from a `[Custom]`-style callback) with deterministic assertions against RichTextKit's own layout data instead (`TextBlock.FontRuns[i].GlyphPositions`/`.Style`, as in `TextCustomizationTests` — no rasterization involved, so it's exact and OS-independent).

## Integration Tests (MonoGameGum.IntegrationTests)

Use this project for anything requiring a real `GraphicsDevice`. Each test creates a minimal nested `Game` subclass, calls `game.RunOneFrame()` to trigger `Initialize`, then asserts. See `Tests/MonoGameGum.IntegrationTests/MonoGameGum/GumServiceUnitTests.cs` for the established pattern. Always call `LoaderManager.Self?.DisposeAndClear()` in the `Game.Dispose` override to prevent state leaking across tests via the singleton.
