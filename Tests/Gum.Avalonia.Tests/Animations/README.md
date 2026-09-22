# Dogfooding the Animations tab headlessly

This folder drives the real Avalonia Animations tab with simulated clicks, drags, hovers and key
presses, inside an in-process headless window. Nothing reaches the desktop, so it is safe to run
while the machine is in use. Use it to find bugs in the animation editor the way a user would hit
them, pin each one with a test, fix it, and keep the scenario as the regression guard.

## Run it

```
dotnet test Tests/Gum.Avalonia.Tests --filter "FullyQualifiedName~Gum.Avalonia.Tests.Animations"
```

About 85 scenarios, roughly a minute. To keep PNGs of what the tab drew, set `GUM_HEADLESS_FRAMES`
to a folder before running; scenarios that call `SaveFrame` write there (default:
`%TEMP%\GumAnimationEditor\frames`). Read the PNGs to check rendering by eye.

## The pieces

| File | Role |
|---|---|
| `AnimationEditorHarness.cs` | Hosts the tab (its own plugin instance on the head's real service graph, over a temp project) in a headless window. Input, lookups, pixel reads, sidecar reads. |
| `ScriptedDialogService.cs` | Answers the dialogs the tab opens. An unanswered dialog fails the test instead of hanging. |
| `TestAnimationPlugin.cs` | The head's plugin with manual playback timers the harness fires, so playback does not depend on dispatcher timers. |
| `*Tests.cs` | One file per area: editor basics, animation list, reload, timeline interaction, instance sub-animations, element lifecycle, playback, keyframe editing, errors, tab lifecycle, JSON projects, detail column, external changes. |

## Write a scenario

Every scenario follows the same shape:

```csharp
[AvaloniaFact]
public void PressingDelete_RemovesTheKeyframe_OnceConfirmed()
{
    using AnimationEditorHarness editor = new AnimationEditorHarness();
    ComponentSave button = editor.AddComponent("Button", "Looks", "Pressed", "Released");
    editor.Select(button);
    editor.AddAnimation("Walk");
    AnimatedKeyframeViewModel keyframe = editor.AddStateKeyframe("Looks/Pressed");
    editor.Click(editor.RowFor(editor.KeyframeList, keyframe));

    editor.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
    editor.Press(Key.Delete, PhysicalKey.Delete);

    editor.ViewModel.SelectedAnimation!.Keyframes.ShouldBeEmpty();
    editor.ReadSavedAnimations(button).ShouldNotBeNull().Animations.Single().States.ShouldBeEmpty();
}
```

Rules that keep scenarios honest:

- Drive the gesture the user would use, not the view model: `Click`, `ClickAt`, `Drag`, `Hover`,
  `Press`, `TypeAndEnter`, `PickAddKeyframe`. `KeyframeMarkerCenter` gives the point of a marker on
  the timeline for a click or hover.
- Queue every dialog answer before the gesture that opens it: `Dialogs.AnswerNext<T>`,
  `AnswerNextMessage`, `AnswerNextUserString`.
- Assert on three things where they apply: the view model (`ViewModel`), the controls
  (`AnimationList`, `KeyframeList`, `Timeline.Rows`, `DetailCombos`, `Scrubber`), and the saved
  sidecar (`ReadSavedAnimations`). A bug often shows in only one of them.
- For tool events the tab reacts to (rename, delete, undo, file changes), call the same entry the
  tool calls: `PluginManager.ElementRename`, `IDeleteLogic.Remove` under `UndoManager.RequestLock`,
  `UndoManager.PerformUndo`, `Plugin.CallReactToFileChanged`.
- Anything that needs real time (playback) goes through `Wait` or `WaitUntil`, never `await`.
- Call `ThrowIfPluginFailed` after an event that could throw inside the plugin: the plugin manager
  swallows the exception and disables the plugin, which otherwise looks like a silent no-op.
- To check what was drawn, use `AnyPixelNear` with a color (solid fills) or a predicate (thin
  antialiased lines, match by hue). `SaveFrame` first so a failure leaves a picture.

## Fix loop

1. Add the scenario for a gesture or situation nobody has tried. Run it.
2. When it fails, decide which layer owns the bug and write a red unit test there first: view models
   and the controller in `Tests/Gum.Presentation.Tests`, timeline math in
   `TimelineLayoutTests`, the runtime in `MonoGameGum.Tests/Runtimes/AnimationRuntimeTests`, error
   reporting in `Tests/Gum.ProjectServices.Tests`, view wiring stays covered by the scenario.
3. Fix, run the unit test, run the scenario, then run this folder and the neighbouring tab tests
   (`CodeOutputTabTests`, `StandardsPaletteAddTests`) to catch leaked state.
4. Commit the scenario, the unit test and the fix together.

## Gotchas

- A plugin only receives tool events when it is in `PluginManager.PluginContainers` as well as
  `Plugins`. The harness registers itself in both and swaps the head's own instance out while it
  runs, so nothing handles an event twice.
- The harness resets the tool's project and the `FileManager.UserApplicationDataFolderOverride` on
  dispose; a test that adds shared state of its own must clear it the same way.
- Keep `[AvaloniaFact]` tests synchronous. An `async Task` one needs a nested dispatcher frame that
  the headless session sometimes refuses.
- The first click after a window opens occasionally does not land in a long run; `AddAnimation`
  retries once. Copy that pattern if another first gesture proves flaky.
- The same approach works for any other tab: build the plugin with `ActivatorUtilities`, set its
  `[Import]` properties (a scripted `DialogService`), `StartUp()`, register it with the plugin
  manager, and host its tab content in a test window. See the `gum-unit-tests` skill.
