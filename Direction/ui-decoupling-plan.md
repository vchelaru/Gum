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

**Phase 0 — Baseline the burn-down (one-time measurement).** Take a one-time grep snapshot of the
coupling budget (counts of `.Self` calls, `System.Windows.*` usages in VMs, inline dialog/`MessageBox`
sites, WinForms-type leaks in interfaces) so progress is legible. *Payoff:* turns "we're decoupling"
into a number. *Risk:* trivial. **Superseded note (2026-06-20):** an earlier draft made Phase 0 a
*permanent* "ratchet" test that scans source and counts `System.Windows.*` usages. That was abandoned
(PR #3228 closed): it largely duplicates what the **compiler** enforces for free the moment a
VM/interface moves into the headless no-WPF assembly (Phase 3 — proven end-to-end by the #3229
boundary spike). The enforcement mechanism is the **project-reference compiler boundary**, not a
custom scanner; the baselines above are a one-time grep, not a standing test. If a guard is ever
wanted for the few patterns the compiler *can't* catch (`.Self` statics, `MessageBox.Show`), use a
Roslyn banned-API analyzer (`Microsoft.CodeAnalysis.BannedApiAnalyzers`) — compile-time, ~1 line per
rule.

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
guarantee. *Risk:* medium; the VM conversion is the bulk. *Status:* the boundary was already proven
end-to-end by the #3229 spike (PR #3231) — `ImportTreeNodeViewModel` now lives in headless
`Gum.ProjectServices` with its `Visibility` converted to `bool`, unit-tested with no WPF. That was a
forward de-risk only; **bulk** VM migration remains gated on Phase 2's static drain and on deciding
the permanent home (grow `Gum.ProjectServices` vs. a dedicated `Gum.Core`/`Gum.Presentation`).

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

## Phase 2 — working notes (transient scaffolding)

> **Why this section exists, and the rule that governs it.** Phase 2 is a many-pass grind — a
> handful of `.Self`/`Locator` drains per PR — and each pass tends to re-derive the same mechanics.
> **Rule: if you research something during a pass that a *later* pass of this phase will need too,
> record it here** rather than re-discovering it. This is deliberately *transient* — when Phase 2
> closes, distill anything still true into the `refactoring-direction` skill and delete the rest.
> The permanent operational *how* lives in the skills (see the "Definition of done" note below);
> this is only the live ledger for the in-flight work, kept here so it doesn't pollute the
> general-purpose skill/`CLAUDE.md` with phase-specific churn. Date additions.
>
> **Treat this section as a flywheel — leave it faster to use after every pass.** Recording new
> facts is only half the job. On each iteration also *prune* what's now stale or wrong, *sharpen*
> the recipe wherever this pass hit friction, and write down the shortcut you wish you'd had at the
> start. If a pass felt slow, that slowness is a defect *in this doc* — fix it here before moving
> on, so each drain costs less than the one before. The explicit goal is to go faster every
> iteration.

### Draining a plugin's `Locator` constructor (MEF parts) — added 2026-06-23

Plugins are MEF parts, so they can't pull from the host DI container directly; a service must be
*bridged* into the plugin container before a plugin can inject it.

- **The `batch.AddExportedValue<…>` block in `PluginManager.LoadPlugins` is the live list of
  what's injectable into plugins.** Read it to see what's already bridged — don't re-derive the
  set. Anything in that block can be taken as an `[ImportingConstructor]` parameter by any plugin.
- **Recipe per plugin:** convert the parameterless ctor (which calls `Locator.GetRequiredService<T>()`)
  to `[ImportingConstructor]` taking its ctor-time deps as parameters. For a dep already in the
  block, just take it. For a dep *not* yet bridged, add one
  `AddExportedValue<T>(Locator.GetRequiredService<T>())` line to the block first — this *relocates*
  the `Locator` call to the composition root, it doesn't delete it. (If the plugin was already
  resolving `T` via `Locator`, `T` is registered in `Builder.cs`, so the bridge resolves with no
  new `Builder.cs` work.)
- **Inject the interface, not the concrete.** If a field is the concrete `Foo : IFoo`, switch it to
  `IFoo` and bridge `IFoo` (confirm the interface carries the members the plugin uses).
- **Only ctor-time lookups are in scope.** `Locator` calls inside `StartUp`, event handlers, and
  menu-click lambdas stay. PluginBase's inherited `[Import]` properties (`_dialogService`,
  `_fileCommands`, `_guiCommands`, `_tabManager`, `_menuStripManager`) are set *after* construction,
  so a ctor-needed dep must be a ctor param even when PluginBase also exposes it.
- **Verify:** build via `GumFull.sln` (plugin post-builds need `$(SolutionDir)`), then launch the
  tool — a MEF composition failure shows an "Error loading plugins" dialog with zero plugins, so
  "tool opens with all tabs and menus present" is the all-clear.

**Bridged into the plugin container as of 2026-06-23** (snapshot — trust `LoadPlugins`, not this
line): `ISelectedState`, `IElementCommands`, `IUndoManager`, `IProjectManager`, `IGuiCommands`,
`IFileCommands`, `ITabManager`, `MenuStripManager`, `IDialogService`, `IWireframeCommands`,
`IFontManager`, `IProjectState`, `IImportLogic`, `IFileWatchManager`.

### Shortcut: batch by bridge-cost — added 2026-06-23

The cheapest, safest pass is the set of plugins whose **every** ctor-time dep is *already* in the
`AddExportedValue` block. Those drain with **no `PluginManager.cs` change at all** — pure
`[ImportingConstructor]` edits — so many fit in one homogeneous, low-risk PR. Sort the remaining
plugins by how many *new* bridges they cost and clear the zero-cost tier first; keep the
bridge-adding ones for their own batches. Nuance: a plugin that service-locates in `StartUp`
instead of the ctor still drains the same way — **relocate the assignment into the
`[ImportingConstructor]`** (construction precedes `StartUp`, so it stays behavior-preserving).

### Drained so far (plugin ctor passes)

- **#3313** — PluginBase shared services → MEF `[Import]` properties (the base-class drain that
  unblocked every per-plugin ctor drain).
- **#3317** — MainSkiaPlugin, MainFontPlugin, MainGumFormsPlugin, MainDuplicateVariablePlugin
  (added the `IWireframeCommands` / `IFontManager` / `IProjectState` / `IImportLogic` /
  `IFileWatchManager` bridges).
- **this PR (2026-06-23)** — AlignmentMainPlugin, MainCirclePlugin, MainParentPlugin, UndoPlugin,
  MainMenuStripPlugin. **Added zero bridges** — every dep (`ISelectedState`, `IUndoManager`,
  concrete `MenuStripManager`) was already in the block.

### Remaining plugin targets (triage ledger — prune as drained)

- **One new bridge each (next easy batch):** `MainInheritancePlugin` (ctor needs concrete
  `InheritanceLogic` — check for an `IInheritanceLogic` and prefer it, else bridge the concrete);
  `MainFavoriteComponentPlugin` (needs `IFavoriteComponentManager`; its lookup is in `StartUp`, so
  move it into the ctor per the nuance above).
- **Medium (1–3 new bridges, some switching a concrete to its interface):** `DeleteObjectPlugin`,
  `MainErrorsPlugin`, `MainVariableGridPlugin`, `MainFileWatchPlugin`, `MainHideShowToolsPlugin`.
- **Heavies (own PR each — large ctors and/or `Locator.GetRequiredService<PluginManager>()`
  self-injection cycle risk):** `MainEditorTabPlugin` (Tool/EditorTabPlugin_XNA),
  `MainCodeOutputPlugin`, `MainTreeViewPlugin` (pulls the 3k-line `ElementTreeViewManager`),
  `MainPropertiesWindowPlugin`, `MainStatePlugin`, `MainTextureCoordinatePlugin`.

## Definition of done — every change lands a *tested* unit

This effort's payoff *is* testability, so the bar for every change is not just "logic moved out of
the view" but "logic moved into a unit that now has a test." Specifically:

- **Each change migrates code into a testable class** — a service, ViewModel, or utility — not
  merely shuffled within a view-bound class.
- **TDD it when the change adds or alters behavior:** failing test first, then the change.
- **Pin it when the change is behavior-preserving:** a pure extraction/move still gets a
  *characterization (pinning) test* on the newly-extracted unit afterward — even though a pure
  refactor would otherwise be test-exempt. An extracted-but-untested seam has done the hard part
  and skipped the reward; capitalizing on the new seam is the whole point.

The operational *how* lives in the `tdd` and `refactoring-direction` skills.

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
