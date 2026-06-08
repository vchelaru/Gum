# Phase 22 — Testing & regression-parity

## Purpose

Give the migration an owned, automated way to prove the Avalonia head behaves like the WPF tool — instead of leaving "parity" as a scatter of manual checklist bullets across Phases 5/10/15/20/25. The repo already has a strong tdd discipline and a headless test allow-list (`build-and-test.yaml`, Bucket A); this phase builds the test *infrastructure* the migration needs:

1. **Codegen / save golden-file parity** — automated proof that the Avalonia head and the WPF head produce byte-identical saved `.gumx`/`.gucx` files and byte-identical generated code, since both share the headless core (`GumCommon`, `Tools/Gum.ProjectServices`, `ObjectFinder`).
2. **Avalonia-head smoke tests** — headless launch/compose tests that prove the Avalonia composition root (`AddGumCore()` + `AddGumAvalonia()`) wires up, the shell window resolves from DI, and views instantiate, without a GPU.
3. **Seam-contract tests** — tests that pin the Phase 5 seams (dialogs, theming, panel host, property grid) so the WPF and Avalonia implementations satisfy the same contract.

The repo has no approval/golden-file framework today (no Verify/ApprovalTests dependency was found); this phase decides and introduces one, or builds a lightweight byte-compare harness, and wires the results into the Phase 7 CI matrix.

This phase is test-only: it adds test projects and CI wiring and changes no product behavior. Per the tdd skill, the parity tests it introduces are the safety net for Phases 15/20/25 — ideally seeded as those phases land, then consolidated and made authoritative here.

## Decisions & rationale

- **Decision → Reason → Direction:** The parity gate is **byte-identical** save/codegen, asserted on raw bytes **per-OS**. → Both heads share the same headless core (`GumCommon`, `Tools/Gum.ProjectServices`, `ObjectFinder`, `CodeOutputPlugin`), so their output *must* match — anything else is a defect. A byte-compare (not a visual/semantic diff) is what catches the line-ending, culture-sensitive formatting, and path-separator hazards that a visual diff silently tolerates. → Load the corpus through the core, re-save and re-generate, and assert `byte[]` equality against committed baselines on Windows, macOS, and Linux separately.
- **Decision → Reason → Direction:** Headless composition smoke tests run via `Avalonia.Headless` with **no GPU** on CI. → The goal is to prove composition + DI wiring + absence of WPF/WinForms types in the resolved graph — none of which need rendering. Canvas pixel correctness is a separate concern and stays a manual job (Phase 12 spike + Phase 15 checklist), because CI runners have no graphics device. → Compose the host, resolve the shell window and key VMs, and assert on the object graph, never on pixels.
- **Decision → Reason → Direction (Deferred to implementation):** The golden-file framework choice — Verify/ApprovalTests vs a bespoke byte-compare harness. → The repo has no approval/golden-file dependency today, and the right tool depends on ergonomics we'll only know once baselines exist. → Tie-breaker: pick whichever makes **per-OS byte assertions** and **baseline updates** cleanest; whatever is chosen, document the baseline-update workflow in this doc.

## Scope

### In scope

- A **golden-file parity harness**: load representative `.gumx` projects through the shared headless core and assert that (a) re-saved project files and (b) `CodeOutputPlugin` generated code are byte-identical to committed baselines (and identical across heads/OSes). Decide framework (Verify/ApprovalTests vs a bespoke byte-compare) and commit a baseline corpus.
- **Headless smoke tests for the Avalonia head**: compose the host via `AddGumCore()` + `AddGumAvalonia()`, resolve the main window/VMs, and assert no DI resolution failures and no WPF/WinForms types in the resolved graph — runnable on CI runners with no GPU (use Avalonia's headless test platform, `Avalonia.Headless`).
- **Seam-contract tests** shared across heads where feasible: the same test asserts the WPF and Avalonia implementations of `IDialogService`, `IThemingService`, the panel-content/`ITabManager` seam, and `IPropertyGrid` honor their contracts.
- A small **representative project corpus** for parity runs (a real `.gumx` with components, states, variables of each editor type) committed under the test project.
- Wiring all of the above into the Phase 7 CI allow-list following the existing Bucket A/B/C headless policy, and a per-OS byte-compare gate (Windows/macOS/Linux) consistent with Phase 25's "byte-identical output is the parity gate."

### Out of scope

- GPU/graphics-device rendering tests of the canvas (CI runners have no GPU; canvas correctness is proven by the Phase 12 spike + Phase 15 manual checklist). Pixel-level render comparison of the hosted canvas is explicitly not attempted here.
- Authoring the Avalonia views/grid/canvas themselves (Phases 10/15/20) — this phase tests them.
- Changing the WPF tool's behavior or its existing tests.
- A full end-to-end UI automation suite (driving real mouse/keyboard through a live window) — the existing `Tool/Tests/AutomatedTests` and manual per-phase checklists cover interactive flows; this phase focuses on headless parity + composition.
- Replacing `ObjectFinder.Self` or other intentional static singletons.

## Tasks

1. **Choose the golden-file mechanism.** Evaluate Verify/ApprovalTests vs a bespoke byte-compare against committed baselines. Favor whichever makes per-OS byte-identical assertions (and baseline updates) cleanest, given the parity gate is *byte*-identical, not visual. Record the decision and rejected option in this doc.
2. **Build the parity corpus + baselines.** Commit a small set of representative `.gumx` projects and the expected re-saved files and generated-code baselines, produced by the current WPF/headless core so they encode today's known-good output.
3. **Codegen/save parity tests.** Run the corpus through the shared headless core (`Gum.ProjectServices` load/save + `CodeOutputPlugin` codegen) and assert byte-identical output vs baselines, on each OS. This is the regression net that guarantees the Avalonia head — which shares the core — cannot silently diverge.
4. **Avalonia-head headless smoke tests.** Using `Avalonia.Headless`, compose `AddGumCore()` + `AddGumAvalonia()`, start the headless app, resolve the main window and key VMs, and assert: no DI failures, teardown (`ApplicationTeardownMessage`) fires, and the resolved graph contains no `System.Windows.*`/`System.Windows.Forms.*` types (mirrors the Phase 7 purity guard at runtime).
5. **Seam-contract tests.** Write contract tests (one set of assertions, run against both head implementations where the impl is constructible headless) for the Phase 5 seams so a future seam change breaks loudly in both heads. The **purity test — "`AddGumCore()` resolves no `System.Windows.*` types"** — is **seeded as Phase 5 lands** (so the seam split is guarded from the moment it exists) but is **owned and maintained here**: this phase is its long-term home, and it is consolidated alongside the other seam-contract assertions.
6. **CI wiring.** Add these test projects to the allow-list slots reserved in Phase 7, classify each into Bucket A (headless) per the existing policy comments, and add the per-OS byte-compare gate. Ensure `dotnet test` for them runs on Windows/macOS/Linux.
7. **Backfill discipline.** Document that Phases 15/20/25 add to the corpus/contract tests as they land (e.g. each newly ported editor display gets a parity case), so this phase's harness stays current rather than going stale.
8. **Feature-parity matrix artifact.** Create and commit a feature-parity matrix that turns the loose "parity with the WPF tool" claim into a checkable gate. Rows = user-facing features/menus/plugins; columns = WPF / Avalonia-Win / Avalonia-mac / Avalonia-Linux; each cell is **pass / defer / N-A**. Keep it under this phase's docs/test assets and update it as phases land. The condition **"the matrix has zero unresolved `defer` rows in the tool graph"** becomes a **Phase 30 entry criterion** — full cutover cannot start while any tool-graph feature is still deferred on a target head.

## Key files & projects

- `Tools\Gum.ProjectServices\` — headless load/save + codegen core the parity tests drive; already net8.0, no WPF.
- `Tests\Gum.ProjectServices.Tests\Gum.ProjectServices.Tests.csproj` — existing headless test project; natural home (or sibling) for codegen/save parity tests.
- `Tool\Tests\GumToolUnitTests\GumToolUnitTests.csproj` — existing WPF-tool unit tests (Windows-only in CI); reference for tool-side test conventions and a candidate base for shared seam-contract tests.
- `Tool\Tests\AutomatedTests\AutomatedTests.csproj` — existing automated tests; check before adding overlapping coverage.
- `Gum\CodeOutputPlugin\` — codegen whose output must stay byte-identical across heads/OSes.
- `Gum.Avalonia\` / `Gum.UiAbstractions\` — the repo-root Avalonia head + seams under test (headless composition, seam contracts).
- `.github\workflows\build-and-test.yaml` — CI allow-list (Bucket A/B/C policy) these test projects slot into; the Phase 7 slots are filled here.
- `.claude\skills\tdd`, `.claude\skills\gum-unit-tests` — repo test discipline and conventions to follow.

## Dependencies

**Summary:** this phase needs Phases 5/7/10 and is strengthened by 15/20/25 — but the **codegen/save golden-file portion branches off 5/7** and does not wait on the UI phases; only the headless-composition smoke tests need the Avalonia shell.

- **Needs Phase 5 (Extract UI seams):** seam interfaces must exist to write contract tests against; the `AddGumCore` split must exist for headless composition tests *and* for the codegen/save parity tests to drive the shared core.
- **Needs Phase 7 (CI & build pipeline):** the cross-platform matrix and reserved allow-list slots are where these tests run; the per-OS byte gate plugs into Phase 7's pipeline.
- **Needs Phase 10 (Avalonia shell skeleton):** the composition root + main window the **headless-composition smoke tests** resolve. This is the *only* part of the phase gated on the Avalonia UI — the codegen/save parity work does not need it.
- **Codegen-parity portion branches off 5/7:** once the headless core is reachable behind `AddGumCore()` (Phase 5) and CI slots exist (Phase 7), the golden-file corpus, baselines, and byte-identical save/codegen tests can be built immediately, in parallel with the UI phases.
- **Strengthened by Phases 15/20/25:** as the canvas, property grid, views, and plugins land, the corpus, contract tests, and feature-parity matrix grow to cover them.
- **Supports Phase 25's parity gate:** Phase 25's "byte-identical save/codegen" checklist items are automated by this phase's harness rather than only run manually.

## Risks & notes

- **Byte-identical is sensitive.** Diffs most often come from line endings, culture-sensitive number/date formatting, and path separators — exactly the cross-OS hazards Phase 25 calls out. Normalize deliberately and assert on bytes, not a visual diff; pin culture in the test harness.
- **No GPU on CI.** Keep every test in this phase headless (Bucket A). Anything needing a real `GraphicsDevice`/window stays out of CI (Bucket C) per the existing policy; canvas pixel correctness is the Phase 12 spike's and Phase 15's manual job.
- **`Avalonia.Headless` maturity.** Headless Avalonia composition/test support exists but has limits (no real rendering, some platform services stubbed). Validate it can resolve the real composition root early; if a service resists headless construction, that's a Phase 5/10 design smell to flag, not to paper over with mocks.
- **Baseline churn.** Golden baselines must be updatable when *intended* output changes (e.g. a deliberate codegen change), without making the gate meaningless. Whatever framework is chosen, document the baseline-update workflow.
- **Don't duplicate existing coverage.** `Gum.ProjectServices.Tests`, `GumToolUnitTests`, and `AutomatedTests` already exist — extend them where it fits rather than creating parallel projects.
- **Tests are the net, not the feature.** This phase doesn't make the Avalonia head work; it proves it stays correct. Seed cases alongside the phases that add behavior (tdd discipline), then consolidate here.

## Done / verification checklist

- [ ] Golden-file mechanism chosen and recorded (with rejected option); baseline-update workflow documented.
- [ ] Representative `.gumx` parity corpus + expected save/codegen baselines committed.
- [ ] Codegen + save parity tests assert byte-identical output vs baselines on Windows, macOS, and Linux.
- [ ] Avalonia-head headless smoke tests compose `AddGumCore()`+`AddGumAvalonia()`, resolve the main window/VMs with no DI failures, fire teardown, and assert no WPF/WinForms types in the resolved graph.
- [ ] Seam-contract tests pin the Phase 5 seams (dialogs, theming, panel host, property grid) for both heads where headless-constructible.
- [ ] Purity test ("`AddGumCore()` resolves no `System.Windows.*` types") seeded as Phase 5 lands, owned and maintained here.
- [ ] Feature-parity matrix (features/menus/plugins × WPF / Avalonia-Win / Avalonia-mac / Avalonia-Linux, each cell pass/defer/N-A) committed and kept current; zero unresolved `defer` rows in the tool graph is a Phase 30 entry criterion.
- [ ] All new test projects wired into the Phase 7 CI allow-list (Bucket A) and run on Windows/macOS/Linux.
- [ ] Per-OS byte-compare gate active and feeding Phase 25's parity checklist.
- [ ] Backfill note in place: Phases 15/20/25 extend the corpus/contract tests as they land.
- [ ] No product behavior changed; WPF head and its existing tests unaffected.
