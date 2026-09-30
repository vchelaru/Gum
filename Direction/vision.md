# Gum — Vision

> **Status: scaffold.** This is the north star, filled in collaboratively over time. The
> sections below are prompts — replace the *italic placeholders* with real content. Edit in
> place; git tracks how the vision evolves.

## What Gum is

Gum provides UI solutions for game developers using C#: a platform-agnostic layout and control
core, runtime libraries for MonoGame, KNI, FNA, SkiaSharp, and raylib, and a WYSIWYG editor
(the Gum tool) for authoring game UI.

## Mission / north star

> Settled 2026-06-20; widened 2026-09-30 by `decisions/0020-bring-unity-and-godot-into-scope.md`.

**Gum is the visual UI editor and cross-platform runtime for code-first C# game development,
wherever Gum can be hosted cheaply.** Over time that may become "anywhere C# runs."

In practice: author UI visually in the Gum tool, then run it on MonoGame (and MonoGame-based
engines such as FlatRedBall), KNI, FNA, and raylib, on SkiaSharp-based app hosts (WPF, Avalonia,
MAUI), and on full engines that can host a Skia surface (Stride, Unity, and later Godot). Which
hosts qualify is decided by the cost-to-integrate test in
`decisions/0014-clarify-engine-scope-cost-to-integrate-not-owns-ui.md`. Gum's roots are in
frameworks that ship no UI of their own (`decisions/0002-target-code-first-frameworks-not-engines.md`),
and portability across hosts has kept it alive across changing owners.

## Who it's for

All of Gum's users share one trait: they are **code-first C# developers who want one UI system
they can author visually and carry across hosts.** Specifically:

- **Primary (now):** MonoGame indie / hobbyist developers — the center of gravity, driven by Gum's
  inclusion in the official MonoGame 2D tutorial. When priorities conflict, this audience wins.
- **Continuing:** FlatRedBall users — FRB2 uses Gum as its UI on the same MonoGame runtime, so the
  relationship carries forward at essentially zero marginal cost.
- **Distinct:** SkiaSharp app developers (WPF / Avalonia / MAUI) — often building *application* UI
  rather than games, a somewhat different user.
- **Emerging:** raylib-cs developers, as that runtime matures toward MonoGame parity.
- **New:** Unity developers (experimental package, Direct3D 11 first) and, on demand, Godot
  developers. They already have native UI, so they come to Gum mostly because they know it from
  MonoGame or want one UI across engine and framework projects.

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

- **Not for hosts that are expensive to integrate.** A host is in scope when it can present a Skia
  surface (or an existing Gum backend) cheaply, so Gum only adds input and packaging. See
  `decisions/0014-clarify-engine-scope-cost-to-integrate-not-owns-ui.md`.

*(Other boundaries will be added here as they are decided.)*

## What sets Gum apart

*Differentiators versus the alternatives (engine-native UI, other UI middleware). Why choose Gum?*
