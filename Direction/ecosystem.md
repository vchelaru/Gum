# Gum — Ecosystem & Reach

> Grounding context: how Gum grows today — partnerships, vertical integration, and the runtimes
> that determine where it can be used. A present-tense snapshot; update as the ecosystem shifts.
> See `history.md` for how Gum got here.

## Growth channels & partnerships

- **MonoGame (official).** Featured in the official MonoGame 2D documentation, and used in
  MonoGame's 3D sample project, *Ascent*. The single biggest adoption driver.
- **XnaFiddle.net.** Featured in this online "IDE" / playground (also maintained by the Gum
  author), lowering the barrier to trying Gum in the browser.
- **Community content.** Social posts and streamers / YouTubers creating Gum videos.

## Integrations (vertical integration)

Partnering with focused libraries so Gum users get capabilities without leaving the ecosystem:

- **Apos.Shapes** — shape rendering.
- **ShadowDusk** — shader compilation (newer partner library).
- **KernSmith** — dynamic font creation (newer partner library).

## Runtime reach

Gum's reach is bounded by which host runtimes it supports and how mature each is. Two axes blur
together in the word "runtime": the **rendering backend** (how pixels get drawn) and the **host
platform/windowing runtime** (where the app actually runs). A runtime can be strong on one axis
and weak on the other.

Current state:

- **MonoGame** — most mature; the primary runtime.
- **raylib** — advanced; close to parity with the MonoGame runtime.
- **Skia** — advanced on the *rendering* axis.
- **Silk.NET** — lacking; an example of a host runtime that still needs significant work.
- **KNI / FNA** — MonoGame-family runtimes.

Expanding and maturing additional runtimes is an active strategy for growing Gum's reach.
