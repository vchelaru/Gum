# 0008. Sequence the runtime-type-first dispatch convergence: parity, then redispatch, then converge

- **Status:** Accepted
- **Date:** 2026-07-13
- **Deciders:** Victor Chelaru, Claude

## Context

[0007](0007-converge-skia-property-dispatch.md) decided to converge the two
`CustomSetPropertyOnRenderable` dispatchers (core and Skia) by moving both toward
runtime-type-first dispatch, incrementally, keeping the files physically separate.

An agent attempt to unify the two dispatchers directly ran into repeated problems. Reviewing
both files surfaced why: their dispatch trees are keyed on the **contained renderable's**
concrete type (`renderableIpso is Sprite`, `containedObjectAsIpso is Text`, etc.), and the
renderable types they match against are not the same type under `#if` — they are unrelated
classes (`RenderingLibrary.Graphics.Sprite` vs `SkiaGum.Renderables.Sprite`). Converging
renderable-type-keyed code across backends this way requires either heavy `#if` gating inside a
shared file to swap in each backend's renderable namespace, or first moving the dispatch key
itself onto something that already is shared: the runtime layer (`SpriteRuntime`, `TextRuntime`,
etc., see [gum-architecture-layers](../../.claude/skills/gum-architecture-layers/SKILL.md)).

Runtime-type-first dispatch only works where the runtime classes already expose the same
property surface across backends. Today they don't uniformly — some runtimes have drifted, which
is itself why a straight redispatch attempt stalls: there's often no matching runtime property to
redirect to yet.

## Decision

We will converge via four phases, applied **per runtime class** (e.g. `SpriteRuntime`,
`TextRuntime`) rather than as a single global gate — a runtime class can be fully converged while
others haven't started:

1. **Parity.** Bring that runtime class's property surface (names, types) to parity across every
   backend that has it. Internals may still differ — this is interface parity, not forcing
   identical implementation (shape runtimes, for example, keep different backing renderables per
   backend at full capability; see [gum-runtime-topology](../../.claude/skills/gum-runtime-topology/SKILL.md)).
2. **Redispatch.** Once a runtime has parity, convert both dispatcher files' handling of that
   runtime's properties to set the *runtime* property rather than reaching into the underlying
   renderable directly.
3. **Converge.** With both dispatchers now keying the same runtime property names, diff the
   resulting bodies using the mirror-`#if` technique in
   [gum-cross-platform-unification](../../.claude/skills/gum-cross-platform-unification/SKILL.md#incremental-convergence-mirror-if-toward-an-empty-diff).
   Identical lines become shared, unguarded blocks. Any surviving difference is diagnosed as
   either genuine backend divergence (keep it under `#if`) or a missed parity gap from phase 1
   (fix the runtime, then retry).
4. **Link.** Once a region's cross-file diff reaches empty, physically link one file for that
   region — subject to the namespace/FRB-Glue blocker [0007](0007-converge-skia-property-dispatch.md)
   already documented. This phase stays conditional on that blocker clearing; it is not a new
   commitment to force a merge.

## Consequences

- Gives future PRs and agents a checkable order of operations: if a dispatch-unification attempt
  is stalling, the first question is "does the target runtime actually have parity yet," not "how
  do I reconcile these two renderable-type branches."
- Work proceeds one runtime class at a time, mirroring the incremental, single-capability-per-PR
  pattern [0006](0006-runtimes-declare-capabilities-through-igumservice.md) already established
  for `IGumService`.
- Phase 1 (parity) work sometimes surfaces genuine behavioral disagreements between backends
  (not just naming gaps) — those get resolved using
  [gum-cross-platform-unification](../../.claude/skills/gum-cross-platform-unification/SKILL.md)'s
  historical-drift-vs-platform-necessary classification, same as any other cross-platform
  convergence.

## Alternatives considered

- **Converge the dispatchers directly against renderable types, no runtime-parity precursor** —
  this is what the agent already attempted; rejected because the renderable types are disjoint
  classes per backend, so it degenerates into heavy `#if` gating rather than a shrinking diff.
- **Add `#if`s in the dispatcher that call each backend's renderable setter directly, skip
  runtime redispatch** — rejected: it doesn't reduce the cross-file diff, it just moves the same
  divergence into a new `#if`, which is the opposite of what 0007 is converging toward.
