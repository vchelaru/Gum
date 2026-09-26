# Shared pieces for headless tab harnesses

Each tab harness (Animations, Variables grid, and later the tree view and canvas selection) hosts
one tool view in a headless window and drives it with real input. These are the parts that do not
depend on the tab.

| File | Role |
|---|---|
| `HeadlessWindowDriver.cs` | The window: clicks, right-click menus, drags, keys, typing, pixel reads, `SaveFrame`. Fails fast on the headless compositor race (see `Animations/README.md`, "Gotchas"). |
| `ToolProjectFixture.cs` | A new project in a temp folder, built through the tool's own commands (`AddComponent`, `AddInstance`, `AddCategory`, `AddState`), with every dialog answered by `Dialogs`. The editor tab plugin sits out meanwhile: it needs a canvas the headless run never builds, and its first failure would disable it for every later test. Dispose restores the tool. |
| `ScriptedDialogService.cs` | Answers dialogs from a queue; an unanswered dialog fails the test instead of hanging. |
| `SwitchableDialogService.cs` | The test container's `IDialogService`. `ToolProjectFixture` points it at its scripted dialogs, so services built once for the whole run (grid manager, delete service) open scripted dialogs too. |

## Hosting a singleton tab's view

A tab whose view the head builds once (the Variables tab, the Project tree) outlives the test. Pass
`contentOutlivesTest: true` to `HeadlessWindowDriver`. Each test runs in a fresh Avalonia session,
so the next window re-applies every template while the old template presenters still hold the
view's content; the driver releases them before hosting and again on dispose. Without it the second
test fails with "already has a visual parent".

Use the head's singleton manager and view rather than building a second one: everything the tool
routes to the tab (`IGuiCommands.RefreshVariables`, plugin events) reaches the singleton only.
