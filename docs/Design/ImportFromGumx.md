# Design: Import Components from .gumx File

## Overview

A new general-purpose plugin that allows importing components and screens from any `.gumx`
file, either from a local path or a remote URL. The existing GumFormsPlugin is migrated to
use the shared services introduced by this feature.

---

## Goals

- Point to a `.gumx` file (local path or URL) and preview what is available to import
- Allow the user to select individual components and screens to import
- Auto-discover transitive dependencies (components used by selected components, behaviors)
- Support importing/overwriting standards with a clear warning
- Migrate GumFormsPlugin away from embedded resources to a local file shipped with the tool

## Non-Goals (deferred)

- Automatically downloading or copying asset files (`.png`, fonts) — phase 2
- Handling `.ganx` animation files — deferred

---

## Plugin Structure

### New plugin: `ImportFromGumxPlugin`

A new plugin project alongside `GumFormsPlugin`. Adds the menu item:

> **Content → Import from .gumx...**

### Updated: `GumFormsPlugin`

- Removes all embedded resources
- Uses the shared `GumxSourceService` to load from a known local path
- Keeps its existing standards-overwrite logic and "already has forms" check
- The Forms `.gumx` and all associated files ship on disk with the tool at:
  ```
  {AppDir}/Content/DefaultFormsGumProject/DefaultFormsGumProject.gumx
  ```

---

## Dialog Flow

The import dialog has two phases.

### Phase 1 — Source & Preview

The user specifies a source and clicks **Load Preview**:

```
[ ● Local File ]  [ ○ URL ]
Path: [________________________________]  [Browse...]

                                   [Load Preview]
```

On load, the `.gumx` is parsed and all referenced element files are read to build a
dependency graph. A progress indicator is shown while loading.

### Phase 2 — Selection & Import

After loading, a flat list of all importable items is shown with three visual states per item:

```
 ☑  Controls/Button           (explicitly selected)
 ⊡  Elements/Icon             (auto — used by Controls/Button)
 ⊡  ButtonBehavior            (auto — used by Controls/Button)
 ☑  Controls/CheckBox
 ☐  Controls/ComboBox
 ☐  Screen: DemoScreen

 ─── Standards  ⚠ will overwrite existing ───────────────
 ☑  ColoredRectangle          (modified in source)
 ☐  Text                      (unmodified — same as current)

Destination subfolder:  [Controls/________________]

                                    [Cancel]  [Import]
```

#### Checkbox States

| State | `bool?` value | Meaning |
|-------|--------------|---------|
| ☑ | `true` | Explicitly selected by the user |
| ⊡ | `null` | Auto-included as a transitive dependency |
| ☐ | `false` | Not included |

- User can click ☐ → ☑ (explicit include)
- User can click ☑ → ☐ (deselect; item may return to ⊡ if still a dependency)
- User can click ⊡ → ☑ (promote auto-include to explicit)
- ⊡ state is only ever set programmatically by the VM, never by user cycling

#### What Appears in Each Section

| Item type | Appears as Direct? | Appears as Transitive? |
|-----------|-------------------|----------------------|
| Components | Yes (all available in source) | Yes (if dependency of selected item) |
| Screens | Yes | No |
| Behaviors | No | Yes (if referenced by a selected component) |
| Standards (`.gutx`) | Separate section only | — |
| Assets / fonts | Not shown (out of scope) | — |

Standards are shown in a visually separate section below the main list. They default to
☑ if referenced by any selected element and differ from the destination project's current
version, ☐ otherwise.

#### Destination Subfolder

- A single text box for the subfolder name, used for both components and screens:
  - Components → `{ProjectDir}/Components/{subfolder}/Button.gucx`
  - Screens → `{ProjectDir}/Screens/{subfolder}/DemoScreen.gusx`
- Defaults to the source `.gumx` filename without extension

---

## Dependency Analysis

When the preview loads, the `GumxDependencyResolver` reads each element's `InstanceSave`
entries and checks `BaseType` values against the source project's component list.

- If `BaseType` resolves to a component in the source project → component dependency
- If `BaseType` resolves to a behavior in the source project → behavior dependency
- If `BaseType` resolves to a standard → standard dependency (shown in standards section)
- Analysis is shallow per element but the closure is computed recursively

When the user changes the selection in the Direct section, the VM recomputes:

1. The full transitive closure of all checked items
2. Subtracts items already present in the destination project (excluded from transitive list)
3. Updates the standards section (which standards from source differ from destination)

The comparison for standards uses XML serialization, matching the existing technique in
`AddFormsViewModel.RemoveUnmodifiedAndUnusedStandards`.

---

## Import Ordering

To ensure base types are registered before the elements that reference them:

1. Standards (`.gutx`) — always imported first
2. Transitive components — topological order (leaves of dependency graph first)
3. Behaviors
4. Direct components
5. Direct screens
6. `TryAutoSaveProject()` followed by `LoadProject()`

All element imports call `IImportLogic` with `saveProject: false` until the final save step.

---

## New Classes

### `GumxSourceService`

Handles loading a `.gumx` from a local path or URL.

- `Task<GumProjectSave?> LoadProjectAsync(string pathOrUrl)`
- `Task<string?> FetchElementTextAsync(string relativeElementPath, string sourceBase)`
  — fetches a single `.gucx`/`.gusx`/`.behx`/`.gutx` relative to the source base path/URL
- `string NormalizeGitHubUrl(string url)`
  — converts `github.com/.../blob/...` URLs to `raw.githubusercontent.com` equivalents

The base path/URL for resolving relative element files is derived from the directory of the
`.gumx` file itself.

### `GumxDependencyResolver`

Builds a dependency graph from a loaded `GumProjectSave`.

- `DependencySet ComputeTransitive(IList<ElementSave> directSelected, GumProjectSave source, GumProjectSave destination)`
  — returns transitive components, behaviors, and differing standards; excludes items
  already present in the destination project

### `GumxImportService`

Orchestrates the actual import: writes files then registers them via `IImportLogic`.

- `Task ImportAsync(ImportSelections selections, GumProjectSave source, string sourceBase, string destinationSubfolder)`

### `ImportFromGumxViewModel : DialogViewModel`

| Property | Type | Notes |
|----------|------|-------|
| `SourcePath` | `string` | Local path or URL |
| `SourceType` | `enum` | `LocalFile \| Url` |
| `DestinationSubfolder` | `string` | Defaults to source project name |
| `IsPreviewLoaded` | `bool` | Controls phase 1 vs phase 2 visibility |
| `IsLoading` | `bool` | Shows progress indicator during fetch |
| `Items` | `ObservableCollection<ImportPreviewItemViewModel>` | All items, flat list |
| `LoadPreviewCommand` | `ICommand` | Fetches source and populates Items |
| `OnAffirmative()` | — | Calls `GumxImportService` |

### `ImportPreviewItemViewModel`

| Property | Type | Notes |
|----------|------|-------|
| `Name` | `string` | e.g. `Controls/Button` |
| `ElementType` | `enum` | `Component \| Screen \| Behavior \| Standard` |
| `InclusionState` | `enum` | `NotIncluded \| AutoIncluded \| Explicit` |
| `IsChecked` | `bool?` | Computed from `InclusionState`; setter drives recompute |
| `AutoIncludedReason` | `string` | e.g. `"used by Controls/Button"` — shown as tooltip |

---

## WPF Checkbox Implementation

Use `IsThreeState="True"` with a `bool?` binding to `IsChecked`.

The indeterminate state (`null`) represents auto-included. To prevent the user from
accidentally cycling into the indeterminate state via clicking, handle the `Indeterminate`
event in code-behind:

```csharp
private void OnCheckBoxIndeterminate(object sender, RoutedEventArgs e)
{
    // User clicked a checked item; WPF cycled to null. Force false instead.
    ((CheckBox)sender).IsChecked = false;
}
```

This handler fires only on user interaction, not when the VM programmatically sets
`IsChecked = null` (auto-included), so VM-driven state changes are unaffected.

Style the indeterminate state distinctly:

```xml
<Style TargetType="CheckBox" x:Key="ImportItemCheckBox">
    <Style.Triggers>
        <Trigger Property="IsChecked" Value="{x:Null}">
            <Setter Property="Foreground" Value="Gray"/>
            <Setter Property="ToolTip" Value="Auto-included as a dependency"/>
        </Trigger>
    </Style.Triggers>
</Style>
```

---

## GumFormsPlugin Migration

### File layout change

**Before:** ~100+ embedded resources compiled into the plugin `.dll`

**After:** Files ship on disk with the tool (kept at original path to avoid breaking other
projects that reference these files as embedded resources or by path):
```
{AppDir}/Content/FormsGumProject/
    GumProject.gumx
    Components/Controls/Button.gucx
    Components/Controls/CheckBox.gucx
    ...
    Behaviors/ButtonBehavior.behx
    ...
    Standards/ColoredRectangle.gutx
    ...
    FontCache/Font14Arial.fnt
    FontCache/Font14Arial_0.png
    ...
```

### FormsFileService change

Replace the embedded resource enumeration with a path lookup:

```csharp
string GetFormsGumxPath() =>
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
        "Content", "FormsGumProject", "GumProject.gumx");
```

Then use `GumxSourceService.LoadProjectAsync(path)` to load the project.

### What stays the same in GumFormsPlugin

- The "already has forms" check (look for existing component files in destination)
- The standards overwrite warning dialog
- The `AddFormsWindow` dialog (can be a subclass or consumer of `ImportFromGumxViewModel`)
- The `IsIncludeDemoScreenGum` option

---

## Implementation Phases

### Phase 1 — Core import

#### Done

- [x] `GumxSourceService` (local + URL loading, GitHub URL normalization, per-element HTTP fetch)
- [x] `GumxDependencyResolver` (transitive closure, standards diff vs. destination)
- [x] `GumxImportService` (topological import order, file write + name remapping, calls `IImportLogic`)
- [x] `ImportPreviewItemViewModel` with three-state checkbox and auto-include tooltip
- [x] `ImportFromGumxViewModel` with `LoadPreviewCommand`, `SelectAllComponentsCommand`, `OnAffirmative`
- [x] `ImportFromGumxView` — two-phase UI, ListBox scrolls in constrained window
- [x] `ImportFromGumxPlugin` — menu entry **Content → Import from .gumx...**
- [x] `GumFormsPlugin` migration — `FormsFileService` loads from `{AppDir}/Content/FormsGumProject/` on disk; no embedded resources

#### Deviations from original design

- No explicit **Load Preview** button. Instead: Browse auto-triggers the preview load; typing a
  path and pressing **Enter** also triggers it. This is equivalent in practice.
- Standards section has no visual separator (`─── Standards  ⚠ will overwrite existing ───`).
  Standards appear in the same flat list below behaviors, distinguished only by the `ElementType`
  label. Could be added as a future polish item.

#### Still needed before shipping

- [ ] **Manual end-to-end test** — open the dialog, point at a local `.gumx`, verify components
  appear, select some, import, confirm files land on disk and Gum registers them in the project
- [ ] **URL path test** — repeat the above with a raw GitHub URL
- [ ] **GumFormsPlugin smoke test** — verify the Forms files ship at
  `{AppDir}/Content/FormsGumProject/` after a normal build and that Add Forms still works
- [ ] **Overwrite behavior** — verify importing a component that already exists in the destination
  overwrites correctly without duplicating the project reference
- [ ] **Name-remapping correctness** — verify `BaseType` references inside imported XML are
  rewritten to the subfolder-prefixed name so instances resolve after import

### Phase 2 — Assets (follow-up)

- [x] Download/copy asset files (`.png`) referenced by imported components
  - `GumxSourceService.FetchBinaryAsync` handles local and URL sources
  - `GumxImportService` scans all imported elements for image path variables (extension-based), copies each once, skips existing files
  - After import, any missing assets are shown in the dialog with a warning; the button changes to **Close**
- [ ] Copy font files referenced by imported components
- [ ] Show asset dependency warnings in the dialog _before_ import (pre-flight check)
