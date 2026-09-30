# 0020. Revisit Unity and Godot under the cost-to-integrate test

- **Status:** Proposed
- **Date:** 2026-09-30
- **Deciders:** Victor Chelaru (to decide), Claude (drafted)

## Context

[ADR-0014](0014-clarify-engine-scope-cost-to-integrate-not-owns-ui.md) made cost-to-integrate the
engine-scope test: an engine is in scope if it can host an `SKCanvas` cheaply and Gum only needs
to supply an input adapter, the way SilkNetGum does. It kept Unity and Godot out because "neither
offers a cheap Skia-hosting path today."

That premise has changed. [SkiaGameRendering](https://github.com/vchelaru/SkiaGameRendering) now
hosts Skia in both engines:

- **Unity:** GPU SkiaSharp rendering into a `RenderTexture` through ANGLE, with no CPU copy.
  Windows x64 on Direct3D 11 only, working under Mono and IL2CPP. Installable as a UPM git package
  (`https://github.com/vchelaru/SkiaGameRendering.git#upm`) since 0.17.0. Other graphics APIs need
  Unity native plugins. Not yet tested in the Unity Editor across script reloads
  (SkiaGameRendering issue #108).
- **Godot 4.7+:** an adapter covering Vulkan, D3D12, Metal and Compatibility (OpenGL). Source
  only, not on NuGet yet.

A contributor is also experimenting with a Unity port in their own fork, so the demand is no
longer hypothetical.

### What is now cheap

Skia hosting, which was the part ADR-0014 named as the blocker. Gum's Skia runtime
(`GumServiceSkiaBase`) already draws to any `SKCanvas`, so rendering Gum inside Unity or Godot is
close to the Stride/Silk.NET shape: take the canvas the host provides and draw into it.

### What is still costly

- **Input adapters.** Each engine needs cursor, keyboard and gamepad adapters, plus focus and
  input-consumption rules so Gum and the engine's own input don't both act on one click. This is
  the same work Stride needs, but Unity and Godot each have two input systems (Unity's legacy
  Input Manager and the Input System package; Godot's `_input` vs `_gui_input` chain).
- **Per-engine packaging and maintenance.** A UPM package for Unity and a Godot addon or NuGet
  package, each tracking fast-moving engine versions. Every runtime is a permanent N-way tax, and
  these two are the largest and fastest-moving hosts Gum would support.
- **Platform coverage.** The Unity path is Windows D3D11 only. A user shipping to macOS, mobile or
  consoles hits a wall that Gum can't fix without native-plugin work in SkiaGameRendering.
- **Users who already have native UI.** Unity (uGUI, UI Toolkit) and Godot (Control nodes) ship
  good-enough UI. The audience that would pick Gum is narrower than the engines' raw user counts:
  mostly developers who already know Gum from MonoGame, or who want one UI across a MonoGame and
  an engine project.

## Decision (proposed)

Treat Unity and Godot as **community-supported, not upstream runtimes**, for now:

1. Upstream Gum does not ship or maintain a Unity or Godot runtime package.
2. Upstream Gum **does** accept small, engine-neutral changes that make the Skia runtime easier to
   host in Unity or Godot (for example, an input seam or a host hook in `GumServiceSkiaBase`),
   judged like any other Skia-host improvement.
3. Forks and community packages that port Gum to Unity or Godot are welcome, and the docs may link
   to them.

Revisit with a superseding ADR, moving either engine into scope, when both hold for that engine:

- its Skia-hosting path covers the platforms its users ship to (for Unity: at least one non-D3D11
  API, and a working Editor script-reload story), and is published as a normal package; and
- a working port (community or fork) shows the input adapter is Silk.NET-sized, not bespoke.

## Consequences

- ADR-0014's test stays the test. This ADR only records that Unity and Godot now pass its first
  half (Skia hosting) partially, and names what would complete it.
- Contributors doing Unity or Godot work get a clear yes for engine-neutral upstream changes,
  instead of a flat "out of scope."
- The maintenance budget stays on the framework segment until a port proves the remaining cost is
  small.
- Risk: a community package can drift or be abandoned, and users may blame Gum. Linking to it as
  community-maintained, not official, limits that.

## Alternatives considered

- **Move Unity and Godot fully into scope now.** Skia hosting is the cheap half, but the Unity
  path is single-platform and the input, packaging and version-tracking costs are unmeasured.
  Committing now repeats the risk ADR-0002 was written to avoid.
- **Keep them out unchanged.** ADR-0014's stated reason no longer holds, so leaving the record
  as-is keeps a false premise in the Direction docs and keeps turning away useful upstream changes.
- **Support only Godot.** Its adapter covers more graphics APIs than the Unity path, which makes it
  the cheaper first candidate. Left as an option inside the revisit trigger above, since it is not
  on NuGet yet and has no port to measure.
