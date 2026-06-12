---
name: gum-visual-events
description: Gum runtime cursor-event dispatch — how Click/Push/RollOver/etc. are raised and routed. Triggers: InteractiveGue, DoUiActivityRecursively, RoutedEventArgs, HandledActions, ClickPreview, RollOverBubbling, ClickBubbling, HasEvents, ExposeChildrenEvents, adding/bubbling a visual event.
---

# Gum Visual Event Dispatch

Internals of how cursor events are raised on visuals. For the user-facing event list and "what fires when" tables, see [visual-events.md](../../../docs/code/events-and-interactivity/visual-events.md) — don't duplicate it here.

## Where it lives

| File | Role |
| --- | --- |
| `GumRuntime/InteractiveGue.cs` | All of it: event declarations, `RoutedEventArgs`/`InputEventArgs`, the `HandledActions` class, and `DoUiActivityRecursively` (the dispatch engine). |

Only types deriving from `InteractiveGue` raise events, and only when `HasEvents == true`. Forms controls layer their own events on top of these visual events (out of scope here).

## The one method that matters: `DoUiActivityRecursively`

A single recursive walk over the visual tree drives every cursor event. It descends from the root, recurses into the deepest child under the cursor first, then unwinds. Understanding the descend-then-unwind shape is the whole game.

## Three dispatch disciplines coexist (this is the non-obvious part)

The event *names* half-hide that three different routing models live side by side:

| Discipline | Order | Suppression | Events |
| --- | --- | --- | --- |
| **Tunneling** | parent → child (on the way *down*) | child skipped if a parent set `Handled` | `ClickPreview`, `PushPreview` |
| **Bubbling** | deepest child → parent (on the way *up*) | parent skipped if a descendant set `Handled` | `RollOverBubbling`, `MouseWheelScroll`, `ClickBubbling` |
| **Single-target** | fires on exactly one element | n/a — no routing | `Click`, `Push`, `DoubleClick`, `RightClick`, and most others |

These mirror WPF's Preview (tunneling) / bubbling routed-event split — same idea, same naming convention.

## Gotchas

- **Bubbling is NOT done by walking parent pointers.** It's a single shared `HandledActions` instance threaded through the recursion, combined with `RoutedEventArgs.Handled`. A handler sets `args.Handled = true`; the dispatcher copies that into a `HandledActions` flag (e.g. `HandledRollOver`), and ancestors check the flag before raising. That `HandledActions` + `Handled` pair *is* the routing machinery — reuse it to add routed events.
- **`handledByChild` is the opposite of bubbling.** Once a child "handles" (returns true), the parent's entire click/push block is skipped (`if (!handledByChild)`). That short-circuit is why `Click` is single-target: a parent container does **not** get `Click` when a child button is clicked. Bubbling events live in a separate block that runs regardless of `handledByChild`, keyed off their own `HandledActions` flag — so they and the single-target events are independent channels.
- **To make an event bubble without breaking existing behavior, add a parallel `*Bubbling` event** rather than changing the single-target one. Precedent: `RollOverBubbling` alongside `RollOver`, and `ClickBubbling` alongside `Click`. Give it its **own** `HandledActions` flag so its `Handled` only suppresses itself on ancestors and never touches single-target `Click`/`Push`.
- **A click has a push origin; rollover doesn't.** Single-target `Click` only fires where `cursor.WindowPushed == asInteractive` — you must push *and* release on the same element. So a bubbling click can't copy the `RollOverBubbling` block (which fires on anything merely under the cursor). `ClickBubbling` solves this with a `HandledActions.DidClickOccur` flag set only when a real click resolves on its target; the bubbling pass then fires on that element and the ancestors it unwinds through.
- **`VisualOverBehavior`'s effective default is `IfHasEventsIsTrue`, NOT its field initializer.** The static on `ICursor` initializes to `OnlyIfEventsAreNotNullAndHasEventsIsTrue` (legacy), but `FormsUtilities.InitializeDefaults` reassigns it to `IfHasEventsIsTrue` — and every `GumService.Initialize` path plus the test assembly init (`TestAssemblyInitializeBase`) calls `InitializeDefaults`. So in any real app *and* in the tests, an element is "over" purely from `HasEvents == true`. The legacy mode (which gates on a hard-coded list of non-null events in the `shouldTreatAsIsOver` block) is only reachable if `InitializeDefaults` is never called — effectively never. **Don't add new bubbling events to that list; it's dead for normal usage** (`RollOverBubbling` and `ClickBubbling` are deliberately not in it). General lesson: a field initializer is not the effective default — grep for assignments in `Initialize`/`Defaults` methods before claiming a default.
- **Participation is gated.** Beyond `HasEvents`, an element's involvement depends on `ExposeChildrenEvents` and `IsEnabledRecursively` (and, only under the legacy `VisualOverBehavior`, on having a listed event attached).
- **`InputEventArgs.InputDevice` carries the cursor**, not the underlying hardware device — single-target input events (`Click`, `Push`, etc.) pass the `ICursor` through here.
- **FRB does not share `InteractiveGue`.** It's absent from `GumCoreShared.projitems`; FlatRedBall has its own copy. Adding an event here does NOT reach FRB. If a *shared* Forms control (those ARE in FRB via `FlatRedBall.Forms.Shared.projitems`) starts subscribing to a new visual event, FRB's `InteractiveGue` must gain the same member or FRB breaks with `CS1061`.
