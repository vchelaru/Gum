# 0003. Decouple the Gum tool's UI from its application logic

- **Status:** Accepted
- **Date:** 2026-06-20
- **Deciders:** Victor Chelaru, Claude

## Context

The Gum tool is built on WPF + WinForms + KNI and has been gradually migrating toward
constructor-injected services and MVVM. A possible **cross-platform (Mac/Linux) editor via
Avalonia** is an unresolved strategic bet (see the roadmap **Later** item and `open-questions.md`):
high ceiling, high cost, not yet scoped. The question is how to proceed *under that uncertainty*
without either committing prematurely to a large rewrite or letting the tool stay un-testable and
welded to one UI framework.

A mapping of the codebase found that WPF itself is a thin, already-half-abstracted layer; the
heavier coupling is WinForms, concentrated in two load-bearing subsystems (the render/editor host
and the element tree), plus pervasive static singleton access (~475 `.Self` calls) and ~35
ViewModels that reach into `System.Windows.*`. Detail and the phased plan live in
`ui-decoupling-plan.md`.

## Decision

We will continue **decoupling UI from application logic as a no-regret investment**, structured so
that **every phase pays off on maintenance, testability, and contributor-friendliness alone** —
and so that the Avalonia swap is **deferred to a measured prototype (Phase 5)**, never a
prerequisite.

The target end-state is a **headless net8.0 assembly** (grown from `Gum.ProjectServices`, or a new
`Gum.Core`) that holds tool logic with **no WPF/WinForms reference**, so that the **compiler — not
convention — enforces** the logic↔view boundary.

## Consequences

- **Easier:** logic becomes unit-testable without standing up the UI; the contribution barrier
  drops (bus-factor insurance for a solo-maintained project); the Avalonia decision becomes
  data-driven and cheap to either enter or skip.
- **Harder / cost:** sustained refactoring effort. The ~475 `.Self` statics and the two WinForms
  subsystems are genuine multi-week work, not cleanup.
- **Watch-out:** half-done migrations leave confusing debris (the state-tree migration already did
  this). The standing rule: a subsystem is not migrated until its old-tech references are *gone*,
  not merely bypassed.
- **Follow-up:** the ViewModel-type question this surfaced is decided separately in **ADR-0004**.

## Alternatives considered

- **Commit to Avalonia now.** Rejected: a large bet on a new audience that is not yet scoped;
  premature, and it would mean rewriting against still-coupled logic.
- **Do nothing / leave coupled.** Rejected: forecloses the cross-platform option and keeps the
  logic untestable and maintenance-hostile.
- **Convention-only separation** (keep everything in one `net8.0-windows` assembly, rely on
  discipline not to import WPF in logic). Rejected: convention does not hold over years under a
  solo maintainer; a project-reference boundary is the only durable enforcement.
