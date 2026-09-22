# Dogfooding the animation editor headlessly

This folder is about one thing: the Animations tab of the Avalonia Gum tool (the animation
editor). It drives the real tab with simulated clicks, drags, hovers and key presses inside an
in-process headless window. Nothing reaches the desktop, so it is safe to run while the machine
is in use. Use it to find bugs in the animation editor the way a user would hit them, pin each
one with a test, fix it, and keep the scenario as the regression guard.

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
- Occasionally a test fails at once with "The tab's window hit-tests nothing after a render tick".
  Confirmed cause, not a Gum bug: `Dispatcher.UIThread` in Avalonia's own
  `src/Avalonia.Base/Threading/Dispatcher.cs` is `s_uiThread ??= CreateUIThreadDispatcher()` with no
  lock; the headless session nulls it before each isolated test's app setup and recreates it lazily,
  so something touching `Dispatcher.UIThread` from another thread in that window (suspected: a GC
  finalizer, unconfirmed) can create a second instance and orphan the media context/render timer on
  the loser. Still present through Avalonia 11.3.22 (checked every 11.3.x changelog since 17); a
  11.3→12 upgrade is not the fix — 12.1.1 has an open, worse version of the same class of bug that
  poisons the whole test process at a measured 3–5% rate (AvaloniaUI/Avalonia#22021). The poisoning
  is scoped to the whole test's isolated dispatcher session, not to one Window: recreating just the
  Window in place (closing it and building a fresh one) still failed 3/3 in local repro, confirmed by
  running the suite until it reproduced locally. Only a fresh process gets a genuinely independent
  roll, since `s_uiThread` resets on process start.

  This assembly has ~90 tests that each independently create a harness (and so each independently
  roll the dice on this race), so a *whole-assembly* retry does not scale: the per-run failure
  probability compounds well above the per-test rate as tests are added, and every retry re-pays the
  full ~90s run cost even though usually at most one test actually flaked. CI instead parses the
  `.trx` on failure for the exact test(s) that failed and re-runs only that filtered set as a fresh
  process (`build-and-test.yaml`, the `Gum.Avalonia.Tests (headless)` step) — a couple of seconds,
  not another ~90s — up to 4 total attempts; a genuine failure reproduces on every attempt and still
  fails the step. Both cost and odds now scale with the number of tests that actually flake (usually
  0 or 1), not with the assembly's size. Locally, just rerun the test. The harness checks for the
  race right after the window opens so it fails in milliseconds with that message rather than as a
  click that "did not land". The tool's file watch plugin used to add its own thread-pool timer to
  the mix; it now runs that timer
  only while its tab is shown. Do not add thread-pool work that reaches into Avalonia to the harness
  or to a plugin's StartUp.
