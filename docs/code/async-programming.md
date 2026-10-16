# async Programming

## Introduction

UI events may interact with systems that are async (return `Task`). MonoGame projects have no default synchronization context, so without one your `await` continuations are not guaranteed to resume on the primary thread — touching UI state from the wrong thread can crash or render mid-update.

Gum ships with `Gum.Async.SingleThreadSynchronizationContext` to keep all continuations on the primary thread, and exposes a single-line opt-in.

## Why a Synchronization Context Is Needed

```csharp
// Click handler on a button:
async void HandleButtonClicked(object sender, EventArgs args)
{
    AnnounceButtonClicked();
    await Task.Delay(1000);
    // ----- Without a sync context, this line may run on a different thread.
    button.IsEnabled = false;
}
```

Without a sync context, the line after the `await` can land on a thread other than the one running `Game.Update` / `Game.Draw`. Mutating UI from there is unsafe. Installing a single-threaded sync context routes every continuation back to the primary thread.

## One-Line Opt-In (Recommended)

Call `UseSingleThreadedAsync` once, after `Initialize`:

```csharp
protected override void Initialize()
{
    GumService.Default.Initialize(this);
    GumService.Default.UseSingleThreadedAsync(); // ← this is all you need
    base.Initialize();
}
```

`GumService.Update` (which you already call each frame) drains queued continuations, so there is no per-frame plumbing. Calling `UseSingleThreadedAsync` more than once is a no-op.

After this, `await` lines in event handlers — including `await dialogBox.ShowAsync(...)` (see [DialogBox](controls/dialogbox.md)) — are safe to follow with UI mutations.

### When _Not_ to Call It

Skip `UseSingleThreadedAsync` if you've already installed your own `SynchronizationContext`. Installing two would route continuations through the wrong queue. The one-liner is opt-in for exactly this reason.

## More Than One GumService

Call `UseSingleThreadedAsync` on **each** service. `GumService.Update` and `GumService.Draw` make a service's own context the current `SynchronizationContext` for the length of the call and then put the previous one back, so an `await` inside a handler fired from that service's `Update` resumes in that service's next `Update`, with that service as `GumService.Default`. Without the swap, the service whose `UseSingleThreadedAsync` ran last would capture every `await`, and the continuation would run inside the wrong service's `Update`.

A service that did not call `UseSingleThreadedAsync` (for example because you installed your own `SynchronizationContext`) does not get its context switched. Continuations that resume through your own context run under whichever service is the default at that moment, so reassign `GumService.Default` after the `await` if you build or read UI there. Do not call `UseSingleThreadedAsync` from `Initialize` for the other services; it stays opt-in.

## Manual Setup (Advanced / Pre-existing Projects)

If you copied `SingleThreadSynchronizationContext` into your own project from an older version of this guide and pump `Update()` yourself, that still works — do **not** also call `UseSingleThreadedAsync`. The two installs would conflict.

If you want to take ownership of the context for advanced reasons (custom queueing, screen-change `Clear()` calls, etc.), you can access the installed instance via `GumService.Default.SynchronizationContext` after calling `UseSingleThreadedAsync`.

## Platform Coverage

`UseSingleThreadedAsync` is available on MonoGame, KNI, FNA, Raylib, Silk.NET, and Sokol. Under FlatRedBall, the FRB runtime supplies its own thread/instruction model; this opt-in is intentionally not exposed there — use FRB's own facilities (e.g. `InstructionManager`) for primary-thread work.
