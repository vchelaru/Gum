# 0015. Treat the Skia/core dispatcher merge as an incremental long-term goal, not a rejected option

- **Status:** Accepted
- **Date:** 2026-09-04
- **Deciders:** Victor Chelaru, Claude

## Context

[0007](0007-converge-skia-property-dispatch.md) declined a physical merge of the two
`CustomSetPropertyOnRenderable` dispatchers and called it "deferred, and may never be worth it" — a
framing that reads as a closed door. In practice the two files duplicating logic is a recurring bug
source (a fix landing on one side silently missing the other — #4567/#4602), not a one-time cost, so
leaving them diverged is not neutral. PR #4619 (this branch) demonstrated the alternative: pushing
duplicated mechanical logic (Container's Alpha normalization, Circle's Radius math) onto the
already-shared runtime classes converges real behavior without needing the dispatcher files to merge.

## Decision

Physical merge stays a standing long-term goal, pursued opportunistically: every touch to either
dispatcher first asks whether the logic can move onto shared runtime-layer code, or be made
byte-identical between the two files, before fixing only the file at hand. 0007's blockers (no
compile symbol per consuming assembly; `AposShapeRuntime`/FRB Glue referencing the dispatcher by
namespace) aren't permanent — they narrow as more logic moves to the runtime layer, and get
revisited when they're the actual thing blocking a specific convergence step, not cited upfront as a
reason not to try.

## Consequences

No PR is required to "finish" the merge; each PR just needs to leave the pair no more diverged than
it found them. 0007's structural decision (runtime-type-first dispatch, files stay separate for now)
is unchanged — only its "may never be worth it" framing is corrected.

## Alternatives considered

- **Leave 0007's framing as-is and treat convergence as complete once phase-2 redispatch lands** —
  rejected, since tolerating divergence long-term is exactly what produces recurring drift-driven
  bugs like #4567/#4602.
