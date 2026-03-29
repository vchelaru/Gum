# April 2026 Refactoring Tasks

## ~~Task: Remove ControlsCompatability.cs~~ (Done)

Deleted `MonoGameGum/Forms/Controls/ControlsCompatability.cs` (582 lines of `[Obsolete(error: true)]` shims). Also removed the stale `<Compile Remove>` entry from `Runtimes/RaylibGum/RaylibGum.csproj` and cleaned up dead `using MonoGameGum.Forms.Controls` in `MonoGameGum/Input/GamePad.cs` and `Keyboard.cs`. All builds and tests pass.

## ~~Task: Fix MenuItemTests~~ (Done)

`MenuItemTests` now inherits `BaseTestClass` and uses `(_, _)` syntax. All 84 tests pass.

## References

- Full refactoring plan: `docs/contributing/runtime-refactoring.md`
- Syntax versions doc: `docs/gum-tool/upgrading/syntax-versions.md`
- Phase 1 infrastructure (attribute, detection service, analyzer project, UI display) was built in this session and is on the current branch
