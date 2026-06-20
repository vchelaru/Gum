# Gum — Vision

> **Status: scaffold.** This is the north star, filled in collaboratively over time. The
> sections below are prompts — replace the *italic placeholders* with real content. Edit in
> place; git tracks how the vision evolves.

## What Gum is

Gum provides UI solutions for game developers using C#: a platform-agnostic layout and control
core, runtime libraries for MonoGame, KNI, FNA, SkiaSharp, and raylib, and a WYSIWYG editor
(the Gum tool) for authoring game UI.

## Mission / north star

> Settled 2026-06-20.

**Gum is the visual UI editor and cross-framework runtime for code-first C# game frameworks — the
ones that ship no UI of their own.**

In practice: author UI visually in the Gum tool, then run it on MonoGame (and MonoGame-based
engines such as FlatRedBall), KNI, FNA, and raylib, plus SkiaSharp-based app hosts (WPF, Avalonia,
MAUI). Gum deliberately serves *frameworks*, not full **engines** (Unity, Godot) that already ship
their own UI — see `decisions/0002-target-code-first-frameworks-not-engines.md`. Portability and
reach *within that segment* are the through-line of Gum's history and what has kept it alive across
changing owners.

## Who it's for

All of Gum's users share one trait: they are **code-first C# developers whose framework ships no
UI of its own.** Specifically:

- **Primary (now):** MonoGame indie / hobbyist developers — the center of gravity, driven by Gum's
  inclusion in the official MonoGame 2D tutorial. When priorities conflict, this audience wins.
- **Continuing:** FlatRedBall users — FRB2 uses Gum as its UI on the same MonoGame runtime, so the
  relationship carries forward at essentially zero marginal cost.
- **Distinct:** SkiaSharp app developers (WPF / Avalonia / MAUI) — often building *application* UI
  rather than games, a somewhat different user.
- **Emerging:** raylib-cs developers, as that runtime matures toward MonoGame parity.

## Principles

*The values that guide decisions — what we optimize for and what we refuse to trade away.
Candidates: cross-platform parity, ease of getting started, WYSIWYG fidelity, runtime
performance, backward compatibility.*

## Constraints

- **Solo-maintained.** Gum is currently maintained primarily by one person, so **maintainer
  sustainability is a first-class design constraint**: direction favors work that grows reach
  without growing maintenance burden faster than a very small team can carry. This is a statement
  of good stewardship — it is *why* Gum leans on leverage, contributor-friendliness, and keeping
  the per-change cost low across runtimes.

## Scope — what Gum is *not*

- **Not a UI layer for full engines.** Unity and Godot ship their own native UI; Gum deliberately
  does not target them — for now, possibly permanently. See
  `decisions/0002-target-code-first-frameworks-not-engines.md`.

*(Other boundaries will be added here as they are decided.)*

## What sets Gum apart

*Differentiators versus the alternatives (engine-native UI, other UI middleware). Why choose Gum?*
