---
name: gum-tool-import-from-gumx
description: The "Import from .gumx" dialog. Triggers: ImportFromGumxPlugin, GumxDependencyResolver, ImportTreeNodeViewModel, ImportFromGumxView, importing components/screens/behaviors/standards across projects, the dialog's TreeView templating.
---

# Import from .gumx Dialog

Cross-project import dialog (Content → Import → ".gumx…"). Lets the user pick a source `.gumx` (local or URL), preview its Components/Screens/Behaviors/Standards in a checkbox TreeView, and import a selected subset into the currently open project.

## Layout (one-screen map)

- `Gum/ImportFromGumxPlugin/MainImportFromGumxPlugin.cs` — plugin entry, menu item, DI wiring.
- `Tools/Gum.Presentation/Plugins/ImportPlugin/Services/GumxSourceService` — loads source `.gumx` (file path or URL).
- `Tools/Gum.Presentation/ImportFromGumx/Services/GumxDependencyResolver` — `ComputeTransitive(...)` → `DependencySet { TransitiveComponents, Behaviors, DifferingStandards, DifferingStandardDiffs }`. Standards that differ from destination are included; standards that match are excluded entirely. The `DifferingStandardDiffs` dictionary (#2779) carries the full `StandardComparisonResult` per differing standard so the dialog can render a variable-level diff.
- `Tools/Gum.Presentation/ImportFromGumx/Services/GumxImportService` — performs the actual import + conflict reporting.

All three concrete services above (and their interfaces) live in the headless **Gum.Presentation** assembly (no WPF) — their dependency closures were already headless, so the concrete classes moved too, not just their interfaces. `GumxSourceService`/`IGumxSourceService` keep their original `Gum.Plugins.ImportPlugin.Services` namespace; `GumxDependencyResolver`/`GumxImportService` use `ImportFromGumxPlugin.Services`.
- `ViewModels/ImportFromGumxViewModel` — orchestrates load/preview/import; owns `RootNodes` and `RecomputeTransitiveDependencies()`.
- `Tools/Gum.ProjectServices/ImportFromGumx/ImportTreeNodeViewModel` — one node in the TreeView; folder or leaf; carries `IsChecked`, `InclusionState`, optional `StandardDiffRows`. Lives in the **headless `net8.0` `Gum.ProjectServices`** assembly (no WPF) so its display logic is unit-testable without standing up WPF (#3229, ADR-0003/0004); namespace stays `ImportFromGumxPlugin.ViewModels`.
- `Tools/Gum.ProjectServices/ImportFromGumx/StandardDiffRowViewModel` — passive `Kind + Summary` display record for one diff entry (same headless assembly; sibling `ImportPreviewItemViewModel.cs` holds the `ElementItemType`/`InclusionState` enums).
- `Views/ImportFromGumxView.xaml` + `.xaml.cs` — the dialog.
- `ViewModels/StandardDiffDetailsViewModel.cs` + `Views/StandardDiffDetailsView.xaml` — the read-only "Details..." modal launched from a flagged Standard row.

## InclusionState bookkeeping (#2642)

Three sets of state live on `ImportFromGumxViewModel`:

- `_autoAddedComponentNames` — components that the resolver pulled in transitively. Cleared and rebuilt every recompute pass. Auto-added items get reset to `NotIncluded` first so unchecked-by-user deselection sticks.
- `_userExplicitBehaviorNames` / `_userExplicitStandardNames` — behaviors/standards the user explicitly checked. Must be preserved across recompute, because `RecomputeTransitiveDependencies` wipes those groups and rebuilds from resolver output.

When you touch the recompute loop: **always** detach `OnItemPropertyChanged` before mutating `InclusionState`, then re-attach. Otherwise the mutation re-enters the recompute path and clobbers tracking.

## TreeView templating

One `HierarchicalDataTemplate` per row: a horizontal `StackPanel` with the checkbox and an optional "Details..." `Hyperlink` next to it. The hyperlink's `Visibility` is bound to `ImportTreeNodeViewModel.IsDetailsButtonVisible` (a `bool` `[DependsOn(nameof(StandardDiffRows))]` computed property) through a stock `BooleanToVisibilityConverter`, so it only shows on flagged-Standard rows. The hyperlink's `Command` reaches up to `ImportFromGumxViewModel.ShowStandardDiffCommand` via `RelativeSource AncestorType={x:Type UserControl}`, with the row VM passed as `CommandParameter`. Clicking opens `StandardDiffDetailsView` (a separate DialogService modal) which renders the row's `StandardDiffRows`.

A `Hyperlink` (inside a `TextBlock`) rather than a `Button` because a padded `Button` overflows the TreeViewItem row height and causes consecutive rows to visually overlap.

### Why no inline expander

Early attempts embedded an `Expander` directly in the TreeView row to show the diff in-place. That fought several layers at once:

1. **`HierarchicalDataTemplate.ItemTemplateSelector` is a plain CLR property, not a DependencyProperty** — `DynamicResource` is illegal there (`XamlParseException` at load); `StaticResource` works but creates a chicken-and-egg with templates that reference the selector.
2. **`TreeView.ItemTemplateSelector` only fires at the root level** — nested rows (children of folder nodes) fall back to the default template unless every `HierarchicalDataTemplate` *also* sets its own selector. No auto-cascade.
3. **The Frb theme defines a `HeaderTemplate` on `Expander` that treats Header content as data** (binds `{Binding}` and renders via `ToString`). A raw `CheckBox` in `<Expander.Header>` renders as `System.Windows.Controls.CheckBox Content:Foo IsChecked:True`, not as a control.

Each had a workaround, but stacking them in the same row produced a brittle XAML/theme arrangement. The "Details..." button + separate modal trades one extra click for zero theme conflicts and ~30 lines of XAML.

## Diff row generation

`ImportFromGumxViewModel.BuildDiffRows(StandardComparisonResult)` flattens:
- Added/removed categories → `StandardDiffRowViewModel("Category added"|"Category removed", name)`.
- Each `StandardVariableDiff` → one row with `Kind` mapped from `StandardVariableDiffKind` (Added/Removed/Changed) and `Summary = "{Variable} · {Field}: {Default} → {Project} · ..."`.

Returns `null` when there are no rows so the selector falls through to the default template and no expander is drawn.

## Testing

- `ImportFromGumxViewModelTests` (in `GumToolUnitTests`) uses `InitializeFromProjectForTesting(GumProjectSave)` to bypass the file/URL load and seed the source. The `_projectState` field exposes the destination (a `FakeProjectState` whose `GumProjectSave` is mutable) so tests can stage destination standards/components for diff and conflict scenarios.
- `GumxDependencyResolverTests` operate on the resolver directly without going through the VM.
- `ImportTreeNodeViewModelTests` lives in `Tests/Gum.ProjectServices.Tests` (**net8.0, no WPF stood up**) — it covers the node's `IsChecked` folder/child cascade and the `IsDetailsButtonVisible` flag. That the node VM is testable without WPF is the payoff of moving it into the headless assembly (#3229).

## History notes

- #2642 fixed user-explicit behaviors/standards being wiped on the dispatcher tick.
- #2644 added the conflict-resolution dialog (Skip / Overwrite All).
- #2779 added variable-level diff rendering for flagged Standards. The underlying diff infrastructure (`IStandardComparer` in `Tools/Gum.ProjectServices`) already existed for the CLI; this issue surfaces it in the UI.
