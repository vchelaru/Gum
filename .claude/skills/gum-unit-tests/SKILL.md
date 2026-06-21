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

## Integration Tests (MonoGameGum.IntegrationTests)

Use this project for anything requiring a real `GraphicsDevice`. Each test creates a minimal nested `Game` subclass, calls `game.RunOneFrame()` to trigger `Initialize`, then asserts. See `Tests/MonoGameGum.IntegrationTests/MonoGameGum/GumServiceUnitTests.cs` for the established pattern. Always call `LoaderManager.Self?.DisposeAndClear()` in the `Game.Dispose` override to prevent state leaking across tests via the singleton.
