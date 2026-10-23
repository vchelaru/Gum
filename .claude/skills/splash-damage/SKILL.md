---
name: splash-damage
description: "Splash damage testing: after a reported bug, test the neighboring behavior in the same area to find sibling bugs. Triggers: the user says 'splash damage', 'splash', 'area sweep' or 'gap analysis' about a bug. Never start it on your own."
---

# Splash Damage

A bug usually exists because a test was missing. Nearby behavior in the same area is probably untested too, so it may be broken in the same way. Splash damage testing writes the one failing test for the reported bug, then a large batch of tests around it, to find the siblings.

**Run it only when the user asks.** A normal bug fix does not get a splash pass (see the `tdd` skill for the normal flow).

## Steps

1. **Pin the reported bug first.** Write the failing test, see it fail for the right reason, fix it. This is the normal `tdd` flow.
2. **Map the area.** Name the class or feature boundary, list its behaviors (inputs, units, visibility, ordering, events, states, open versus closed, before versus after layout), and mark which already have tests by grepping the matching test project. The unmarked rows are the gap list. Put it in the PR body.
3. **Write tests for the gaps in bulk.** Often 20 to 50. Cover the matrix, not one-offs: the same behavior across the inputs that could differ. Assert the correct behavior, not the current behavior. Keep the whole batch fast (a few seconds; large sweeps take their case count from `SweepScale.Cases`).
4. **Run them.** Green tests stay as coverage. Each red test is a new bug.

## What happens to a red test

- **First check whether the PR caused it.** Run the red test against `main`. If it also fails on main it is a pre-existing bug: follow the rules below. If it passes on main and fails only on this branch, the PR's own change broke it: that is a regression, so fix it inside the same PR and do not file a separate issue or park the test on a branch.
- Do not fix it inside the splash PR, and do not skip or exclude the test (a skipped test counts as untracked).
- File one issue per distinct bug (`gum-issue-creation`), naming the failing test. Keep the red test on a branch and link that branch from the issue, so the test lands with its fix.
- The splash PR contains the fix for the reported bug and the green tests only.

## Gotchas

- Stay inside the named boundary. Drifting into neighboring features turns a sweep into a rewrite.
- Do not change existing tests to make new ones pass.
- If nothing in the batch is red, say so. That is a result, and the coverage is still worth merging.
