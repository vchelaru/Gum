# Gum — Roadmap

> Living document. Items move between horizons as reality changes — this is intent, not a
> contract. Date significant changes. Last updated 2026-06-20.

Horizons describe *confidence and proximity*, not fixed dates:

- **Now** — actively being worked, or next up.
- **Next** — intended and reasonably committed, but not yet started.
- **Later** — directionally wanted; not yet scoped.

## Now

- **Web-runnable Gum demos on MonoGame / KNI.** 1–2 *showcase-quality*, linkable in-browser demos
  (KNI is the actual web / WASM target; MonoGame proper has no first-class web build). Treat them
  as **marketing collateral** — droppable in Discord / social / docs — not just sample code.
  - Double as a **themes showcase** (users are already asking how to work with the UI themes), so
    this one artifact covers samples + themes documentation + web demo.
  - Wire through **XnaFiddle** for edit-and-run-in-the-browser — a funnel only Gum has.
  - **Scope discipline:** stay in "samples / demos on KNI's existing web target." Do *not* drift
    into open-ended web-platform plumbing (WASM perf, web fonts, input quirks). Let platform fixes
    be *pulled* by a real blocker, not pushed speculatively.

- **Decouple UI from logic in the Gum tool.** (Agent-driven, in parallel with the web demos.)
  Separate the WPF UI layer from application / business logic — continuing the tool's existing
  move to constructor-injected services. **A no-regret move:** if Gum later goes cross-platform via
  Avalonia (the Mac/Linux editor bet), this is the key enabler; if it doesn't, it still improves
  testability (logic becomes unit-testable without the UI), maintainability, and contributor
  friendliness — valuable under every branch of the editor decision.
  - **Plan & decisions:** the phased approach lives in [`ui-decoupling-plan.md`](ui-decoupling-plan.md);
    the architecture calls are recorded in
    [ADR-0003](decisions/0003-decouple-tool-ui-from-logic.md) (the approach) and
    [ADR-0004](decisions/0004-viewmodels-expose-neutral-presentation-state.md) (the ViewModel rule).

## Next

- **Finish the raylib runtime's last ~10%** (shader support, advanced gamepad, other minor gaps).
  Cheap completion for a clean "raylib fully supported" story — then stop. Not a growth focus:
  small audience, low current attention; ride the 3rd-party video and maintainer goodwill as
  low-cost cross-promotion. (Whether raylib becomes a *real* strategic bet is an open question.)

## Later

- **Cross-platform (Mac / Linux) WYSIWYG editor.** Highest *ceiling* of any option — it unlocks the
  OS-locked-out slice of the **strong** (MonoGame) audience, not a new small one — but also the
  highest cost (the tool is WPF; cross-platform means a major lift and a permanent maintenance
  step-up). Could be built on Gum's own Avalonia / Skia stack (ultimate dogfooding). **Needs a
  dedicated scoping pass before committing** — see `open-questions.md`. The UI/logic decoupling now
  in flight (see **Now**) is the enabling groundwork that lowers the cost of a future "yes."

## Parked (deliberately not now)

- **Silk.NET game-readiness** (the full interactive Forms-controls layer) — high cost vs. thin
  demand (a single voice). Revisit only if real demand appears.
- **raylib codegen** — gated on first deciding raylib is a real strategic audience; don't build it
  speculatively.

## Strategic threads

Cross-cutting reasoning behind the items above (the "why"):

- **Deepen the strong (MonoGame) audience before expanding into new ones.** Serving a new small
  audience costs about the same as serving the strong one and returns far less.
- **Diversify acquisition *channels* within that audience** (web demos, samples, videos, XnaFiddle,
  social) to reduce dependence on the single MonoGame-tutorial channel — *not* by chasing new
  audiences.
- **Growth must be sublinear in ongoing cost** — favor low-maintenance-tail wins; treat each new
  runtime as a permanent N-way tax.
- **Dogfooding** (FRB2, own games) as a refuel + quality thread — improves Gum-on-MonoGame and
  keeps the solo maintainer engaged.
- **Prefer no-regret enabling investments under uncertainty.** When a big bet is unresolved (e.g.
  the Mac/Linux editor), favor groundwork that pays off whether or not you take the bet — like
  decoupling UI from logic, which improves the codebase regardless *and* cheapens the bet.

(The candid maintainer-sustainability reasoning behind these lives in the private strategy file —
see Gum's `CLAUDE.local.md` for the pointer.)
