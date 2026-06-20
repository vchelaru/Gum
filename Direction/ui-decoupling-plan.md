# Gum Tool — UI / Logic Decoupling Plan

> Living document. The detail behind the **Now** roadmap item *"Decouple UI from logic in the Gum
> tool."* Phases move and evolve as reality changes — this is intent, not a contract. Date
> significant changes. Created 2026-06-20.
>
> Point-in-time decisions are captured separately (append-only): **ADR-0003** (the approach) and
> **ADR-0004** (the ViewModel rule).

## Why — and why it's no-regret

The goal is **not "remove WPF."** It is: make application logic stop depending on the view, and
let the compiler enforce it. Every phase below pays off on **maintenance, testability, and
contributor-friendliness alone** — independent of whether Gum ever ships an Avalonia editor. The
actual Avalonia swap (the Mac/Linux editor bet) is a *separate* reach decision, deferred to a
measured prototype at the end, never a prerequisite. See ADR-0003.

## Where the tool actually is today (grounding)

The premise "swap WPF for Avalonia" mis-locates the cost. The mapping found:

- **WPF's footprint is thin and already half-abstracted.** There is a real Microsoft.Extensions
  DI Generic-Host composition root, a genuinely headless `Gum.ProjectServices` assembly (net8.0,
  cannot reference WPF), an `IDispatcher`/`IDialogService` seam, and a real unit-test suite.
- **The real entanglement is WinForms**, concentrated in **two load-bearing subsystems**: the KNI
  render/editor host (`GraphicsDeviceControl` + the input library) and the element tree
  (`MultiSelectTreeView` + the ~3k-line `ElementTreeViewManager`). Everything else is shallow:
  one true WinForms `Form`, the `Keys` enum for hotkeys, `DataObject`/`DragDropEffects` for
  drag-drop, and vestigial glue left by the already-migrated state tree.
- **Cross-cutting drag:** ~475 `.Self` static calls across ~103 files, and ~35 ViewModels that
  reach into `System.Windows.*`.
- **The biggest asset:** the renderer is **render-to-texture + CPU readback**, not a swapchain
  presenting to an HWND. It already hands any host a portable CPU pixel buffer — so the hard part
  of embedding a GPU surface in a new UI framework (airspace, HWND interop, swapchain present)
  **does not exist here.** Any framework that can blit a bitmap can host the Gum canvas.

## The phases

Each lists its goal, the payoff if Avalonia never ships, and the main risk.

**Phase 0 — Instrument the burn-down.** Establish a measurable coupling budget (counts of `.Self`
calls, `System.Windows.*` usages in VMs, inline dialog/`MessageBox` sites, WinForms-type leaks in
interfaces) and, ideally, a guard test that fails when a VM imports `System.Windows`. *Payoff:*
turns "we're decoupling" into a number that can't silently regress. *Risk:* trivial.

**Phase 1 — Kill the shallow scatter & seal the leaky seams.** Gum-owned key enum to replace
`Keys`; abstract drag-drop off the WinForms types; move the lone `Form` and the inline dialogs
behind `IDialogService`; stop the "abstraction" interfaces from returning view types; delete the
vestigial state-tree glue. *Payoff:* removes the WinForms flag from several projects; clears real
debt. *Risk:* low; wide but shallow.

**Phase 2 — Drain the statics.** Migrate `.Self` call sites to constructor injection; retire the
`Locator` fallback. *Payoff:* the biggest single lever on testability and the contribution
barrier (bus-factor insurance). *Risk:* high volume, low per-change risk.

**Phase 3 — Extract logic into the headless assembly.** Move WPF-free logic into a net8.0 assembly
so the boundary is compiler-enforced. Convert the ViewModels that reach into `System.Windows.*` to
neutral types per **ADR-0004**. *Payoff:* the testability ceiling jumps; the boundary becomes a
guarantee. *Risk:* medium; the VM conversion is the bulk.

**Phase 4 — The two WinForms subsystems** (the real cost; multi-week each, can overlap).
- *4a — Element tree:* decouple `ElementTreeViewManager` from `TreeNode`; the already-migrated
  state tree is the proven template.
- *4b — Rendering host:* formalize a host contract around `SystemManagers.Initialize(GraphicsDevice)`
  plus pixel-buffer-out / input-in, and lift the cursor/keyboard off the WinForms control. The
  `3218-skiasharp-host-model` work is the prototype. *Payoff (even on WPF):* lets the
  `WindowsFormsHost` airspace/focus hacks be deleted.

**Phase 5 — The actual bet (deferred, and only now decidable).** Build *one* real Avalonia screen
(the tree, or the canvas on the Skia host) against the now-unchanged logic. **Measure** the
effort, then decide the reach-vs-cost question (see `open-questions.md` and the roadmap **Later**
item) with data instead of a guess.

## Ordering rationale

The order is chosen so each phase de-risks the next: instrument before changing (Phase 0 is the
ruler); drain statics before extracting the assembly (Phase 2 → 3), because a half-global object
graph can't cleanly move; seal the interface seams before any view swap (Phase 1 → 5). Avalonia is
last because only then is its cost measurable rather than guessed.

**Motivation caveat (deliberate deviation from strict risk-order):** the rendering host (4b) is
both the most fun and already in flight. It may run as a *parallel refuel track* alongside the
Phase 0–2 grind — kept as a **prototype** so it doesn't force the assembly split before Phase 3 is
ready. For a solo-maintained project, protecting maintainer engagement outranks strict sequencing.

**Migration discipline:** a subsystem is not "migrated" until the old-tech references are *gone*,
not merely bypassed. The state-tree migration left vestigial WinForms-typed glue behind — exactly
the kind of debris that taxes the next contributor. Finish each migration.
