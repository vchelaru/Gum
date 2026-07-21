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

## The north star — how "done" is actually measured (added 2026-07-20)

"Done" is not a closed issue, a completed phase, or a checked box in this doc. The only test that
counts: **could an Avalonia UI be built today, reusing the same libraries/services/ViewModels as-is
— only new Views, no business-logic rewrite?** Phase and issue completion are proxies for this, and
proxies can be wrong: a phase can close honestly against its own stated scope while the tool's
highest-value, most-interacted-with logic sits untouched, because that scope was drawn too narrowly
relative to the real question. (Concretely: Phase 3 closing "done" was true to its own defined
ViewModel list, but `ElementTreeViewManager` and the entire wireframe-canvas editing subsystem —
selection, camera, move/resize/rotate/polygon-point handling — were never in that list, and remain
fully WinForms/WPF-coupled today.)

Apply the test by evaluating, not by trusting a closure comment: pick a core interaction surface and
check concretely whether its code lives in `Gum.Presentation` (or another WPF/WinForms-free
assembly), or references `System.Windows.*`/`System.Windows.Forms.*` directly, or lives in a project
declaring `UseWPF`/`UseWindowsForms`. This is a loop, not a one-time check — evaluate against the
test, implement the gap it surfaces, evaluate again — repeated until an Avalonia app genuinely could
be built on the shared libraries as they exist, not until the phases run out.

**Scope boundary: the test targets business logic, not a control's own interactive mechanics.**
Multi-select tracking, drag-and-drop, and owner-draw rendering built directly against one framework's
widget APIs (e.g. `MultiSelectTreeView`'s WinForms `TreeView` internals) don't get pre-extracted or
"shrunk" toward a hypothetical future framework — that produces an abstraction shaped by guesswork,
not by the framework actually chosen. Defer this class of work entirely until a framework decision is
real (ADR-0003's measured prototype), then design and build it fresh against that framework's actual
paradigms.

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
guarantee. *Risk:* medium; the VM conversion is the bulk. The permanent home is **ADR-0005**: a
dedicated `Gum.Presentation` (net8.0, no WPF) assembly.

> **Current status and remaining work live in GitHub issues, not here.** See the Phase 3 tracking
> issue and its linked follow-ups for what has moved, what's blocked, and why. This doc intentionally
> does not keep a running log of per-PR progress — that duplicated the issues and let the two drift
> out of sync (#3754 was nearly closed while real Phase 3 work remained open).

**Known gotchas when moving a VM into `Gum.Presentation`** (durable architectural facts, not a
changelog — update this list when a *new kind* of gotcha is discovered, not for every move):
- **Interface relocation is the real gate, not the VM move.** A VM with a clean dependency closure
  (zero injected interfaces, or only interfaces that already live in `Gum.Presentation`) moves for
  free; one injecting a still-WPF/WinForms-coupled interface can't move until that interface is
  cleaned or relocated too. Re-check the current state of each dependency — a past note's "this
  interface is blocked" claim can go stale as unrelated PRs clean things up.
- **XAML `clr-namespace` declarations need `;assembly=Gum.Presentation`** once a bound type moves
  out of `Gum.csproj`, or the XAML fails to resolve it. `d:DesignInstance`/`d:`-namespaced
  attributes are silently skipped by `dotnet build` (only the VS designer breaks); real
  `DataTemplate`/`x:Type` bindings break the build outright.
- **`Builder.cs`'s VM auto-registration (`ForEachConcreteTypeAssignableTo<ViewModel>`) only scans
  `typeof(GumBuilder).Assembly`.** A DI-resolved VM moved to `Gum.Presentation` needs a second scan
  anchored in that assembly too, or it silently stops resolving (caught by
  `ServiceProviderCompositionSpikeTests`, not by a compile error).
- **`DialogViewResolver` scans the VM's own assembly, then falls back to an injected
  `IDialogViewAssemblyProvider`** (default impl scans every assembly currently loaded in the
  process) when the VM's own assembly has no View. This lets a relocated `DialogViewModel` resolve
  regardless of which assembly its View lives in — the Gum tool assembly, or a dynamically-loaded
  plugin (e.g. `StandardDiffDetailsViewModel` -> `ImportFromGumxPlugin.dll`). One caveat: the
  fallback only works for `[Dialog(typeof(...))]`-attributed Views, not naming-convention matching
  - that convention pairs a VM+View found within the *same* scanned assembly, so a View's naming
  convention alone can never resolve a VM that lives elsewhere. Attribute the View explicitly before
  moving its VM out of the tool assembly. The `GetUserStringDialogBaseViewModel` family resolves via
  its own assembly-agnostic special case (hardcoded to `GetUserStringDialogView`), independent of
  this fallback.
- **Porting a WinForms-cast extension method (`(node as ConcreteWrapper)?.Method() ?? false`)
  to a headless one on the same interface must *replace*, not add alongside, the old overload.**
  Both live in the same namespace with the identical `(this ITreeNode)` signature, so leaving both
  is an ambiguous-call compile error at every existing call site, not just the moved VM's. Reuse an
  existing headless identity check on the interface (e.g. a root node's `Parent == null && Text ==
  "..."`) instead of the WinForms instance-equality the cast-based version relied on.
- **A VM can depend on an "ambient" extension method that isn't linked into `Gum.Presentation`.**
  `ChoiceDialogViewModel` called `ObservableCollection<T>.AddRange` via an extension method
  declared *inside* `System.Collections.ObjectModel` (`Gum/Mvvm/ObservableCollectionExtensions.cs`)
  so no explicit `using` was needed - invisible until the VM moved and the compiler silently
  resolved to a different-signature BCL overload instead, producing a real (if confusing) CS0411.
  Fixed the same way as `ViewModel.cs`: link the file into `GumCommon.csproj` (mirroring its
  existing `ViewModel.cs` entry) and `<Compile Remove>` it from `Gum.csproj` so it isn't compiled
  twice. Watch for other extension methods declared in a BCL namespace the same way.
- **A VM's dependency on `IPluginManager` can still block the move even when the interface itself
  is already in `Gum.Presentation`, if the *operation* it needs naturally returns a tool-only
  concrete type** (e.g. `PluginContainer`/`IPlugin`, which live in `Gum.csproj` and can't be
  referenced from the headless assembly). Widen the interface using the same opaque-`object`
  pattern as `FillWithErrors`/`TreeNodeSelected`: return a small headless DTO (a `record`, e.g.
  `PluginSummary`) carrying an opaque handle `object`, and have the concrete implementation cast
  the handle back to its real type internally (#3754).
- **A VM in a separate plugin assembly (its own `.csproj`, not `Gum.csproj`) can reference sibling
  concrete classes directly, not just interfaces — those must physically move too.**
  `AddFormsViewModel` (`GumFormsPlugin.csproj`) called `ThemeRequirements`/`ThemeRequirementsDiff`
  (plain data/logic classes, no interface) directly; since a plugin assembly only ever depends
  *toward* `Gum.Presentation` (never the reverse), the VM's own assembly becomes unreachable from
  Gum.Presentation once the VM moves out, so the concrete helper had to move alongside it. Relatedly,
  a **static member on a concrete class the VM is being decoupled from** (`FormsFileService
  .DefaultThemeName` was a `public const`) has to become an **instance member on the new interface**
  — the VM can no longer name the concrete type to reach a static. And when the interface being
  extracted is declared *inside* its own implementer's file (`FileWatchManager.cs` held both
  `IFileWatchManager` and `FileWatchManager`) rather than its own file, extract it into a standalone
  file first — namespace stays the same either way, so no consumer needs a `using` change.
- **An interface-typed field doesn't save you if the VM constructs the concrete type itself.**
  `SubAnimationSelectionDialogViewModel` held `IAnimationFilePathService` but assigned it via
  `new AnimationFilePathService(selectedState)` inside its own constructor; the concrete class was
  still tool-side (its own dependency chain reached `Locator` transitively), so the field's
  interface type didn't unblock the move. Fixed by making the concrete type a constructor
  parameter instead of a `new` — the same "inject, don't construct" fix as the `Locator`/`.Self`
  drain, just triggered by a field whose *declared* type already looked headless-safe. Grep for
  `new ConcreteType(` inside a VM's own constructor, not just its field types, when auditing a
  move.
- **A VM's only obvious blocker (e.g. a `DispatcherTimer`) can mask a second, easy-to-miss one: a
  direct reference to a runtime-backend-only type** (`RenderingLibrary.Graphics.Renderer`,
  `SystemManagers`, or similar `Microsoft.Xna.Framework.*` symbols), which is XNALIKE-only and
  unreachable from the headless net8.0 assembly the same way a WPF type is. Grep the whole VM for
  `RenderingLibrary`/`Renderer`/`SystemManagers`/XNA symbols before assuming the fix is mechanical
  — this needs a render-diagnostics port designed, not a relocation. Conversely, an XNA-namespace
  `using` isn't always load-bearing; verify it's actually referenced before assuming it's a blocker.
- **Searching for a leaked interface's consumers must not be scoped to `Gum/`.** Top-level folders
  like `Tool/EditorTabPlugin_XNA/` have their own consumers of tool-wide interfaces
  (`IProjectManager`, etc.) that a `Gum/`-only grep silently misses, understating the narrowing
  work and risking a missed call site.
- **A controller's methods can be `internal` because caller and callee used to share an assembly.**
  Once the caller (a VM) moves to `Gum.Presentation`, those members need bumping to `public` even
  though the controller itself doesn't move — the same accessibility-bump cost as injecting an
  `internal` dependency, just triggered by the *VM* leaving rather than a *dependency* arriving.
- **Moving a `DialogViewModel` that resolves via naming-convention (no `[Dialog]` attribute) can
  silently drop `DialogViewResolverTests`' coverage of that resolution path** if it was the last
  production example still using it. Before swapping such a VM's pin to the cross-assembly fallback
  pattern, check whether another same-assembly VM/View pair still exercises the naming-convention
  branch; if not, add a synthetic pair to the test so that path stays covered.
- **A VM going headless doesn't automatically catch extension methods that operate on it and still
  live WPF-side.** When narrowing a dependency closure, check for extension methods keyed off the
  moved type (e.g. an `IDialogServiceExt`-style `Show<T>` helper), not just the type's own members.
- **A class with its own assembly added to `LoadPlugins`'s MEF catalog and an `[Export(...)]` member
  gets activated twice** — once as the ordinary DI-built singleton, once as an implicit second MEF
  part. Adding a new required constructor dependency to such a class needs the same
  `[ImportingConstructor]` + `batch.AddExportedValue<T>` bridge as a real plugin ctor drain, even
  though the class isn't a `PluginBase`. Only `AllPluginsCompositionTests` (a real MEF catalog scan)
  catches a miss — `ServiceProviderCompositionSpikeTests` passes regardless, since it never touches
  MEF's catalog.
- **An interface can be "mostly" headless-clean with one bad method mixed in.** `IEditVariableService`
  had 3 clean members plus one (`TryAddEditVariableOptions`) typed against `WpfDataUi.DataTypes
  .InstanceMember` (a `net8.0-windows`/WPF-only library). Split the interface rather than block the
  whole move: the clean members go to `Gum.Presentation`, the WPF-typed one moves to a new
  tool-only sibling interface implemented by the same concrete class.
- **A plugin's handler method can be WPF-free by type yet still unmovable, because it reads the
  plugin's own private fields rather than taking its dependencies as parameters.** Unlike a
  WPF-typed member (fixed by splitting an interface or relocating a type), this is fixed by pushing
  the handler into a new ctor-injected controller class — the same shape as `CameraController`/
  `SelectionManager` — taking those fields as constructor dependencies. Check field access, not just
  type references, when a plugin method looks clean on a first read.
- **A blocker doesn't have to be WPF/WinForms — a NuGet target-framework mismatch blocks identically.**
  A dependency on a `net8.0-windows`-targeted library (e.g. `CsvLibrary`) from the plain-`net8.0`
  `Gum.Presentation` is a hard `NU1201` restore error, not a compile-time type error, even though the
  library itself contains no WPF/WinForms types. Fix with the same narrow-interface seam used for a
  real WPF blocker.
- **Mocked constructor-injection unit tests give zero signal on a real DI construction cycle.**
  Injecting a new interface non-lazily can introduce a cycle through the *real* DI graph
  (`A → B → ... → A`) that every test mocking `A`'s dependencies passes cleanly, because a mock never
  touches the container — only a test that resolves the real container (`ServiceProviderCompositionSpikeTests`)
  catches it, and it fails as a hang/timeout in CI, not a clean assertion failure. Fix the same way as
  any other cycle: inject the cycling dependency as `Lazy<T>` (rule 4 of the `refactoring-direction`
  skill). Run the composition-spike test locally before pushing whenever a PR adds a new constructor
  dependency, not just when mocked tests are green.
- **A dependency's live value can come from a WPF resource dictionary
  (`Application.Current.Resources[...]`), not just a `.Self`/`Locator`-typed service.** The resource's
  own type is often a `DependencyObject`, unconstructable and unmockable outside WPF, so the fix isn't
  a DI registration for that type — it's a narrow interface exposing only the property(ies) actually
  read/written, with a WPF-side implementation that resolves the resource lazily
  (`IAppScaleProvider`/`AppScale`, #3754).
- **A class can look already-neutralized while still being blocked by two subtler things:** a
  runtime-backend-only singleton read (`Renderer.Self`/`SystemManagers.Default`) where an
  already-injected equivalent (e.g. a `Camera` field) sits unused right next to it, and a
  framework-agnostic extension method physically declared alongside its framework-specific siblings
  in a WPF/WinForms-side partner file rather than co-located with the type it extends. Converting
  the class's own event-arg types doesn't surface either — check the full dependency chain, not just
  the class body.
- **A continuous-state singleton is the same class of blocker as a discrete-event one, and is
  easy to miss because it doesn't look like "an event."** `GumMouseEventArgs`/`GumKeyEventArgs`
  neutralize WinForms *events* passed into a method; `InputLibrary.Cursor.Self` (screen X/Y, frame
  deltas, primary-button state) is state code *polls* every frame (`cursor.XChange`,
  `cursor.PrimaryDown`), with no event-arg parameter to convert — so a class can pass every other
  check and still fail at compile time on this alone. `InputLibrary.csproj` itself is the actual
  blocker (`net8.0-windows7.0`, `UseWindowsForms`/`UseWPF`), not `Cursor`'s own member surface, so
  the fix is the same shape as `CameraController`'s `Camera` injection: a neutral interface
  (`IGumCursorState`, matching `GumCursorKind`'s naming) exposing only the members polled, with
  `InputLibrary.Cursor` implementing it and the concrete singleton's value (`Cursor.Self`) passed
  in from the tool side at construction — never referenced by name from the headless code.
- **A visual-overlay dependency (handle/highlight decoration) and a selected-object-geometry
  dependency are two different blockers that look identical at first grep.** Both routes through
  `RenderingLibrary.Math.Geometry` (`Line`/`LineRectangle`/`LineCircle`/`LinePolygon`,
  XNALIKE-only — draws via `SpriteBatch`), so both fail the same way. But an overlay visual
  (`ResizeHandlesVisual`, `RotationHandleVisual`) is legitimate "platform glue" per the north-star
  test's own carve-out and unblocks with a narrow per-visual interface (`IResizeHandlesVisual`,
  exposing e.g. `GetSideOver`/`Width`/`Height`, implemented by the concrete visual that stays
  tool-side). A handler that reads/writes the *selected object's own* renderable geometry directly
  (`PolygonPointInputHandler` calling `LinePolygon.PointAt`/`SetPointAt`/`InsertPointAt`/
  `RemovePointAtIndex`) is not decoration — narrowing it needs its own `ILinePolygon`-style seam
  (undesigned as of this writing), not a quick wrapper interface, so it's a legitimate scope cut
  to leave that one handler (and any wireframe-editor subclass that only exists to host it) tool
  side while relocating the rest of an otherwise-clean family.
- **A coordinator class that directly `new`s a family it hands off to a headless consumer can be
  the real blocker, even after every dependency it reads is fixed.** `SelectionManager` injected
  `Camera`/`IGumCursorState` cleanly, but still directly constructed
  `StandardWireframeEditor`/`PolygonWireframeEditor` (tool-only concrete types, needing a tool-only
  font service and project-settings colors) and the XNALIKE rendering-primitive visuals
  (`GraphicalOutline`, `HighlightManager`, `SelectionRectangleVisual`). The fix is a factory
  interface (`IWireframeEditorFactory`) the tool side implements and the headless coordinator calls
  — the same "push construction behind a seam" shape as the visual interfaces, just one level up
  the object graph. A side effect worth naming: once construction moves behind the factory, a
  `WireframeEditor is ConcreteToolType` check the coordinator used to decide "do I already have the
  right kind" can no longer name that type either — track the decision explicitly (a small enum
  field set alongside each construction call) instead of re-deriving it via a type check. This is a
  *new* branch the compiler won't catch a regression in — it needs its own pinning test, not just a
  build-passes check.
- **A dependency injected as a full interface can be blocked by returning one non-portable type on
  a member the consumer never even reads.** `SelectionManager` took `IEditingManager` only to read
  `.ContextMenu?.IsOpen` — `ContextMenu` itself is `System.Windows.Controls.ContextMenu` (WPF),
  so the whole interface was unreachable regardless of which member was actually used. Splitting
  a same-shaped-problem interface (see the `IEditVariableService`/`WpfDataUi` bullet above) isn't
  always right when the *interface itself* is what's blocked, not one member on an otherwise-clean
  interface: here the fix was a second, narrower interface (`IContextMenuState`, just
  `IsContextMenuOpen`) implemented by the same concrete class alongside the original, unmodified
  `IEditingManager` — cheaper than splitting when nothing else consumes the interface being
  narrowed (confirm via grep before choosing this over a split).
- **Two type-check branches that look identical can differ in whether they're dead code — check
  reachability, don't assume.** `SelectionManager`'s `IsIpsoVisible` had an `is IVisible` branch
  followed by `is Sprite`/`is Text` branches for the same `.AbsoluteVisible` read; since both
  `Sprite` and `Text` already implement `IVisible`, the later branches were unreachable and safe to
  delete outright (removing the blocker, not just relocating it) — a `LinePolygon` branch doing a
  *different* check (`IsPointInside` vs. the generic bounding-box `HasCursorOver`) right next to it
  in the same class was live and needed the narrow-interface treatment instead. Don't pattern-match
  "type-check on a renderable" to one fix; check what each branch actually does before choosing
  delete vs. seam.

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
few method-body/`StartUp` lookups noted above). **Phase 2 as a whole is still early-to-mid (~20–30%)**:
the visible plugin entry points are now injected, but the ~224 `Locator` fallback sites behind them —
inside the services those plugins consume — are largely untouched. Don't read "all plugins drained" as
"Phase 2 nearly done"; the next front is the service-layer self-location, not more plugins.

### Next up (post-plugin Phase 2) — added 2026-06-24

The plugin ctor-drain pass is done and the runtime singletons are scoped out, so the next front is
the **service layer**: the ~90 drainable *tool-DI* `.Self` sites and the ~224
`Locator.GetRequiredService` fallback sites *inside* the services the plugins now inject. Suggested
ordering (cheapest, highest-confidence first):

- **First bite — `SelectedState.Self` → injected `ISelectedState`.** The interface already exists, is
  DI-registered, and is injected everywhere as `_selectedState`; the remaining ~16 `.Self` calls are
  laggard call sites. Pure call-site migration, low risk — a good warm-up for the service-layer pass.
- **The real bulk — the ~224 `Locator` fallback:** services that self-locate their own deps instead of
  taking them via the ctor. Drain the biggest self-locating services first. Same recipe as the plugins,
  minus the MEF bridge (these are host-container, not MEF parts): inject the interface, break any
  construction cycle with `Lazy<T>` on the *consumer's* edge (rule 4 of the `refactoring-direction`
  skill).
- **Do NOT drain:** `ObjectFinder.Self`, `StandardElementsManager.Self`, and the
  `RenderingLibrary`/`InputLibrary` runtime singletons (see the resolved scoping question above and
  the `refactoring-direction` skill).

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

**Left deferred — different reason than before, now a real constraint, not a smell:** `PolygonPointInputHandler`,
`ResizeInputHandler`, and `WireframeControl` also call `IPluginManager`/`PluginManager` methods, but all
three are constructed via explicit `new` with fixed args (not MEF/DI-resolved) by code several layers up
the construction chain (`SelectionManager`/`MainEditorTabPlugin`), so threading the dependency through
requires touching multiple intermediate constructors — a bigger, separate pass, not a same-shape drain.
Phase 2's service-layer front is otherwise closed; this is the only remaining item, and it's gated on
appetite for that multi-layer threading, not on any cycle risk.

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
