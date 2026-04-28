# DialogBox

## Introduction

`DialogBox` displays paged text with optional letter-by-letter ("typewriter")
reveal. It targets game-style dialog: text appears at a configurable rate, the
player advances through pages by pressing a confirm input or clicking, and long
text auto-paginates to fit the visual.

`DialogBox` lives in `Gum.Forms.Controls.Games` (and `FlatRedBall.Forms.Controls.Games`
under FRB).

## Basic Usage

```csharp
using Gum.Forms.Controls.Games;

DialogBox dialogBox;

void Initialize()
{
    dialogBox = new DialogBox();
    dialogBox.LettersPerSecond = 30;
    dialogBox.Visual.X = 100;
    dialogBox.Visual.Y = 400;
}

void OnSomeTrigger()
{
    dialogBox.Show("Welcome, traveler! Press Enter to continue.");
    dialogBox.IsFocused = true;
}
```

`Show(string)` runs the text through `ConvertToPages` and auto-splits based on
the visual's height. Calling `Show` while a previous dialog is still running
restarts cleanly — pages are cleared, in-flight typewriter is stopped.

## Multiple Pages

You can pass an array to specify explicit page breaks. Each entry is treated as
a *minimum* page boundary; if any entry overflows the visual it is split
further automatically.

```csharp
dialogBox.Show(new[]
{
    "First page of text.",
    "Second page — possibly very long, will auto-split if it doesn't fit.",
    "Final page."
});
dialogBox.IsFocused = true;
```

## Typewriter Speed

`LettersPerSecond` controls the reveal rate. Set to `0` (or null/negative) to
print the page instantly.

```csharp
dialogBox.LettersPerSecond = 60; // 60 chars per second
dialogBox.LettersPerSecond = 0;  // immediate
```

The typewriter advances regardless of focus — multiple `DialogBox` instances
can type simultaneously (e.g. one NPC speaking while the player can move).

## Advancing Pages

When the dialog has focus, the registered click combo (Enter by default)
advances:

* If the current page is still typing → it skips to the end of the page (full
  text shown, continue indicator appears).
* If the current page is fully typed → advances to the next page.
* If no pages remain → dismisses the dialog.

Mouse click on the visual does the same. To customize the advance trigger,
assign `AdvancePageInputPredicate`:

```csharp
dialogBox.AdvancePageInputPredicate = () =>
    GumService.Default.Keyboard.KeyPushed(Keys.Z);
```

## Async Usage

`ShowAsync` returns a `Task` that completes once all pages have been displayed
and the dialog is dismissed:

```csharp
async Task RunDialog()
{
    await dialogBox.ShowAsync("Greetings.");
    // Safe to mutate UI here — but only if a synchronization context is installed.
    nextStep.IsVisible = true;
}
```

> **Important:** `await dialogBox.ShowAsync(...)` resumes on whichever thread the
> async runtime picks unless you've installed a primary-thread synchronization
> context. Without one, the line after the `await` may run off the primary
> thread and crash when it touches UI state.
>
> The one-line opt-in is:
>
> ```csharp
> GumService.Default.UseSingleThreadedAsync();
> ```
>
> Call once after `Initialize`. See [async Programming](../async-programming.md)
> for the full explanation and platform notes.

## Events

| Event                 | Fires when                                                    |
| --------------------- | ------------------------------------------------------------- |
| `FinishedTypingPage`  | The current page finishes typing (or is skipped).             |
| `PageAdvanced`        | The user advances past a fully-typed page.                    |
| `FinishedShowing`     | The dialog dismisses (last page advanced).                    |

## Default Visual

When you write `new DialogBox()`, Gum constructs the V3 default visual
(`DialogBoxVisual`) with:

* `Background` — bordered nine-slice, fills the control.
* `TextInstance` — `TextRuntime` (top-left, padded, `TruncateLine`).
* `ContinueIndicatorInstance` — small solid square at bottom-right, shown when a
  page finishes typing and the dialog is taking input.

Default size is 600 × 140 (configurable via `Visual.Width`/`Visual.Height`).
`BackgroundColor` and `ForegroundColor` on the visual recolor it without
changing the layout.

## Custom Visual

To supply your own visual (Gum tool component or hand-built `InteractiveGue`),
pass it to the constructor:

```csharp
var customVisual = ...;             // your InteractiveGue
var dialogBox = new DialogBox(customVisual);
```

The custom visual **must** contain children named:

| Name                        | Required | Notes                                                                             |
| --------------------------- | -------- | --------------------------------------------------------------------------------- |
| `TextInstance`              | Yes      | A `TextRuntime`. For pagination, set `HeightUnits` to a fixed mode (not `RelativeToChildren`) and `TextOverflowVerticalMode = TruncateLine`. |
| `ContinueIndicatorInstance` | Optional | Any `GraphicalUiElement`. DialogBox toggles its `Visible` between pages.          |

If `TextInstance` uses `TextOverflowVerticalMode = SpillOver` (the default),
text will not paginate — it will spill past the visible area. Set it to
`TruncateLine` so `ConvertToPages` can detect the line cap.

## See Also

* [async Programming](../async-programming.md) — required reading before using
  `ShowAsync`.
* [FrameworkElement](frameworkelement/README.md) — base class for all Forms
  controls.
