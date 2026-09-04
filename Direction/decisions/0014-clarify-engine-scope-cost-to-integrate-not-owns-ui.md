# 0014. Clarify the engine-scope boundary: cost-to-integrate, not "ships its own UI"

- **Status:** Accepted
- **Date:** 2026-09-04
- **Deciders:** Victor Chelaru, Claude

## Context

ADR-0002 excluded full engines "that ship their own UI (Unity, Godot)," reasoning from the fact
that both ship a native UI system. Stride is also a full engine with its own UI system
(Stride.UI) and editor, which reads as in-scope for that same exclusion — but a Stride runtime
request (issue #4600) is cheap: Stride can host an `SKCanvas`, so Gum plugs in the same way it
already does for WPF/MAUI/Silk.NET, via the shared `GumServiceSkiaBase` (#4452/#4459). Multiple
users have asked for Stride support (issue #2159 and others).

ADR-0002's literal "ships its own UI" test would block this, but that was never the actual
reason for excluding Unity/Godot. The real reason is cost of entry: Unity and Godot require
building and maintaining a bespoke rendering/input integration against an entrenched native UI
default, in a large, fast-moving host — a permanent, expensive tax with low odds of users
switching off the native option. That cost doesn't exist for a host that can already present an
`SKCanvas` surface: the integration is small (an input adapter, following the Silk.NET template)
regardless of whether the engine happens to ship its own UI system too.

## Decision

The engine-scope boundary is **cost-to-integrate**, not "does this engine ship its own UI." A
full engine is in scope if Gum can reach it cheaply — concretely, if it can host an `SKCanvas`
and Gum only needs to supply an `IGumService`-capability input adapter (cursor/keyboard/gamepad),
the way SilkNetGum does. Unity and Godot remain out of scope under this same test: neither offers
a cheap Skia-hosting path today, so supporting them still means a bespoke, expensive integration
against an entrenched native UI. Stride is in scope under this test.

## Consequences

- Unblocks the Stride runtime work already scoped in issue #4600, without reversing Unity/Godot's
  exclusion.
- Future "does engine X count as in scope" questions resolve by asking "can it host `SKCanvas`
  cheaply," not by checking whether it has a native UI system.
- ADR-0002's stated boundary was imprecise; this ADR amends the *reasoning*, not the outcome, for
  Unity/Godot. ADR-0002 is not superseded — its conclusion for Unity/Godot stands — but this ADR
  is the current source of truth for *why*, and for how to evaluate future candidates.

## Alternatives considered

- **Supersede ADR-0002 outright** — rejected; the Unity/Godot conclusion is unchanged, only the
  stated test was wrong. A full supersession would misleadingly suggest the boundary reopened
  more broadly than it did.
- **Treat Stride as a one-off exception without recording the general rule** — rejected; the same
  question will recur for the next Skia-hostable engine, and the reasoning is worth keeping.
