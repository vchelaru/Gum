# April 2026 Refactoring Tasks

## ~~Task: Remove ControlsCompatability.cs~~ (Done)

Deleted `MonoGameGum/Forms/Controls/ControlsCompatability.cs` (582 lines of `[Obsolete(error: true)]` shims). Also removed the stale `<Compile Remove>` entry from `Runtimes/RaylibGum/RaylibGum.csproj` and cleaned up dead `using MonoGameGum.Forms.Controls` in `MonoGameGum/Input/GamePad.cs` and `Keyboard.cs`. All builds and tests pass.

## ~~Task: Fix MenuItemTests~~ (Done)

`MenuItemTests` now inherits `BaseTestClass` and uses `(_, _)` syntax. All 84 tests pass.

## ~~Task: Mark parameterless AddToManagers() as Obsolete~~ (Done)

Marked the parameterless `AddToManagers()` as `[Obsolete]` across all non-FRB runtime classes, pointing users to the `AddToRoot()` extension method instead. FlatRedBall keeps `AddToManagers()` permanently (guarded with `#if !FRB`).

**What was changed:**

- `GumRuntime/GraphicalUiElement.cs` — base `AddToManagers()` gets `[Obsolete]` inside `#if !FRB`, updated XML docs on both overloads
- `MonoGameGum/GumService.cs` — added XML docs to `AddToRoot(GraphicalUiElement)`, `AddToRoot(FrameworkElement)`, and `RemoveFromRoot`
- 7 MonoGameGum runtime classes (`ContainerRuntime`, `SpriteRuntime`, `NineSliceRuntime`, `ColoredRectangleRuntime`, `CircleRuntime`, `RectangleRuntime`, `PolygonRuntime`) — added `[Obsolete]` + `/// <inheritdoc>` to `AddToManagers()`
- `MonoGameGum/GueDeriving/TextRuntime.cs` and `Runtimes/SkiaGum/GueDeriving/TextRuntime.cs` — replaced `// We should phase this out` comment with proper `[Obsolete]` + `/// <inheritdoc>`
- `MonoGameGum/Forms/Controls/FrameworkElement.cs` — updated XML doc example from `button.Visual.AddToManagers()` to `button.AddToRoot()`
- `MonoGameGum.Tests/Forms/FrameworkElementTests.cs` — migrated test from `AddToManagers()` to `AddToRoot()`

**What users should do:**

- **If you use `AddToManagers()`** — replace with `myElement.AddToRoot()` (for visuals) or `myFormsControl.AddToRoot()` (for Forms controls). This adds the element to the `GumService.Default.Root` container.
- **If you run multiple Gum instances** (e.g. SkiaGum with multiple canvases) — use the two-parameter `AddToManagers(ISystemManagers, Layer?)` overload directly. This is not obsolete and remains the correct approach for multi-instance scenarios.
- **FlatRedBall users** — no change. `AddToManagers()` is not marked obsolete in FRB builds.

## ~~Task: Migrate SkiaGum runtimes from GraphicalUiElement to InteractiveGue~~ (Done)

Changed the base class of all SkiaGum runtimes from `GraphicalUiElement` to `InteractiveGue` to match MonoGame/Raylib and unblock future Forms/input support in SkiaGum.

**What was changed (8 classes):**

- `TextRuntime`, `ContainerRuntime`, `SpriteRuntime`, `ColoredRectangleRuntime`, `SolidRectangleRuntime`, `SvgRuntime`, `LottieAnimationRuntime` — changed from `: GraphicalUiElement` to `: InteractiveGue`
- `SkiaShapeRuntime` — changed from `: GraphicalUiElement` to `: InteractiveGue` (this transitively covers ArcRuntime, CircleRuntime, ColoredCircleRuntime, LineGridRuntime, LineRuntime, PolygonRuntime, RoundedRectangleRuntime)

**Why this is safe:** `InteractiveGue` adds events and properties (Click, Push, RollOn, etc.) but they are all inert unless an input pipeline drives them. SkiaGum currently has no `ICursor` implementation or `DoUiActivityRecursively` call, so the events simply sit unused. No behavior change.

**What this enables:** Future work to add an input pipeline to SkiaGum (ICursor implementation, FormsUtilities integration) will be able to use Forms controls without needing to change the base class of every runtime again.

## References

- Full refactoring plan: `docs/contributing/runtime-refactoring.md`
- Syntax versions doc: `docs/gum-tool/upgrading/syntax-versions.md`
- Phase 1 infrastructure (attribute, detection service, analyzer project, UI display) was built in this session and is on the current branch
