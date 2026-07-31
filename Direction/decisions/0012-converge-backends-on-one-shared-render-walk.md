# 0012. Converge every backend on one shared render walk, including render-target bakes

- **Status:** Accepted
- **Date:** 2026-07-31
- **Deciders:** Victor Chelaru, Claude

## Context

The main render pass was split into a **build** phase (walk the tree, emit a flat `DrawCommand`
list) and a **submit** phase (walk that list, issue device calls). `IRenderableOrderer` /
`HierarchicalOrderer` own the build phase; the split exists so an alternative ordering
(`BatchKeyGroupedOrderer`) can be swapped in without touching submit.

Only the XNALIKE backends went through that abstraction. raylib kept a hand-rolled recursive
walk (`DrawGumRecursively`) that reimplements the same concerns inline: the off-screen cull, the
`ClipsChildren` scissor push/pop, and hierarchy traversal. Duplicated logic drifts silently, and
it did: the off-screen cull fix in #4152 was applied to the orderers, went green in CI, and left
raylib still broken. The bug was only found by manually testing raylib, and the fix had to be
written a second time by hand.

Two facts make "just move raylib onto the orderer" insufficient:

1. **`BuildDrawList` only starts at a `Layer`, and computes clip rectangles in screen space by
   calling `camera.GetScissorRectangleFor` directly.** A render-target bake needs neither: it
   walks one container's children, and its children draw into an offscreen texture whose origin
   is the container's clamped top-left, so clip rects must be expressed relative to that origin.
2. **XNALIKE is not fully on the orderer either.** Its bake path (`RenderToRenderTarget`) and the
   GumBatch immediate-mode path still use the legacy recursive `Draw`. So the duplication is not
   a raylib deficiency to be corrected against a clean reference — the reference has the same
   split, and the two backends' bake walks have already drifted from each other in opposite
   directions (raylib culls inside bakes, XNALIKE does not; both had independently grown the
   same child-type-filter bug).

## Decision

We will make **one** walk implementation serve every backend and both passes — the main pass and
the render-target bake — rather than unifying only the main pass.

Concretely, `IRenderableOrderer` gains a subtree entry point (build a draw list from an arbitrary
root, not only from a `Layer`) and takes the renderable-to-clip-rectangle mapping from its caller
instead of hardcoding screen space. The bake supplies its own origin-relative mapping. The
existing `BuildDrawList(Layer, …)` behavior stays byte-identical for current callers.

The shared walk **culls off-screen content whenever a clip is active, in bakes as well as the
main pass** — raylib's current behavior. XNALIKE's bake path gains the cull it never had.

## Consequences

- One cull/clip/traversal implementation instead of four (two backends × two passes). A fix
  applied once reaches every backend, which is the property that #4152 lacked.
- Backend-specific concerns stay in the submit phase, where they belong: raylib's manual scissor
  intersection (its `BeginScissorMode` replaces rather than intersects), its
  `BatchDrawCallCounter` routing, and each backend's render-target composite.
- **This changes real XNALIKE behavior**, so it is not a pure refactor and cannot be validated by
  compilation plus existing tests alone. Landing it requires new XNALIKE coverage for clipped
  content inside a render-target container. `CullOffscreenWhenClipped` is still marked
  experimental in the source and remains globally switchable via
  `CameraScissorExtensions.CullOffscreenWhenClipped`, which is the escape hatch if the widened
  cull misbehaves on a real project.
- Sequencing: the entry point can be built and raylib's main walk migrated behavior-preservingly
  first, with both bake paths moved onto the entry point as separate follow-up work. The behavioral
  change is thereby isolated from the structural one.
- The three places raylib's walk disagreed with `HierarchicalOrderer` (visibility gate placement,
  clip pushed after rather than before the element's own draw, and recursion filtered to
  `GraphicalUiElement` children) were corrected ahead of this convergence, so the migration itself
  has no semantics left to renegotiate.
- The GumBatch immediate-mode path is deliberately out of scope. It is a different entry contract
  (a caller-driven `Begin`/`Draw`/`End` cycle with no layer and no render-target support), not
  another copy of the same walk.

## Alternatives considered

- **Unify only the main pass; leave every backend's bake on a hand-written recursive walk.**
  Cheaper, and it stops raylib's main walk from drifting. Rejected because it preserves exactly
  the duplication this convergence exists to remove, in a path already demonstrated to drift
  independently.
- **Keep XNALIKE's no-cull bake behavior in the shared walk.** Rejected: raylib would give up an
  optimization it already ships, and the bake path would stay slower on every backend, to avoid a
  change that the cull's own active-clip precondition makes safe.
- **A flag that culls in the main pass but not in bakes.** Rejected as the worst of both — it
  writes today's accidental inconsistency into the shared code as though it were intentional.
- **Give `HierarchicalOrderer` a general-purpose coordinate-space abstraction.** Rejected as
  over-built for the one caller that needs it: bake-local space is a translate away from screen
  space, so an origin offset suffices.
