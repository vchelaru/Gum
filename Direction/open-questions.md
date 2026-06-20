# Gum — Open Strategic Questions

> Questions that shape direction. When one is settled by a real decision, record it as an ADR in
> `decisions/` and move it to the **Resolved** list below with a link. Keep these **strategic**
> (direction-shaping). Tactical to-dos belong in GitHub issues.

## Open

- **Cross-platform (Mac/Linux) editor — pursue?** Highest-ceiling bet (unlocks the OS-locked-out
  slice of the MonoGame audience), but highest cost (WPF → cross-platform; permanent maintenance
  step-up). Could be built on Gum's own Avalonia/Skia stack (dogfooding). Needs a dedicated
  scoping pass before committing. Currently parked in `roadmap.md` → Later. The UI/logic decoupling
  now in flight is no-regret groundwork that lowers the cost of a future "yes," so this decision
  can be deferred without blocking progress.
- **raylib — real audience, or just finish-and-ride-goodwill?** Committing to raylib as a
  strategic audience would justify raylib codegen and deeper investment; current lean is to finish
  the runtime cheaply and *not* chase it (small audience, low attention — ride the 3rd-party video
  and maintainer goodwill as low-cost cross-promotion instead).
- **Platform strategy.** Which runtimes are first-class vs. best-effort, and how is parity
  prioritized across MonoGame / KNI / FNA / Skia / raylib? (Sharper now that each runtime is
  understood as a permanent maintenance tax — see the private growth strategy.)
- **Tool vs. code-first.** How do we balance investment between the WYSIWYG tool and
  code-only / code-generation workflows?
- **Scope boundaries (beyond engines).** Full engines are settled (ADR-0002); what *other*
  boundaries should Gum name as explicitly out of scope?

## Resolved

- **North star / mission** — settled (2026-06-20): *"Gum is the visual UI editor and
  cross-framework runtime for code-first C# game frameworks — the ones that ship no UI of their
  own."* See `vision.md` (mission).
- **Primary audience** — settled (2026-06-20): MonoGame indie / hobbyist devs are primary;
  FlatRedBall (via FRB2 on the MonoGame runtime) continuing; SkiaSharp app devs distinct; raylib
  emerging. See `vision.md` ("Who it's for"). The earlier "cross-engine teams" idea was retracted.
- **Scope: full engines** — settled (2026-06-20): Unity and Godot are deliberately out, for now
  and possibly permanently. See `decisions/0002-target-code-first-frameworks-not-engines.md`.
