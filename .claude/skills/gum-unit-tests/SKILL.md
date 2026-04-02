---
name: gum-unit-tests
description: Reference guide for writing unit tests in the Gum repository. Load this when writing or modifying tests in Gum.ProjectServices.Tests, Gum.Cli.Tests, or any other Gum test project.
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

**When in doubt, put tests in `MonoGameGum.Tests/`.** Only use V2/V3 projects for tests that exercise visual-version-specific behavior.

## Key Rules

- Always use **Shouldly** — never xUnit `Assert`. Alphabetize test methods within a class.
- Disable parallel execution in every test project (`[assembly: CollectionBehavior(DisableTestParallelization = true)]`) — Gum uses global singletons.
- Use named parameters for boolean literals.

## Headless Tests (ProjectServices, MonoGameGum.Tests.V2)

Read `BaseTestClass` before adding setup — it handles singleton init, a ready-made `GumProjectSave`, and `Dispose` cleanup. Don't repeat that in subclasses.

Every `StateSave` must have `ParentContainer` set — `GetValueRecursive` traverses via that field and silently misbehaves or throws when it is null. Use `ScreenSave` for standalone state tests (no base type, no StandardElementsManager fallback).

`InternalsVisibleTo` is set up in `Gum.ProjectServices.csproj` for `Gum.ProjectServices.Tests` — internal members are directly accessible.

## Integration Tests (MonoGameGum.IntegrationTests)

Use this project for anything requiring a real `GraphicsDevice`. Each test creates a minimal nested `Game` subclass, calls `game.RunOneFrame()` to trigger `Initialize`, then asserts. See `Tests/MonoGameGum.IntegrationTests/MonoGameGum/GumServiceUnitTests.cs` for the established pattern. Always call `LoaderManager.Self?.DisposeAndClear()` in the `Game.Dispose` override to prevent state leaking across tests via the singleton.
