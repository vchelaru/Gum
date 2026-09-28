# 0019. An end-to-end regression suite, run per PR and nightly

- **Status:** Accepted
- **Date:** 2026-09-28
- **Deciders:** Victor Chelaru, Claude

## Context

Pre-release testing of the tool (#5126) mostly re-ran narrow checks and left most user actions
untested: creating, copying, pasting, renaming, reparenting and undo. #5141 asked for an automated
equivalent of a manual QA pass that needs no AI to run.

That suite now exists. `Direction/avalonia-migration/functionality-inventory.md` lists every tool
feature with a stable ID, and the scenarios in `Tests/Gum.Avalonia.Tests/EndToEnd/` drive the real
Avalonia head headlessly, tagged `[Trait("Feature", "<ID>")]` and `[Trait("Category", "EndToEnd")]`.
Every scenario checks the shared oracles: save, reload and `gumcli check` come back clean, undo back
to the start restores the files byte for byte, and no exception is logged. `Tools/e2e-coverage.ps1`
lists the inventory items no test covers.

The issue planned to keep the suite out of per-PR CI. Per-PR CI already runs it, as part of
`Gum.Avalonia.Tests` in `Build-Avalonia-Head`, and it is off the critical path: that job takes 7 to 10
minutes on each OS while `Build-and-Test` takes 12 to 14 and `Build-Samples` about 18.

## Decision

We keep a feature inventory and an end-to-end suite derived from it. A new tool feature gets an
inventory ID and a scenario tagged with it.

The suite keeps running in per-PR CI. It adds no wall-clock time to a PR, and a scenario that fails there
fails in the PR that caused the break. Building the suite found over a dozen tool bugs (#5141).

`.github/workflows/e2e-nightly.yaml` also runs `Category=EndToEnd` on main every night on Windows,
macOS and Linux, and on demand before a release. It catches what changes without a PR (runner
images, the SDK, floating package versions) and repeats the suite daily, so a flaky scenario shows
up as an intermittent failure. A failed run only fails; it files no issue.

## Consequences

- A PR that breaks a tool feature fails before merge, not the next morning.
- Per-PR time grows with the suite. When `Build-Avalonia-Head` becomes the slowest required job,
  filter `Category!=EndToEnd` out of the per-PR run and rely on the nightly run. The trait already
  supports it.
- A red nightly run is seen only by someone who looks at the Actions tab.
- The canvas scenarios need a GL device, so CI runs them only on Linux under Xvfb.

## Alternatives considered

- **Nightly only, as #5141 first proposed.** It saves no PR time today and delays every failure by
  up to a day, after the change that caused it has merged.
- **A separate test project.** The scenarios share harnesses with the other `Gum.Avalonia.Tests`
  tests; a trait selects them without splitting that code.
