# CodeOutputPlugin Refactor Plan

Living roadmap for breaking up `MainCodeOutputPlugin` and `CodeGenerator.cs`. Each step is designed to ship as its own PR with the system still working. Update this file as steps land.

## Current state

### `MainCodeOutputPlugin` (~600 lines)
Five jobs in one class:
- Composition root — `new`s ~10 collaborators inline
- Event bus — wires **19** Gum editor events to handlers
- UI controller — Code tab lifecycle
- Code-gen orchestrator — calls `CodeGenerator` directly for previews
- Delete-confirmation UI owner

Also has `HandleProjectLoaded` that rebuilds half its graph because `CodeGenerationFileLocationsService`, `CodeOutputElementSettingsManager`, and `CodeOutputProjectSettingsManager` capture `ProjectDirectory` at construction.

### `CodeGenerator.cs` (~5467 lines)
A dozen generators in one class, organized only by `#region`:

| Region | Approx lines | Notes |
|---|---|---|
| Variable Assignments | 990 | The largest. Gum + Xamarin/MAUI branches |
| Position/Size layout | 1120 | Gum + Xamarin layout |
| States | 430 | Enums, properties, switch, BindableProperty |
| Variables Properties (Exposed/new) | 375 | Three BindingBehavior branches |
| Initialize | 340 | InitializeInstances, Forms instantiation |
| Constructors | 298 | Per-OutputLibrary variants |
| Class/Inheritance | 216 | `GetClassNameForType`, `GetInheritance` |
| Parent | 170 | |
| MAUI-specific | 120 | |
| Register MonoGame | 135 | |
| Animation Fields | 90 | Reads `.ganx` files from disk |
| Localization | 110 | |
| Using Statements | 96 | |
| Namespace | 40 | |
| Gum Save Objects | 28 | |
| Utilities | 110 | |

**Static mutable state:** `CanvasWidth`, `CanvasHeight`, `AdjustPixelValuesForDensity` are static and mutated per-call. Latent concurrency hazard.

### Duplication
`CodeOutputPlugin.Manager.CodeGenerationService` (tool) and `HeadlessCodeGenerationService` (CLI) duplicate missing-reference handling, directory creation, file writing, and custom-stub creation. Tool prompts user; CLI always generates — preserve this semantic difference.

## Target architecture

Two-layer split: **pure engine** (strings only) + **IO/orchestration** above.

```
Gum.ProjectServices.CodeGeneration (shared, pure — no IO)
  ICodeGenerationEngine          <- coordinator, replaces CodeGenerator (~300 lines)
    IUsingStatementEmitter
    INamespaceResolver
    IInheritanceResolver           (current GetInheritance + DefaultScreenBase cascade)
    IClassNameResolver
    IStateCodeEmitter
    IInstanceFieldEmitter
    IInstanceInstantiationEmitter
    IConstructorEmitter
    IParentAssignmentEmitter
    IExposedVariableEmitter
    IVariableAssignmentEmitter     (owns TryGetFullGumLineReplacement etc.)
    ILayoutCodeEmitter             (may split Gum vs Xamarin)
    IAnimationCodeEmitter          (+ IAnimationSaveLoader for .ganx IO)
    ILocalizationCodeEmitter
    IGumSaveObjectEmitter
    IFormsBehaviorMap
    IVisualApiResolver             (shared — many callers)
    IVariableCodeHelpers           (GetVisualCast, GetGumVariableName, value-to-code, etc.)

  ICodeGenerationWriter          <- unified IO/orchestration
    Used by both tool and CLI. Owns directory creation, writing .Generated.cs,
    creating custom stubs, and missing-reference recursion.
    IMissingReferencePrompt abstraction: dialog-based (tool) vs logger-based (CLI).

Gum/CodeOutputPlugin (tool-only)
  MainCodeOutputPlugin (~120 lines)     <- composition + subscription only
  ICodeGenerationEventRouter             (the 19 events → refresh/regenerate)
  ICodeGenerationPreviewService          (Code tab refresh, ObjectFinder cache scope)
  ICodeGenerationOrchestrator            (Generate-Selected / Generate-All buttons)
  IElementDeletionCodeHandler            (the "also delete generated files?" dialog)
  ParentSetLogic (unchanged)
  RenameService (depends on ICodeGenerationWriter)
```

**New helper:** `CodeWriter` — indented `StringBuilder` wrapper with `PushIndent()`/`PopIndent()`/`Line()`/`Block()`. Replaces every `ToTabs(tabCount) + ...` call in the codebase.

**Seams, ranked by risk:**
- **Low:** `IInheritanceResolver`, `INamespaceResolver`, `IUsingStatementEmitter`, `IFormsBehaviorMap`, `IAnimationCodeEmitter`, `ILocalizationCodeEmitter`, `IGumSaveObjectEmitter`
- **Medium:** `IStateCodeEmitter`, `IConstructorEmitter`, `IParentAssignmentEmitter`, `IInstanceFieldEmitter`, `IInstanceInstantiationEmitter`
- **High:** `IVariableAssignmentEmitter`, `IExposedVariableEmitter`, `ILayoutCodeEmitter` — share a nest of helpers. Extract `IVariableCodeHelpers` first.

## Incremental plan

Each step ships independently; `CodeGenerator.cs` keeps compiling and both tool preview + `gumcli codegen` keep working throughout.

### Phase 1 — Cleanup & plugin slimming (reasonable first-cycle stopping point)

- [ ] **Step 1. Unify the IO/orchestration layer.** Extract `ICodeGenerationWriter` in `Gum.ProjectServices`. Move the duplicated logic from `CodeOutputPlugin.Manager.CodeGenerationService` and `HeadlessCodeGenerationService` (missing-reference recursion, directory creation, file writing, custom-stub creation). Introduce `IMissingReferencePrompt` with two implementations.
- [x] **Step 2. Kill `HandleProjectLoaded` reconstruction.** Done: introduced `IProjectDirectoryProvider` + `FixedProjectDirectoryProvider` (engine) and `ProjectStateDirectoryProvider` (tool adapter to `IProjectState`). All four services (`CodeGenerator`, `CodeGenerationFileLocationsService`, `CodeOutputElementSettingsManager`, `CodeOutputProjectSettingsManager`) now resolve `ProjectDirectory` lazily. Rebuild block in `MainCodeOutputPlugin.HandleProjectLoaded` removed. **Still TODO:** `CodeGenerationService` and `RenameService` internally `new` up their own `CodeGenerationFileLocationsService` + `CodeOutputElementSettingsManager` — those instantiations should be DI'd rather than duplicated; and `Builder.cs` registration of these as singletons hasn't been done yet (still instantiated inline inside `MainCodeOutputPlugin`). Roll these into Step 1 or a dedicated sub-step.
- [ ] **Step 3. Extract `ICodeGenerationEventRouter`.** Move the 19 event wirings out of `MainCodeOutputPlugin.AssignEvents`. Write an integration test enumerating all 19 **before** touching the code.
- [ ] **Step 4. Extract `ICodeGenerationPreviewService`.** Move `RefreshCodeDisplay`, Code tab show/hide, the three `CodeGenerator` preview calls, and the ObjectFinder cache dance.
- [ ] **Step 5. Extract `ICodeGenerationOrchestrator`** (Generate-Selected/All buttons) and `IElementDeletionCodeHandler` (delete-code dialog). After this, `MainCodeOutputPlugin` is ~120-150 lines.
- [ ] **Step 6. Introduce `CodeWriter`.** Migrate Namespace + Using Statements regions first — purely mechanical, ~200 lines deleted.

### Phase 2 — Low-coupling emitters

- [ ] **Step 7. Extract `IVisualApiResolver` and `IFormsBehaviorMap`.** Leaf dependencies used from dozens of places — getting them out first unblocks later steps. Update `ParentSetLogic`'s static calls.
- [ ] **Step 8. Extract `IInheritanceResolver` and `IClassNameResolver`.** Cleans up `CustomCodeGenerator`'s relationship.
- [ ] **Step 9. Extract `IAnimationCodeEmitter`** (with `IAnimationSaveLoader` for `.ganx` IO), **`ILocalizationCodeEmitter`**, **`IGumSaveObjectEmitter`**, **`IRegisterRuntimeTypeEmitter`**.

### Phase 3 — High-coupling emitters

- [ ] **Step 10. Extract `IStateCodeEmitter`.** Inject `IVariableAssignmentEmitter` as a callback (real impl still inside `CodeGenerator`).
- [ ] **Step 11. Extract `IVariableCodeHelpers`.** Gathers `GetVisualCast`, `GetIfShouldSetDirectlyOnInstance`, `GetGumVariableName`, `VariableValueToGumCode`, `VariableValueToXamarinFormsCodeValue`, `GetIfVariableShouldBeIncludedForInstance`, `TryGetFullGumLineReplacement`, `TryGetFullXamarinFormsLineReplacement`. Zero behavior change.
- [ ] **Step 12. Extract `IExposedVariableEmitter`, `IInstanceFieldEmitter`, `IInstanceInstantiationEmitter`, `IConstructorEmitter`, `IParentAssignmentEmitter`.** In that order — all depend on Step 11.
- [ ] **Step 13. Extract `ILayoutCodeEmitter`.** The biggest. Consider splitting Gum vs Xamarin. After this, `CodeGenerator.cs` is ~300 lines. Rename to `CodeGenerationEngine` behind an `ICodeGenerationEngine` facade; keep `[Obsolete]` `CodeGenerator` forwarder for one release.
- [ ] **Step 14. Move static mutable state onto context.** `CanvasWidth`, `CanvasHeight`, `AdjustPixelValuesForDensity` → `CodeGenerationRuntimeOptions` on `CodeGenerationContext`. Smallest-risk step; must come last so every emitter threads it through one context.

## Risks and gotchas

- **Auto-regeneration firehose.** 19 events funnel into `HandleRefreshAndExport`. Missing any (especially `AfterUndo`, `BehaviorReferencesChanged`, `VariableRemovedFromCategory`) produces silently stale `.Generated.cs` — users only discover at compile time. Write the integration test in Step 3 first.
- **ProjectDirectory capture.** Multiple services capture it at construction, which is why services get duplicated instances throughout the plugin. Step 2 unblocks real singleton registration.
- **Static mutable state.** `CanvasWidth`/`CanvasHeight`/`AdjustPixelValuesForDensity` cause torn reads if the CLI ever parallelizes, or if `RequestCodeGenerationMessage` fires during a tool generation loop. Fix in Step 14.
- **ObjectFinder cache scope.** Enabled/disabled at three layers today (plugin button handler, plugin preview render, `HeadlessCodeGenerationService.GenerateCodeForAllElements`). Push into `ICodeGenerationWriter` as a `using` disposable in Step 1.
- **Cross-project dependencies.** `Gum.ProjectServices` must not reference `Gum/CodeOutputPlugin`. All extracted engine services live in `Gum.ProjectServices`. Use `IMissingReferencePrompt`, `ICodeGenLogger` abstractions on the engine side — no `IDialogService` or `IGuiCommands`.
- **External callers of static members.** `CustomCodeGenerator`, `ParentSetLogic`, and likely FlatRedBall integration call static methods on `CodeGenerator` (`GetInheritance`, `GetVisualApiForInstance`, `DoesTypeHaveContent`, `GetGumFormsTypeFromBehaviors`, `StringIdPrefix`, `FormattedLocalizationCode`). Grep before renaming. Keep `[Obsolete]` forwarders for one release after each rename.
- **Tool vs CLI missing-reference semantics.** Tool prompts Yes/No and only generates on confirm; CLI always generates. Preserve both via `IMissingReferencePrompt` — do **not** accidentally make the CLI start prompting.
- **`CodeWindowViewModel.CodeGenerationAutoSetupService`** is `new`d inline. CLI has a registered `ICodeGenerationAutoSetupService`. Step 2 should consolidate these.
