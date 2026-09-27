# Shared pieces for headless tab harnesses

Each tab harness (Animations, Variables grid, and later the tree view and canvas selection) hosts
one tool view in a headless window and drives it with real input. These are the parts that do not
depend on the tab.

| File | Role |
|---|---|
| `HeadlessWindowDriver.cs` | The window: clicks, right-click menus, drags, keys, typing, pixel reads, `SaveFrame`. Fails fast on the headless compositor race (see `Animations/README.md`, "Gotchas"). |
| `ToolProjectFixture.cs` | A new project in a temp folder (`.gumx` or `.gumj`, optionally with a shared per-user folder), built through the tool's own commands (`AddComponent`, `AddInstance`, `AddCategory`, `AddState`), with every dialog answered by `Dialogs`. The editor tab plugin sits out meanwhile: it needs a canvas the headless run never builds. Dispose restores the tool, including the plugin set, so a harness that swaps plugins in need not put them back itself. |
| `ScriptedDialogService.cs` | Answers dialogs from a queue; an unanswered dialog fails the test instead of hanging. |
| `SwitchableDialogService.cs` | The test container's `IDialogService`. `ToolProjectFixture` points it at its scripted dialogs, so services built once for the whole run (grid manager, delete service) open scripted dialogs too. |

A test window does not get the main window's app-wide hotkeys (Ctrl+Z, Ctrl+Plus) or its UI font
size on its own. Give it the main window's own wiring: `AppWideWindowInput.RouteHotkeys` and
`AppWideWindowInput.FollowBaseFontSize` (dispose the latter). `UiFontSizeEndToEndTests` shows both.

The harnesses built on these: `../Animations/AnimationEditorHarness.cs` (hosts its own plugin
instance) and `../VariableGrid/VariableGridHarness.cs` (hosts the head's singleton tab).

## Plugins between tests

`PluginManager` disables a plugin that throws for the rest of the process. `../PluginFailureGuardAttribute.cs`
runs around every test in the assembly: it fails the test that disabled a guarded plugin, then gives
every plugin the test disabled a fresh, enabled container and restores `PluginManager.Plugins` if the
test left it changed. Each test starts with the same plugins enabled, whatever ran before it.

## Hosting a singleton tab's view

A tab whose view the head builds once (the Variables tab, the Project tree) outlives the test. Pass
`contentOutlivesTest: true` to `HeadlessWindowDriver`. Each test runs in a fresh Avalonia session,
so the next window re-applies every template while the old template presenters still hold the
view's content; the driver releases them before hosting and again on dispose. Without it the second
test fails with "already has a visual parent".

Use the head's singleton manager and view rather than building a second one: everything the tool
routes to the tab (`IGuiCommands.RefreshVariables`, plugin events) reaches the singleton only.

## End-to-end suite (`../EndToEnd/`)

The regression pass from #5141: scenarios that drive whole tool features with real input, tagged
with the IDs of `Direction/avalonia-migration/functionality-inventory.md`
(`[Trait("Feature", "TREE-043")]`) and `[Trait("Category", "EndToEnd")]`, so a nightly run can
select them and per-PR CI can later leave them out.

```
dotnet test Tests/Gum.Avalonia.Tests --filter "Category=EndToEnd"
```

| File | Role |
|---|---|
| `ProjectTreeHarness.cs` | The head's own Project tab in a window with the main window's app-wide hotkeys, over a project saved and reopened from disk (`ToolProjectFixture.SaveAndReload`). Row clicks, the tree's right-click menu (`PickMenu("Add object to Button", "Sprite")`), keys, Ctrl+Z and Ctrl+Y. A gesture that crashed fails at once. |
| `ProjectOracles.cs` | The checks any scenario runs at its end: what the tool auto-saved equals Save All; the saved project passes the checks `gumcli check` runs; the tree shows exactly the saved folders, elements and instances; reopening reports no errors and re-saving changes no byte. |
| `ProjectFileSnapshot.cs` | Every project file's bytes, for "undo back to the start restores the files". A mismatch names each file and its first differing line, and copies both versions to `%TEMP%\GumEndToEnd\diffs`. |
| `ToolExceptionWatch.cs` | Output-tab errors, exceptions the head's UI-thread hook would write to the crash log, and plugins disabled after throwing. |
| `ProjectOracleTests.cs` | Each oracle fails on the damage it exists to catch. |

A scenario builds its project with the fixture, clicks the starting node, takes a snapshot, does
the gesture with its dialogs queued, asserts the model and the tree, undoes back and compares the
snapshot, redoes, then calls `AssertOracles()`. Undo history is per element, so a cross-element
change is undone in each element. Adding or deleting a whole element records no undo; assert that
Ctrl+Z leaves the files alone instead.
