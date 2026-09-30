# 0020. Bring Unity and Godot into scope, and widen the mission beyond UI-less frameworks

- **Status:** Accepted
- **Date:** 2026-09-30
- **Deciders:** Victor Chelaru, Claude
- **Supersedes:** the Unity/Godot conclusion of [0002](0002-target-code-first-frameworks-not-engines.md)
  and [0014](0014-clarify-engine-scope-cost-to-integrate-not-owns-ui.md). 0014's cost-to-integrate
  test still stands.

## Context

[ADR-0014](0014-clarify-engine-scope-cost-to-integrate-not-owns-ui.md) made cost-to-integrate the
engine-scope test: an engine is in scope if it can host an `SKCanvas` cheaply and Gum only needs
to supply an input adapter. It kept Unity and Godot out because "neither offers a cheap
Skia-hosting path today."

That premise no longer holds. [SkiaGameRendering](https://github.com/vchelaru/SkiaGameRendering)
now hosts Skia in both engines:

- **Unity:** GPU SkiaSharp rendering into a `RenderTexture` through ANGLE, with no CPU copy.
  Windows x64 on Direct3D 11, under Mono and IL2CPP, installable as a UPM git package. Other
  graphics APIs need Unity native plugins, which look straightforward and are expected to follow
  on request. Editor script reloads are not yet tested (SkiaGameRendering issue #108).
- **Godot 4.7+:** an adapter that appears to cover all of Godot's rendering APIs (Vulkan, D3D12,
  Metal, Compatibility). Source only; publishing NuGet packages is the remaining, easy step.

A community member has ported Gum to Unity and it works end to end. Work is under way to bring
what that port learned into Gum, mainly by making it simpler to port Gum to any new Skia host.
That same work makes a future Godot runtime cheaper.

## Decision

1. **Unity is in scope now.** Gum ships an official Unity package, maintained in the Gum repo,
   targeting Direct3D 11 first. It is labeled experimental until it has real-world use. The
   contributor behind the community port has offered to help.
2. **Godot is in scope, with no scheduled work.** The hard part is done. A Godot runtime gets built
   when someone in the community asks for it, or earlier if a marketing push ("Gum supports Godot
   too") is judged worth it.
3. **The mission widens.** Gum is no longer defined as the UI for frameworks that ship no UI of
   their own. It serves code-first C# game development wherever Gum can be hosted cheaply,
   including full engines. The direction of travel may be "anywhere C# runs."
4. **ADR-0014's test is unchanged** and is how future hosts are judged: can the host present a
   Skia surface cheaply, so that Gum only adds input and packaging?

## Consequences

- Gum takes on a permanent maintenance cost for a Unity package that tracks Unity versions, and
  later for a Godot package. Keeping the per-host port small (shared Skia base, thin input
  adapters) is what keeps that cost bearable, so that work comes first.
- The Unity package ships with known limits: Windows D3D11 only, and Editor script reloads
  untested. Users targeting other platforms or APIs are blocked until SkiaGameRendering adds them.
- Unity and Godot users already have native UI, so adoption there will come mostly from people who
  already know Gum or want one UI across engine and framework projects. Positioning should reflect
  that, not the engines' raw user counts.
- `vision.md` (mission, audience, scope) is rewritten to match. ADR-0002's framework-first
  reasoning still describes Gum's history and its strongest audience (MonoGame).

## Alternatives considered

- **Community-supported only (the earlier draft of this ADR).** No upstream package, but accept
  engine-neutral changes. Rejected: a working port exists, a contributor is offering help, and the
  Skia-hosting cost is paid, so holding back an official package only delays reach.
- **Keep Unity and Godot out.** Rejected: ADR-0014's stated reason no longer holds.
- **Ship Godot alongside Unity now.** Rejected for now: no one has asked, and Unity has a working
  port to build from. Godot follows on demand.
