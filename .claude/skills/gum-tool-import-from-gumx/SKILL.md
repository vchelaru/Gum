---
name: gum-tool-import-from-gumx
description: The "Import from .gumx" dialog. Triggers: ImportFromGumxPlugin, GumxDependencyResolver, ImportTreeNodeViewModel, ImportFromGumxView, importing components/screens/behaviors/standards across projects, the dialog's tree rows and Details link.
---

# Import from .gumx Dialog

Cross-project import dialog (Content → Import → ".gumx…"). Lets the user pick a source `.gumx` (local or URL), preview its Components/Screens/Behaviors/Standards in a checkbox TreeView, and import a selected subset into the currently open project.

## Layout (one-screen map)

- `Gum/ImportFromGumxPlugin/MainImportFromGumxPlugin.cs` — plugin entry, menu item, DI wiring.
- `Tools/Gum.Presentation/Plugins/ImportPlugin/Services/GumxSourceService` — loads source `.gumx` (file path or URL).
- `Tools/Gum.Presentation/ImportFromGumx/Services/GumxDependencyResolver` — `ComputeTransitive(...)` → `DependencySet { TransitiveComponents, Behaviors, DifferingStandards, DifferingStandardDiffs }`. Standards that differ from destination are included; standards that match are excluded entirely. An imported Standard replaces the target's whole, element file and animation file (one the source lacks is deleted); components and screens instead follow the Skip/Overwrite choice. A differing Standard a selected element uses is force-included, its checkbox disabled with the reason as tooltip. The `DifferingStandardDiffs` dictionary (#2779) carries the full `StandardComparisonResult` per differing standard so the dialog can render a variable-level diff.
- `Tools/Gum.Presentation/ImportFromGumx/Services/GumxImportService` — performs the actual import + conflict reporting.

All three concrete services above (and their interfaces) live in the headless **Gum.Presentation** assembly (no WPF) — their dependency closures were already headless, so the concrete classes moved too, not just their interfaces. `GumxSourceService`/`IGumxSourceService` keep their original `Gum.Plugins.ImportPlugin.Services` namespace; `GumxDependencyResolver`/`GumxImportService` use `ImportFromGumxPlugin.Services`.
- `Tools/Gum.Presentation/ImportFromGumx/ImportFromGumxViewModel` — orchestrates load/preview/import; owns `RootNodes` and `RecomputeTransitiveDependencies()`.
- `Tools/Gum.ProjectServices/ImportFromGumx/ImportTreeNodeViewModel` — one node in the TreeView; folder or leaf; carries `IsChecked`, `InclusionState`, optional `StandardDiffRows`. Lives in the **headless `net8.0` `Gum.ProjectServices`** assembly (no WPF) so its display logic is unit-testable without standing up WPF (#3229, ADR-0003/0004); namespace stays `ImportFromGumxPlugin.ViewModels`.
- `Tools/Gum.ProjectServices/ImportFromGumx/StandardDiffRowViewModel` — passive `Kind + Summary` display record for one diff entry (same headless assembly; sibling `ImportPreviewItemViewModel.cs` holds the `ElementItemType`/`InclusionState` enums).
- `Tool/Gum.Avalonia/Plugins/PluginDialogs/ImportFromGumxViews.cs` — the dialog (`ImportFromGumxView`) and the read-only "Details..." modal (`StandardDiffDetailsView`), built in C#. The modal's view model is `Tools/Gum.Presentation/ImportFromGumx/StandardDiffDetailsViewModel`.

## InclusionState bookkeeping (#2642)

Three sets of state live on `ImportFromGumxViewModel`:

- `_autoAddedComponentNames` — components that the resolver pulled in transitively. Cleared and rebuilt every recompute pass. Auto-added items get reset to `NotIncluded` first so unchecked-by-user deselection sticks.
- `_userExplicitBehaviorNames` / `_userExplicitStandardNames` — behaviors/standards the user explicitly checked. Must be preserved across recompute, because `RecomputeTransitiveDependencies` wipes those groups and rebuilds from resolver output.

When you touch the recompute loop: **always** detach `OnItemPropertyChanged` before mutating `InclusionState`, then re-attach. Otherwise the mutation re-enters the recompute path and clobbers tracking.

## Tree rows

Each row is a check box plus a "Details..." `HyperlinkButton`, visible only on flagged-Standard rows (`ImportTreeNodeViewModel.IsDetailsButtonVisible`). The link opens `StandardDiffDetailsView` as a separate modal showing the row's `StandardDiffRows`; the diff is not expanded inline in the tree. A force-included Standard's check box is disabled, with `RequiredReason` as its tooltip.

## Diff row generation

`ImportFromGumxViewModel.BuildDiffRows(StandardComparisonResult)` flattens:
- Added/removed categories → `StandardDiffRowViewModel("Category added"|"Category removed", name)`.
- Each `StandardVariableDiff` → one row with `Kind` mapped from `StandardVariableDiffKind` (Added/Removed/Changed) and `Summary = "{Variable} · {Field}: {Default} → {Project} · ..."`.

Returns `null` when there are no rows, so the row shows no Details link.

## Testing

- `ImportFromGumxViewModelTests` (in `Tests/Gum.Presentation.Tests/Plugins/ImportFromGumxPlugin/`) uses `InitializeFromProjectForTesting(GumProjectSave)` to bypass the file/URL load and seed the source. The `_projectState` field exposes the destination (a `FakeProjectState` whose `GumProjectSave` is mutable) so tests can stage destination standards/components for diff and conflict scenarios.
- `GumxDependencyResolverTests` operate on the resolver directly without going through the VM.
- `ImportTreeNodeViewModelTests` lives in `Tests/Gum.ProjectServices.Tests` — it covers the node's `IsChecked` folder/child cascade and the `IsDetailsButtonVisible` flag.
- The Avalonia view is covered headlessly in `Tests/Gum.Avalonia.Tests` (`PluginDialogTests`), and the end-to-end import scenarios are in `EndToEnd/FormsAndImportScenarioTests.cs`.

## History notes

- #2642 fixed user-explicit behaviors/standards being wiped on the dispatcher tick.
- #2644 added the conflict-resolution dialog (Skip / Overwrite All).
- #2779 added variable-level diff rendering for flagged Standards. The underlying diff infrastructure (`IStandardComparer` in `Tools/Gum.ProjectServices`) already existed for the CLI; this issue surfaces it in the UI.
