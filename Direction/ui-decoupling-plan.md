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
barrier (bus-factor insurance). *Risk:* high volume, low per-change risk. **Status (2026-07-17):
complete** — see the "Phase 2 progress gauge" section below for the closeout.

**Phase 3 — Extract logic into the headless assembly.** Move WPF-free logic into a net8.0 assembly
so the boundary is compiler-enforced. Convert the ViewModels that reach into `System.Windows.*` to
neutral types per **ADR-0004**. *Payoff:* the testability ceiling jumps; the boundary becomes a
guarantee. *Risk:* medium; the VM conversion is the bulk. *Status:* the boundary was already proven
end-to-end by the #3229 spike (PR #3231) — `ImportTreeNodeViewModel` now lives in headless
`Gum.ProjectServices` with its `Visibility` converted to `bool`, unit-tested with no WPF. That was a
forward de-risk only; **bulk** VM migration remains gated on Phase 2's static drain.
The permanent-home question is now **decided — ADR-0005**: a dedicated `Gum.Presentation` (net8.0,
no WPF) assembly, stood up and proven with its first real slice (`FileWatchViewModel` relocated out
of `Gum.csproj`, namespace preserved, with a headless pinning test). `ImportTreeNodeViewModel`
should later migrate from `Gum.ProjectServices` into `Gum.Presentation` so all VMs share one home.

> **Scouting finding (2026-06-24) that sets the bulk-migration order.** A full pass over the ~35
> tool VMs found a hard split: every VM with a *clean* dependency closure injects **zero**
> interfaces (e.g. `FileWatchViewModel`, `UndoItemViewModel`, `MainOutputViewModel`), while every
> VM that *does* inject a service pulls a WPF/WinForms-coupled contract into its closure —
> `IHotkeyManager` exposes `System.Windows.Forms.Keys`/`KeyEventArgs`; `IDialogService` is generic
> over the tool-bound `DialogViewModel`; `StateTreeViewModel` injects a concrete service with no
> interface seam. So **interface relocation is the real gate, not the VM move**: the cheap leaf VMs
> move for free, but the interesting (service-backed) VMs can't follow until the WPF types are first
> lifted out of those shared interfaces (Phase 1's "stop interfaces returning view types" work).
> Two more cross-assembly wrinkles to budget for: (1) `Builder.cs`'s VM auto-registration scans
> only `typeof(GumBuilder).Assembly`, so a DI-resolved VM moved to `Gum.Presentation` needs that
> scan (and `AddViewModelFuncFactories`) extended to the new assembly; (2) `DialogViewResolver`
> scans the *VM's own assembly* for its view, so a relocated `DialogViewModel` needs the resolver
> taught to also scan the tool assembly. The first slice sidestepped both by picking a leaf VM that
> is `new`-constructed and code-behind-bound.

**Phase 4 — The two WinForms subsystems** (the real cost; multi-week each, can overlap).
- *4a — Element tree:* decouple `ElementTreeViewManager` from `TreeNode`; the already-migrated
  state tree is the proven template.
- *4b — Rendering host:* formalize a host contract around `SystemManagers.Initialize(GraphicsDevice)`
  plus pixel-buffer-out / input-in, and lift the cursor/keyboard off the WinForms control. The
  `3218-skiasharp-host-model` work is the prototype. *Payoff (even on WPF):* lets the
  `WindowsFormsHost` airspace/focus hacks be deleted. **In scope (moved from Phase 2, 2026-07-17):**
  `WireframeControl`, `PolygonPointInputHandler`, `ResizeInputHandler` — all three `using
  System.Windows.Forms` directly and are `new`-constructed several layers deep by
  `SelectionManager`/`MainEditorTabPlugin`, not DI/MEF-resolved. They were briefly considered Phase 2
  Locator-drain debt, but they're WinForms-typed at the core, not just Locator-coupled — this phase's
  host-contract rewrite replaces them wholesale, so threading DI through code destined to be rewritten
  would be wasted work.

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
- **A `StartUp`-resolved field that's *also* wired up there (`.Tick +=`, `.Start(...)`) only
  relocates the *resolution* to the ctor — the wiring stays in `StartUp`.** Construction precedes
  `StartUp`, so assigning the field earlier is behavior-preserving; the subscription/start are
  lifecycle, not construction. (Live case: `MainFileWatchPlugin`'s `PeriodicUiTimer`.)
- **`using` bookkeeping has two traps.** (1) Bridging a *concrete* type may need a new `using` in
  `PluginManager.cs` (e.g. `Gum.Controls` for `MainPanelViewModel`, `Gum.Plugins.InternalPlugins.VariableGrid`
  for `IVariableReferenceLogic`, `Gum.Plugins.InternalPlugins.Hotkey.ViewModels` for `HotkeyViewModel`).
  (2) Don't reflexively drop `using Gum.Services;` from a drained plugin — it's only safe when `Locator`
  was the *only* thing it provided. Keep it if a still-referenced lookup remains (Errors' per-call
  `IProjectState`, RecentFiles' method-body `IProjectManager`) or if a bridged type lives in that
  namespace (`PeriodicUiTimer` is in `Gum.Services`).
- **Injecting an *internal* type costs an accessibility bump.** A `public [ImportingConstructor]`
  param must be at least as accessible as the ctor, so an internal ViewModel/service injected this way
  must be made `public` (else **CS0051**). That can cascade: **CS0053** then fires if the now-public
  type exposes a public member whose type is still internal (e.g. a nested item-VM) — bump those too.
  The already-injected VMs (`MainPanelViewModel`, `PropertyGridManager`) are public, so this only
  brings the new one in line. (Live case: `HotkeyViewModel` + its exposed `HotkeyItemViewModel`.)
- **Verify:** build via `GumFull.sln` (plugin post-builds need `$(SolutionDir)`), then launch the
  tool — a MEF composition failure shows an "Error loading plugins" dialog with zero plugins, so
  "tool opens with all tabs and menus present" is the all-clear.

**Bridged into the plugin container as of 2026-06-24** (snapshot — trust `LoadPlugins`, not this
line): `ISelectedState`, `IElementCommands`, `IUndoManager`, `IProjectManager`, `IGuiCommands`,
`IFileCommands`, `ITabManager`, `MenuStripManager`, `IDialogService`, `IWireframeCommands`,
`IFontManager`, `IProjectState`, `IImportLogic`, `IFileWatchManager`, `InheritanceLogic`,
`IFavoriteComponentManager`, `MainPanelViewModel`, `PropertyGridManager`, `IVariableReferenceLogic`,
`IErrorChecker`, `IMessenger`, `FileWatchLogic`, `PeriodicUiTimer`, `IDeleteLogic`, `HotkeyViewModel`,
`MainOutputViewModel`, `IDispatcher`, `IWireframeObjectManager`, `ISetVariableLogic`, `IHotkeyManager`,
`IEditCommands`, `ICopyPasteLogic`, `IVariableInCategoryPropagationLogic`, `ElementTreeViewManager`,
`IUserProjectSettingsManager`, `IOutputManager`, `INameVerifier`, `ITypeManager`, `LocalizationService`,
`IRetryService` (these four from #3328), `IReorderLogic`, `FileLocations`, `IUiSettingsService`,
`IThemingService`, `IDragDropManager`, `WireframeCommands` (these six from #3331).

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
- **#3319 (2026-06-23)** — AlignmentMainPlugin, MainCirclePlugin, MainParentPlugin, UndoPlugin,
  MainMenuStripPlugin. **Added zero bridges** — every dep (`ISelectedState`, `IUndoManager`,
  concrete `MenuStripManager`) was already in the block.
- **#3320 (2026-06-23)** — MainInheritancePlugin, MainFavoriteComponentPlugin. **One bridge each:**
  concrete `InheritanceLogic` (no `IInheritanceLogic` exists, so bridge the concrete) and interface
  `IFavoriteComponentManager`. MainFavoriteComponentPlugin had no ctor and resolved in `StartUp`;
  added an `[ImportingConstructor]`, moved the assignment into it, tightened the field to `readonly`.
- **#3322 (2026-06-23)** — MainHideShowToolsPlugin, MainVariableGridPlugin, MainErrorsPlugin,
  MainFileWatchPlugin. **Seven new bridges:** concretes `MainPanelViewModel`, `PropertyGridManager`,
  `FileWatchLogic` (no interfaces); interfaces `IVariableReferenceLogic`, `IErrorChecker`,
  `IMessenger`; plus transient `PeriodicUiTimer`. HideShow/VariableGrid had parameterless ctors;
  Errors/FileWatch had none and resolved in `StartUp` — both got a new `[ImportingConstructor]` with
  the assignments relocated (FileWatch keeps the timer's `.Tick`/`.Start` wiring in `StartUp` — only
  the resolution moved). Tightened the relocated fields to `readonly`; dropped a stray
  `using HarfBuzzSharp;` from VariableGrid (boyscout). Errors keeps `using Gum.Services;` (its
  per-call `IProjectState` lookup stays); FileWatch keeps it too (`PeriodicUiTimer` is in that namespace).
- **#3323 (2026-06-23)** — DeleteObjectPlugin, MainHotkeyPlugin, MainOutputPlugin,
  MainSvgExportPlugin (the whole remaining cheap/medium tier in one PR). **Three new bridges:**
  `IDeleteLogic`, transient VM `HotkeyViewModel`, singleton VM `MainOutputViewModel` (also the
  `IOutputManager` instance). DeleteObjectPlugin was already `[ImportingConstructor]`: moved its
  `IDeleteLogic`/`IWireframeCommands` lookups to params, switched the field from concrete
  `WireframeCommands` to the bridged `IWireframeCommands` (safe — `InstanceDeletionHelper` already took
  the interface and only `.Refresh()` is used), and **removed a dead injected `IElementCommands`**
  (assigned, never read) plus its now-stray `using Gum.ToolCommands`. MainSvgExportPlugin (already
  `[ImportingConstructor]`): added `ISelectedState` + `IProjectState` params — both already bridged,
  zero new bridges. Hotkey/Output had no ctor and resolved their VM in `StartUp`; both got a new
  `[ImportingConstructor]` with the resolution relocated (Hotkey's `HotkeyViewModel` + its exposed
  `HotkeyItemViewModel` had to be made `public` — see the CS0051/CS0053 recipe bullet). Dropped
  `using Gum.Services;` from all four (Locator was each one's only use of it). **Scouted but left
  (no in-scope lookup):** MainBehaviorsPlugin (already ctor-clean; only `Locator<PluginManager>()` in
  an event handler — the self-injection cycle smell) and MainRecentFilesPlugin (its `IProjectManager`
  lookups are all in method bodies / event handlers — keeps `using Gum.Services;`).
- **#3325 (2026-06-23)** — MainPropertiesWindowPlugin (first of the heavies). **Two new bridges:**
  interfaces `IDispatcher` and `IWireframeObjectManager` (both namespaces already imported in
  `PluginManager.cs`, so no new `using`). Its other five ctor-time deps — `IFontManager`,
  `IWireframeCommands`, `IDialogService`, `FileWatchLogic`, `IProjectState` — were already bridged.
  Converted the parameterless ctor (seven `Locator.GetRequiredService` lines) to an
  `[ImportingConstructor]`, and switched the field from concrete `WireframeCommands` to the bridged
  `IWireframeCommands` (safe — only `.Refresh()` is used). **Out-of-scope lookups deliberately kept**
  (so `using Gum.Services;` stays): the two `Locator<IProjectManager>()` method-body lookups
  (`HandleProjectLoad`/`HandlePropertiesClicked`) and the `Locator<IPluginManager>()` self-injection
  in `HandlePropertyChanged` — the host-into-its-own-plugin cycle smell, not a drain target. Left the
  `[Import("LocalizationService")]` property untouched (already real DI). No cycle-break and no
  accessibility bump needed — all seven deps are public and registered in `Builder.cs`.
- **#3326 (2026-06-23)** — MainTextureCoordinatePlugin + MainStatePlugin (the two cheapest,
  cycle-free heavies, drained together). **Deliberate deviation from the "own PR each" heavies
  framing:** they share `IHotkeyManager` and adjoin the same bridge block, so one combined PR avoids
  two near-identical edits to `LoadPlugins`. **Five new bridges:** `ISetVariableLogic` +
  `IHotkeyManager` (Texture Coordinate), `IEditCommands` + `ICopyPasteLogic` +
  `IVariableInCategoryPropagationLogic` (State); `IHotkeyManager` is shared. Only one new `using`
  in `PluginManager.cs` (`Gum.PropertyGridHelpers`); the other four namespaces were already imported.
  MainTextureCoordinatePlugin: parameterless ctor → `[ImportingConstructor]` (11 distinct ctor-time
  deps); repeated services — `IGuiCommands`, `IFileCommands` — injected once and reused across the
  `TextureCoordinateDisplayController` / `MainControlViewModel` / `ExposedTextureCoordinateLogic`
  constructions; took `IMessenger` as a param and kept `messenger.RegisterAll(this)` at construction.
  **Kept the lone `Locator<IObjectFinder>()`** (resolves the sanctioned `ObjectFinder.Self`, not a
  drain target), so `using Gum.Services;` stays in that file; tightened the two injected service
  fields to `readonly`. MainStatePlugin (already `[ImportingConstructor]`): added six params
  (`IElementCommands`, `IEditCommands`, `IDialogService`, `IHotkeyManager`,
  `IVariableInCategoryPropagationLogic`, `ICopyPasteLogic`) and deleted the six ctor-body `Locator`
  lines. `IDialogService` is also PluginBase's `_dialogService` `[Import]`, but it's consumed at
  construction (before property injection runs), so it **must** be a ctor param. `_objectFinder =
  ObjectFinder.Self` stays (sanctioned). Dropped `using Gum.Services;` (Locator was its only use).
  No cycle-break and no accessibility bump needed — all five interfaces are public and registered in
  `Builder.cs`. Also fixed a stale bullet in the `refactoring-specialist` agent file that claimed
  plugins can't receive DI.
- **#3327 (2026-06-24)** — MainTreeViewPlugin (third heavy). **Three new bridges:** concrete
  `ElementTreeViewManager` (the ~3k-line WinForms tree manager — registered concrete with no interface
  in `Builder.cs`, drained from a `.Self` static back in #3286), `IUserProjectSettingsManager`, and
  `IOutputManager`. Its other four ctor-time deps (`ISelectedState`, `IMessenger`, `IErrorChecker`,
  `IProjectState`) were already bridged. Converted the parameterless ctor (seven `Locator` lines) to
  an `[ImportingConstructor]`; the inline `new TreeViewStateService(_userProjectSettingsManager,
  outputManager)` stays (plain construction, not a `Locator` call) — `outputManager` is now a ctor
  param consumed only to build it, mirroring the old local `var`. **Cycle scout (the flagged risk):
  no cycle.** `ElementTreeViewManager`'s own ctor takes `PluginManager`, but that's not a construction
  cycle: ETVM is a *host*-container singleton (built before `LoadPlugins`), the plugin is only a
  *consumer* of it (never a dependency), and plugins live in the MEF container so ETVM can't depend on
  one. The plugin already resolved ETVM via `Locator` today, so the bridge just moves that same
  host-container resolution microscopically earlier — no `Lazy<T>` needed (contrast the
  `PropertyGridManager`/`SelectedState` case in `Builder.cs`, where the consumer *is* also a dependency
  and `Lazy<>` is required). One new `using` in `PluginManager.cs`
  (`Gum.Plugins.InternalPlugins.TreeView` for `ElementTreeViewManager`); the other two namespaces were
  already imported. **Kept `using Gum.Services;`** — `Locator` was *not* its only use: the plugin
  references `UiBaseFontSizeChangedMessage` (in `Gum.Services`) via `IRecipient<…>`. No accessibility
  bump needed — all three new bridges are public and registered in `Builder.cs`.
- **#3329 (2026-06-24)** — MainStateAnimationPlugin
  (`StateAnimationPlugin/MainStateAnimationPlugin.cs`) — was omitted from the triage below entirely.
  **Zero new bridges** — all six ctor-time deps (`ISelectedState`, `INameVerifier`, `IMessenger`,
  `IOutputManager`, `IFileWatchManager`, `IProjectState`) were already in the block (`INameVerifier`
  arrived with #3328, the other five long-standing). Converted the parameterless ctor (six `Locator`
  lines) to an `[ImportingConstructor]`. **No cycle-break needed:** the ctor inline-constructs
  `AnimationCollectionViewModelManager` ↔ `ElementAnimationsViewModel` ↔ `RenameManager`, but that
  construction cycle is already broken by the existing `_animationVmFactory` closure (it reads the
  fields lazily at invoke-time, *after* both are assigned) — plain `new`, not a `Locator` call, so the
  drain left it untouched. Dropped `using Gum.Services;` (Locator was its only use). No accessibility
  bump — all six deps are public and registered in `Builder.cs`.
- **#3331 (2026-06-24)** — MainEditorTabPlugin
  (`Tool/EditorTabPlugin_XNA/MainEditorTabPlugin.cs` — the external, central editor/wireframe plugin
  and the last substantive plugin ctor-drain). Converted the parameterless ctor (~22 `Locator` lookups)
  to a 21-param `[ImportingConstructor]`. **Six new bridges:** interfaces `IReorderLogic`,
  `IUiSettingsService`, `IThemingService`, `IDragDropManager`; concretes `FileLocations` and
  `WireframeCommands`. The concrete `WireframeCommands` is distinct from the already-bridged
  `IWireframeCommands` interface (resolves to the same singleton): the field is passed to
  `BackgroundManager`, whose ctor takes the *concrete* type, and `BackgroundManager.cs` was outside this
  PR's file boundary — so the "otherwise bridge concrete" branch applied instead of switching the field
  to the interface. Four service types were resolved twice in the old ctor (`IWireframeObjectManager`,
  `IElementCommands`, `ISetVariableLogic`, `IMessenger`) — injected once and reused (the inline
  `EditingManager`/`SelectionManager`/`BackgroundManager` constructions now take the injected
  fields/params). `IMessenger` is param-only (no field), used twice — `BackgroundManager` arg +
  `messenger.RegisterAll(this)`. One new `using` in `PluginManager.cs` (`Gum.Dialogs` for
  `IThemingService`); the other five bridge namespaces were already imported. **Sanctioned drive-by:**
  the method-body `Locator<IThemingService>()` in `HandleXnaInitialized` now reuses the injected
  `_themingService` field. **Kept in the body (not drain targets):** the four `Locator<PluginManager>()`
  self-injection calls (ctor + three event handlers — the host-into-its-own-plugin cycle smell) and the
  StartUp `Locator<IFontManager>()` (StartUp-time, not a ctor dep). `using Gum.Services;` stays (still
  needed for `Locator` and `UiBaseFontSizeChangedMessage`). **No cycle / no `Lazy<T>`:** all 21 injected
  deps are host-container singletons built before `LoadPlugins`, so MEF only constructs the plugin from
  pre-built deps — same benign topology scouted in #3327. No accessibility bump — all 21 deps are public
  and registered in `Builder.cs`. **This finishes the plugin ctor-drain pass.**

### Remaining plugin targets (triage ledger — prune as drained)

- **Heavies (own PR each).** ✅ **All drained — the plugin ctor-drain pass is complete.**
  `MainPropertiesWindowPlugin` (#3325), `MainTextureCoordinatePlugin` + `MainStatePlugin` (#3326),
  `MainTreeViewPlugin` (#3327 — the `ElementTreeViewManager` cycle was scouted and found benign),
  `MainCodeOutputPlugin` (#3328 — finished a partial conversion that already had a 2-param
  `[ImportingConstructor]`; only 4 new bridges, not the ~5 estimated, since #3327 already bridged
  `IOutputManager`; no cycle), `MainStateAnimationPlugin` (#3329 — never catalogued here; zero new
  bridges since all six ctor deps were already bridged, and its construction cycle was already broken by
  a VM factory closure), `MainEditorTabPlugin` (#3331 — the last and largest; 21-param ctor, six new
  bridges, no cycle). Every drained heavy turned out cycle-free, so the "cycle-prone tail" fear never
  materialized.
- **Scouted and left as out-of-scope** (no ctor/`StartUp` lookup to relocate — re-confirm before
  re-touching): `MainBehaviorsPlugin` (already ctor-clean; only `Locator<PluginManager>()` in an event
  handler — the cycle smell) and `MainRecentFilesPlugin` (only method-body/event-handler
  `IProjectManager` lookups).
- **Grep caveat when finding undrained plugins.** `grep -rl "Locator.GetRequiredService"
  Gum/Plugins/InternalPlugins` over-reports: an already-drained plugin reappears if it kept a
  *deliberate* non-ctor lookup (e.g. `MainErrorsPlugin`'s per-call `IProjectState`). It also
  under-reports — a heavy can be ctor-undrained yet not match if its lookups are all in the body.
  Classify each hit by *where* the call is (ctor/`StartUp` vs method body), don't trust the file list.

### Phase 2 progress gauge — added 2026-06-23

Phase 2 = *drain every `.Self`* **and** *retire the `Locator` fallback* — the plugin ctor passes
above are only the front door. Measured on this PR's base (`grep` over `Gum/` + `Tool/` `.cs`):

- **`Locator.GetRequiredService` call sites: ~224.** This is the fallback Phase 2 must drive to
  zero. Plugin bridges *relocate* these (composition root) but don't reduce the count much; the
  bulk are inside services that still self-locate.
- **`.Self` call sites: ~700 total, but ~476 are `ObjectFinder.Self`** (sanctioned — stays). The
  earlier "drainable ~224 / residual tool-DI ~90" figures **over-counted** — auditing the named
  targets one by one (below) collapsed the residual to nearly nothing.

**Correction (2026-06-24, `p2-closeout` PR) — the `.Self` front is effectively closed.** Three of the
four biggest named tool-DI `.Self` targets were grep mirages or `ObjectFinder`-class singletons, not
drains. Grep counts name *text*, not *drain targets*; classify each hit by where it lives
(dead/commented/shared-library/runtime) before calling it Phase 2 debt.
- **`RenderingLibrary`/`InputLibrary` runtime singletons (~130) — scoped OUT (sanctioned).** The old
  open question is resolved: `Renderer`, `ShapeManager`, `SpriteManager`, `TimeManager`,
  `LoaderManager`, `Cursor`, `Keyboard` are shared across all runtimes → `ObjectFinder` treatment,
  not a tool-DI drain. The drain line is **ownership** — tool-owned singletons stay in scope,
  runtime-owned ones do not; the permanent form of this rule lives in the `refactoring-direction`
  skill (rule 4).
- **`StandardElementsManager` (51) — NOT a drain; `ObjectFinder`-class.** It lives in `GumDataTypes`
  (excluded from `Gum.csproj`, compiled via GumCommon) and is `.Self`-used across runtimes
  (`GumService`, Skia/Raylib/Sokol `SystemManagers`), `Gum.ProjectServices`, `Gum.Cli`, and ~30 test
  files. The "51" counted Gum/+Tool/ hits that include GumCommon source. Draining it is a
  project-wide refactor with `ObjectFinder`'s exact blast radius — sanction it, don't drain it.
- **`SelectedState` (16) — DONE (deleted, not drained).** All 16 lived in `AlignmentControl.xaml.cs`,
  which `Gum.csproj` excluded from the build (`<Compile Remove>`/`<Page Remove>`) — orphaned dead code
  superseded by `AlignmentPluginControl`/`AnchorControl`/`DockControl`. Deleted the file; the live
  `ISelectedState` is already injected everywhere as `_selectedState`. `SelectedState.Self` → 0.
- **`GumCommands` — already 0 live.** Its only compiled use was the dead `AlignmentControl` (3 calls);
  the other 3 grep hits were commented-out code (removed in the same PR). `GumCommands.Self` → 0.
- **`UnitConverter` (7) — leave; `ObjectFinder`-class.** A stateless pure-math singleton in
  `GumDataTypes`; nothing to mock, and injecting it would mean extracting an interface for a math
  helper or injecting a concrete (both wrong per `refactoring-direction`).
- **`PluginManager` (5) — was deferred as a self-injection cycle smell; re-investigated and mostly
  drained (#3753)**, see the "PluginManager self-injection" entry further below — it wasn't a real cycle.

  **Net:** the genuinely-drainable tool-DI `.Self` surface is now ~empty. Phase 2's remaining bulk is
  **entirely the ~224 `Locator.GetRequiredService` self-locating services** inside the consumed
  services — that, not `.Self`, is the next front.

**Honest standing:** the *plugin ctor-drain* sub-workstream is **complete** — every substantive
plugin ctor (Properties/TextureCoordinate/State/TreeView/CodeOutput/StateAnimation/EditorTab) now
takes its ctor-time deps via `[ImportingConstructor]`, and all turned out cycle-free, so the
"cycle-prone tail" fear never materialized — even the largest, `MainEditorTabPlugin` (21 deps, the
external Tool/EditorTabPlugin_XNA home), needed no `Lazy<T>`. What remains under "plugins" is only the
deliberately-kept non-ctor lookups (the `Locator<PluginManager>()` self-injection cycle smells, and a
few method-body/`StartUp` lookups noted above). **Update (2026-07-17): Phase 2 as a whole is now
complete**, not "early-to-mid" as this paragraph originally estimated — the ~224 `Locator` fallback
figure was itself a grep-count mirage (see "Service-layer Locator sweep" below), and the service layer
turned out to have only 6 genuine drain targets plus the `PluginManager` self-injection/concrete-field
items, all landed. See the "Phase 2 is complete as of #3767" note further below for the full closeout.

### Next up (post-plugin Phase 2) — added 2026-06-24

The plugin ctor-drain pass is done and the runtime singletons are scoped out, so the next front is
the **service layer**: the ~90 drainable *tool-DI* `.Self` sites and the ~224
`Locator.GetRequiredService` fallback sites *inside* the services the plugins now inject. Suggested
ordering (cheapest, highest-confidence first):

- **First bite — `SelectedState.Self` → injected `ISelectedState`.** The interface already exists, is
  DI-registered, and is injected everywhere as `_selectedState`; the remaining ~16 `.Self` calls are
  laggard call sites. Pure call-site migration, low risk — a good warm-up for the service-layer pass.
- **Then the bigger tool-DI `.Self` targets:** `StandardElementsManager` (~51 — first check whether an
  interface exists; if not, extract one or register the concrete, *then* migrate call sites — likely
  its own PR), then `GumCommands`, `UnitConverter`, etc. Leave `PluginManager.Self` for later — it
  carries the same self-injection cycle smell deliberately kept in the drained plugins.
- **The real bulk — the ~224 `Locator` fallback:** services that self-locate their own deps instead of
  taking them via the ctor. Drain the biggest self-locating services first. Same recipe as the plugins,
  minus the MEF bridge (these are host-container, not MEF parts): inject the interface, break any
  construction cycle with `Lazy<T>` on the *consumer's* edge (rule 4 of the `refactoring-direction`
  skill).
- **Do NOT drain:** `ObjectFinder.Self` and the `RenderingLibrary`/`InputLibrary` runtime singletons
  (see the resolved scoping question above and the `refactoring-direction` skill).

Definition of done below still applies to each of these: every drain lands a *tested* unit.

### Service-layer Locator sweep (#3753) — added 2026-07-17

A full classification pass over all 140 `Locator.GetRequiredService<T>()` call sites (48 files, `grep -rn
"Locator.GetRequiredService" Gum/ Tool/`) found the "~224 fallback sites" estimate above was another
grep-count mirage, same lesson as the `.Self` figures in the Correction note. Breakdown:

- **53 + 3 are the composition roots** (`PluginManager.LoadPlugins`'s `AddExportedValue` bridge block,
  `Program.cs`) — these calls ARE the drain destination, correct as-is, not a target.
- **The rest split into:** deliberate keeps (plugin self-injection cycle smells, `StartUp`/event-handler
  lookups in already-drained plugins — matches the precedents in the plugin-ctor-pass notes above), and
  structurally out-of-scope classes that can't take ctor injection at all — WPF/WinForms Views and
  `UserControl`/`Control` code-behind instantiated by XAML/markup (not DI), `TypeConverter`s
  reflection-activated by `[TypeConverter(typeof(...))]` (parameterless-ctor contract), static
  extension-method classes, and the separate non-DI plugin assemblies (SvgPlugin, EventOutputPlugin)
  whose managers are either genuinely static or constructed outside the host container.
- **Genuine drain targets: 6**, landed as 6 separate PRs (all part of #3753, all behavior-preserving,
  each with a `GumFull.sln` build + a pinning test where the seam was cleanly mockable):
  - #3757 — `MainWindow.xaml.cs`: stored an already-injected `IMessenger` ctor param as a field instead
    of re-resolving it in a click handler.
  - #3758 — `UndosViewModel` + `Undos/MainPlugin.cs`: real ctor on the VM, threaded `ISelectedState`/
    `IUndoManager` through a new plugin `[ImportingConstructor]` (both already MEF-bridged).
  - #3759 — `MainWindowPlugin.cs`: new MEF bridge for `MainWindowViewModel`, `[ImportingConstructor]`
    replacing a method-body Locator call in a `ProjectLoad` handler.
  - #3760 — `FormsFileService` + `MainGumFormsPlugin.cs`: real ctor on the service, threaded
    `IProjectState` through the plugin's existing `[ImportingConstructor]`, plus a drive-by dedupe of a
    second method-body Locator call that became redundant once the field existed.
  - #3761 — `RenameManager` (StateAnimationPlugin): 5th ctor param for the one remaining method-body
    lookup; `MainStateAnimationPlugin` already held the dependency.
  - #3762 — `CommonControlLogic` + `AlignmentViewModel` + `AlignmentPluginControl.xaml.cs`: real ctors on
    both, `CommonControlLogic` newly registered in `Builder.cs`, the View resolves the VM via
    `Locator.GetRequiredService<AlignmentViewModel>()` at construction — the View-boundary relocation
    pattern, same role the MEF bridge plays for plugins.

**Net effect on the standing "next up" list above:** the `.Self`/`Locator` service-layer front reads as
*much thinner* than the 2026-06-24 estimates once non-DI/non-instantiable classes are excluded. There is
no large remaining "drain the biggest self-locating services first" backlog — the easy/medium tier is
now essentially exhausted. What's left in the 140-site count is either already-correct composition-root
code or structurally can't be drained without a bigger, separate refactor (e.g. converting
`ExportEventFileManager`'s all-static-member design to an instance service — a design change, not a
drain — or pulling the SvgPlugin/StateAnimationPlugin managers into the main DI container, an
assembly-boundary question outside a simple ctor-injection pass).

### PluginManager self-injection — re-investigated and partly drained (#3753) — added 2026-07-17

The "cycle smell" framing this doc (and the `refactoring-direction` skill) previously gave the ~5
`PluginManager`/`IPluginManager` self-injection sites turned out to conflate PluginManager's *role*
(orchestrating plugin loading) with an actual DI construction cycle — there isn't one. `PluginManager` is
DI-constructed with an empty ctor and fully exists before `LoadPlugins` composes a single plugin, so
bridging it (`batch.AddExportedValue<PluginManager>(instance)`/`<IPluginManager>`) is exactly as safe as
the already-bridged `ElementTreeViewManager`. The corrected rule now lives in the `refactoring-direction`
skill (rule 4): safe as long as the consuming ctor only stores the reference and doesn't call a
plugin-list-dependent method on it synchronously during construction.

Drained: `MainEditorTabPlugin` (ctor + 3 method-body/lambda call sites, all just storing/using the
reference post-construction), `MainBehaviorsPlugin` (1 method-body site, 5th ctor param added),
`MainPropertiesWindowPlugin` (1 method-body site via `IPluginManager`, 8th ctor param added). Verified via
`AllPluginsCompositionTests` (MEF composes all 3 with the new bridge) and
`ServiceProviderCompositionSpikeTests` (the real `Builder.cs` container resolves both `PluginManager` and
`IPluginManager` with no cycle exception — direct evidence the DI graph is acyclic here).

**Reclassified to Phase 4b, not Phase 2 (2026-07-17):** `PolygonPointInputHandler`, `ResizeInputHandler`,
and `WireframeControl` also call `IPluginManager`/`PluginManager` methods, and are constructed via
explicit `new` with fixed args (not MEF/DI-resolved) by code several layers up the construction chain
(`SelectionManager`/`MainEditorTabPlugin`), so threading the dependency through would mean touching
multiple intermediate constructors. Initially framed as Phase 2's one remaining item — but all three
files `using System.Windows.Forms` directly, so they're squarely Phase 4b's "lift the cursor/keyboard
off the WinForms control" rendering-host rewrite, not a Locator-drain. Threading DI through code that
phase will replace wholesale would be wasted work; see the Phase 4b bullet above.

**Phase 2 is complete as of #3767 (2026-07-17).** The concrete-`PluginManager`-field cleanup (widened
`IPluginManager` with `RefreshVariableView`, `WireframePropertyChanged`, `CreateRenderableForType`,
`WireframeRefreshed`, `TreeNodeSelected` — sealed to `object`, not `TreeNode`, per the WinForms-leak
ratchet test — `IsInitialized`, `SetHighlightedIpso`; swapped `SelectedState`, `ElementCommands`,
`EditCommands`, `GuiCommands`, `WireframeCommands`, `WireframeObjectManager`, `ElementTreeViewManager`
off the concrete class) was the last Phase 2 item once the three classes above were moved to Phase 4b.
The two remaining loose ends noted in the "Service-layer Locator sweep" section
(`ExportEventFileManager`'s all-static design, the SvgPlugin/StateAnimationPlugin DI-container question)
are separate, bigger efforts already scoped out of a simple ctor-injection pass — not Phase 2 debt.

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
