# Shared pieces for headless tab harnesses

Each tab harness (Animations, Variables grid, and later the tree view and canvas selection) hosts
one tool view in a headless window and drives it with real input. These are the parts that do not
depend on the tab.

| File | Role |
|---|---|
| `HeadlessWindowDriver.cs` | The window: clicks, right-click menus, drags, keys, typing, pixel reads, `SaveFrame`. Fails fast when the window hit-tests nothing (see `Animations/README.md`, "Gotchas"). |
| `ToolProjectFixture.cs` | A new project in a temp folder (`.gumx` or `.gumj`, optionally with a shared per-user folder), built through the tool's own commands (`AddComponent`, `AddInstance`, `AddCategory`, `AddState`), with every dialog answered by `Dialogs`. The editor tab and Texture Coordinates plugins sit out meanwhile: they need canvases the headless run never builds. Dispose restores the tool, including the plugin set, so a harness that swaps plugins in need not put them back itself. |
| `ScriptedDialogService.cs` | Answers dialogs from a queue, file pickers included (`AnswerNextOpenFile`, `AnswerNextSaveFile`); an unanswered dialog fails the test instead of hanging. |
| `SwitchableDialogService.cs` | The test container's `IDialogService`. `ToolProjectFixture` points it at its scripted dialogs, so services built once for the whole run (grid manager, delete service) open scripted dialogs too. |
| `RecordingClipboardService.cs` | The test container's `IClipboardService`. The headless app has no main window to copy to, so it also keeps the last text copied (`LastText`). |
| `RecordingFileSystemRevealService.cs` | The test container's `IFileSystemRevealService`. View in explorer, Open Settings Folder and the Help links are recorded (`Requests`) instead of starting a file manager or browser. |

## PR screenshots

A tool UI PR gets a before/after table from screenshot tests. `PrScreenshot.cs` shows a dialog view
model in the head's `DialogWindow` (`ShowDialog`) or any control in a plain window (`Show`), with an
optional light or dark theme; the returned `ScreenshotWindow` pumps frames, finds and clicks
controls, hovers one for its tooltip (`HoverForToolTip`, a real-time wait), and saves a PNG.
`PrScreenshot.SaveWindow` captures a window another harness owns, such as `CanvasHarness.Input.Window`.
`ManagePluginsScreenshotTests` is the example:

```csharp
[Trait("Category", PrScreenshot.Category)]
public class ManagePluginsScreenshotTests
{
    [SkippableFact]
    public void ManagePlugins() => PrScreenshot.Run(() =>
    {
        using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel, ThemeVariant.Dark);
        window.Save("manage-plugins-dark");
    });
}
```

`PrScreenshot.Run` skips the test unless `GUM_SCREENSHOT_DIR` names an output folder, so CI and
`Tools/verify.ps1` report it as skipped. A save fails when the window shows the user's profile
folder or user name in a path; feed the view made-up paths. Then:

```
pwsh Tools/pr-screenshots.ps1 -Pr <num> -Filter ManagePluginsScreenshotTests [-DryRun]
```

It renders the tests here (after) and in a reused sibling worktree at `origin/main` (before, `-Base`
to change, `-NoBefore` for new UI), copying the screenshot tests into it, so they must compile
against the base. It uploads the PNGs to the `pr-screenshots` branch under `pr-<num>/` and writes
the table into the PR body between `pr-screenshots` markers, replacing it on a rerun. `-DryRun`
leaves the PNGs and the proposed body in `%TEMP%\gum-pr-screenshots\pr-<num>\` and changes nothing
on GitHub.

## App-wide window wiring

A test window does not get the main window's app-wide hotkeys (Ctrl+Z, Ctrl+Plus) or its UI font
size on its own. Give it the main window's own wiring: `AppWideWindowInput.RouteHotkeys` and
`AppWideWindowInput.FollowBaseFontSize` (dispose the latter). `UiFontSizeEndToEndTests` shows both.

The harnesses built on these: `../Animations/AnimationEditorHarness.cs` (hosts its own plugin
instance), `../VariableGrid/VariableGridHarness.cs` (hosts the head's singleton tab) and
`../TextureCoordinates/TextureCoordinateTabHarness.cs` (the head's Texture Coordinates tab on a
graphics device, beside `CanvasHarness`, so selecting in the tree gives it the real visual).

## Plugins between tests

`PluginManager` disables a plugin that throws for the rest of the process. `../PluginFailureGuardAttribute.cs`
runs around every test in the assembly: it fails the test that disabled a guarded plugin, then
re-enables every plugin the test disabled (`PluginContainer.RestoreEnabled`, which keeps its
started state) and restores `PluginManager.Plugins` if the test left it changed. Each test starts with
the same plugins enabled, whatever ran before it.

## State that outlives a test

Head singletons (tabs, menus, the tree panel) live for the whole test process. A harness that wraps
them restores what a test can change when it is disposed (`ProjectTreeHarness` restores tab
visibility), rather than each test cleaning up in its own `finally`. The Avalonia application
itself is shared too (one per assembly), so a window, theme variant or focus a test changes stays
changed for the next test unless its harness puts it back. When an end-to-end test passes alone
but fails in a full run, it is usually state an earlier test left behind.

## Hosting a singleton tab's view

A tab whose view the head builds once (the Variables tab, the Project tree) outlives the test.
`HeadlessWindowDriver` takes it out of whatever still holds it (the tab of a main window an earlier
test showed and closed) before hosting it, and detaches it again on dispose, so the next window can
host it. The shared application keeps each control's template across windows.

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
| `ProjectTreeHarness.cs` | The head's own Project tab in a window with the main window's app-wide hotkeys, over a new project saved to disk. Row clicks, the tree's right-click menu (`PickMenu("Add object to Button", "Sprite")`), keys, Ctrl+Z and Ctrl+Y. A gesture that crashed fails at once. Its `Grid` is the Variables tab over the same project, in a second window (`../VariableGrid/VariableGridHarness.cs`). |
| `StatesTabHarness.cs` | The head's States tab in its own window over the same project (`tree.States`): row clicks and right-click menus (`PickMenu("Move to category", "Size")`), the "+" buttons, keys, and the edited and behavior markers. |
| `ProjectOracles.cs` | The checks any scenario runs at its end: what the tool auto-saved equals Save All; the saved project passes the checks `gumcli check` runs; the tree shows exactly the saved folders, elements and instances; reopening reports no errors and re-saving changes no byte. |
| `ProjectFileSnapshot.cs` | Every project file's bytes, for "undo back to the start restores the files". A mismatch names each file and its first differing line, and copies both versions to `%TEMP%\GumEndToEnd\diffs`. |
| `ToolExceptionWatch.cs` | Output-tab errors, exceptions the head's UI-thread hook would write to the crash log, and plugins disabled after throwing. |
| `ProjectOracleTests.cs` | Each oracle fails on the damage it exists to catch. |
| `CanvasHarness.cs` | The head's Editor tab (toolbar, canvas, scroll bars) on a real graphics device, next to a `ProjectTreeHarness` over the same project, whose oracles it ends with. Pointer, key, wheel and drop input in window coordinates (`WindowPointOf(worldX, worldY)`), a drawn frame after every event, and `SavedValue` to read what reached disk. |
| `CanvasScenarioTests.cs` | The Editor canvas: selection, move, resize, rotate, nudge, polygon points, camera, rulers, drops. |
| `TreeScenarioTests.cs`, `TreeNavigationScenarioTests.cs`, `EditMenuScenarioTests.cs`, `VariableScenarioTests.cs`, `StateScenarioTests.cs`, `CopyPasteRenameScenarioTests.cs` | The scenarios: the Project tree (menus, keys, search; selecting, expanding, icons, file menus, importing); the main menu's Edit menu; the Variables tab, states and edits that cascade into other elements; the States tab; copy, paste, rename and delete where they meet references, parents, states and animations. |
| `AnimationScenarioTests.cs` | The Animations tab on `../Animations/AnimationEditorHarness.cs`: `StartScenario()` after setup (saves, routes Ctrl+Z/Ctrl+Y, starts the exception watch, returns the start snapshot), `Undo`/`Redo`, and `AssertOracles()`, which also checks the saved sidecar holds exactly what the tab shows, before and after the reload. |
| `DialogScenarioTests.cs` | Dialogs reached from the main menu (`ProjectTreeHarness.PickMainMenu("File", "New Project")`) and the Project tree: New Project, Load Project and Load Recent, Import Components, Theming, Manage Plugins, Project Properties. An async menu action is followed by `WaitUntil`. |
| `CodeTabHarness.cs`, `CodeGenScenarioTests.cs` | The head's Code tab in its own window beside a `ProjectTreeHarness`: settings rows, Generate, and the generated files read back from disk. |
| `HeadCommandLineScenarioTests.cs` | The head's command line without a window: `HeadOptions` and `CommandLineManager` parse a launch line, and the test hands what they read to the services startup uses. gumcli's process-level scenarios are in `Tests/Gum.Cli.Tests/EndToEnd/`. |
| `DisplayPropertiesScenarioTests.cs` | The grid's editors on real rows (slider, angle, file, list, toggles, corner radius, remove button), the Project Properties tab hosted in its own window and edited through its grid, and the Standards palette's chips (menu, drop on a tree row, drop on the canvas). A guide that changes rendering is checked with pixel reads of the canvas window. |
| `TabViewScenarioTests.cs` | The other tabs (Output, Errors, History, Alignment, Behaviors, Hotkeys, File Watch, Performance) and the View menu. A tab's view is the head's singleton. Theme, font size, renderer options and the standards palette outlive the test, so a scenario puts them back in a `finally`. The File Watch tab lists folders only after a project load (`SaveAndReload`). |

A scenario builds its project with the fixture, clicks the starting node, takes a snapshot, does
the gesture with its dialogs queued, asserts the model and the tree, undoes back and compares the
snapshot, redoes, then calls `AssertOracles()`. Undo history is per element, so a cross-element
change is undone in each element. Adding or deleting a whole element records no undo; assert that
Ctrl+Z leaves the files alone instead. A Variables-tab scenario selects through the tree (`tree.Click`), edits
through `tree.Grid`, and undoes with `tree.Undo()`.

`ToolProjectFixture.AddCategory` and `AddState` record undo as the tool's dialogs do. Setup that
edits an element some other way without an undo lock leaves the element's undo baseline behind, and
the scenario's first Ctrl+Z then undoes the setup too.

Gotchas in scenario setup:

- The Animations tab reads an element's `.ganx` when the element is selected. A scenario that
  writes a sidecar after adding the element (which selects it) selects something else and back
  before the gesture, or the tab keeps its empty copy and writes that over the file.
- The delete dialog remembers the last "delete children" choice for the session. A scenario that
  deletes a parent instance sets both options itself.
- The Standard folder is hidden while the Standards palette is on (the default), so a standard
  element has no tree row to right-click.
- The search box and "Include Variables" belong to the head's panel, which outlives the test; the
  harness clears both on dispose.
- An Animations scenario builds its elements in memory and selects one, so `StartScenario()` must
  come after setup: it saves everything, or the "auto-saved equals Save All" oracle fails on setup.
  Setup edits in the tab (adding the animations to edit) are in the undo history too; undo only as
  many steps as the scenario's own gestures.
- A scripted Import .gumx answer must wait for the dialog's posted dependency recompute before it
  returns true; answering at once imports without the dependencies.
- Add Forms and Import .gumx end by reloading the project, so read it from `IProjectManager`
  afterwards; `tree.Project.Project` is the stale pre-reload copy.
- The Code Output plugin reads `ProjectCodeSettings.codsj` only when a project loads, and
  `ToolProjectFixture` loads its project before the project has a folder. A scenario that needs
  code settings or a `.csproj` in place passes them to `CodeTabHarness`'s `beforeLoad`, which
  writes them and then reopens the project.
- A nullable number field (the single pixel texture bounds) is disabled while its value is null;
  click its "Is Null" check box before typing, or the typed Enter lands on whatever kept focus.
- The editor tab, which sits out, fills a project's canvas sizes when the tool opens it; a scenario
  that makes a new project through the tool sets `CustomCanvasSizes` itself before the oracles.

### Canvas scenarios

The canvas needs a display and a GL driver, so its scenarios are `[SkippableFact]`s that run on
the Avalonia UI thread through `CanvasHarness.OnUiThread` (an `[AvaloniaFact]` cannot skip). Off CI
they run wherever there is a display, except macOS (a plain `dotnet test` runs them on Windows). On
CI they run only in the Linux Xvfb step, which sets `GUM_RUN_CANVAS_DEVICE_TESTS=1` and names the
class in its filter.

- The canvas polls its input once per frame, and `CanvasHarness` draws one after each event. The
  editor tab plugin, its canvas and the shared device are built once and live for the rest of the
  process, as in the tool; the fixture lets the plugin back in for the harness's lifetime. The
  Texture Coordinates plugin builds its tab at the same moment and sits out and comes back with it.
- The Texture Coordinates tab's view model (zoom, snap to grid, exposed source) is a singleton.
  `TextureCoordinateTabHarness` resets the zoom; snap to grid follows each new project's settings.
- A drag is ignored until it is more than 6 pixels from the press, and movement before that is
  lost (#5257). `Drag` moves in 10 to 20 pixel steps so nothing is lost; pass `steps` to test
  small moves.
- Shift on the press adds to the selection instead of moving (#5258): for an axis-locked move,
  `PressButton`, `DragTo`, then `HoldKey` Shift and `DragTo` again.
- A drop onto the canvas is the platform's drag events with the payload the source would build
  (`DropOnCanvas`); headless Avalonia has no drag source. A drop parents the new instance only to
  the selected instance under it.
- Undo replaces an element's instances with copies; after an undo, find instances again by name.

`pwsh Tools/e2e-coverage.ps1` lists the inventory IDs no non-skipped test tags, per area.
