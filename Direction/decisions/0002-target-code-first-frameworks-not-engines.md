# 0002. Target code-first C# frameworks, not full engines

- **Status:** Accepted
- **Date:** 2026-06-20
- **Deciders:** Victor Chelaru, Claude

## Context

Gum is a UI authoring tool plus runtime libraries for C# game development. The two largest C#
game-development environments are **Unity** and **Godot**, and Gum supports neither. It does
support MonoGame (and MonoGame-based engines such as FlatRedBall), KNI, FNA, raylib, and
SkiaSharp-based hosts.

This is not incidental. Unity and Godot are full **engines**: they ship their own native UI
systems (Unity's uGUI / UI Toolkit; Godot's Control nodes), an editor, and a scene system. The
environments Gum *does* support are code-first **frameworks / libraries** that ship **no UI
solution at all** — which is precisely the gap Gum fills.

A "support Unity / Godot" strategy is tempting because they are the biggest audiences, but it
would mean (a) competing against an entrenched, good-enough native default in someone else's
house, and (b) taking on the highest-burden growth possible for a solo-maintained project: two
large, fast-moving hosts, each a permanent maintenance tax, for users who mostly will not switch.
That is the opposite of "reach with sublinear cost" (see the private maintainer-sustainability
strategy).

## Decision

Gum deliberately targets **code-first C# game frameworks** (MonoGame and MonoGame-based engines
such as FlatRedBall, KNI, FNA, raylib) and **SkiaSharp-based app hosts**, and **does not target
full engines that ship their own UI (Unity, Godot)**. This boundary holds **for now, and may
become permanent**; it is revisited only if a compelling low-cost path or overwhelming demand
appears.

## Consequences

- Sharper, more honest positioning — "the UI for C# frameworks that don't have one," not
  "universal C# UI." This clarifies the mission, the audience, and the scope at once.
- Engineering effort and the maintenance budget stay focused on the framework segment, consistent
  with solo-maintainer sustainability.
- The cost: Gum forgoes the two largest *raw* C# gamedev audiences. We accept that "biggest
  audience" is not the same as "winnable audience."
- Not a permanent ban: this is a current boundary, re-openable via a superseding ADR.

## Alternatives considered

- **Add Unity and/or Godot runtimes** — maximal reach, but maximal maintenance burden against an
  entrenched native default; rejected as misaligned with a solo-maintained project's
  sustainability.
- **Stay silent on scope** — leaves the boundary looking like an oversight rather than a choice;
  rejected in favor of stating it explicitly.
