# Phase 25 — Plugins, theming, polish

## Purpose
Bring the Avalonia head (`Gum.Avalonia`) to full cross-platform parity with the shipping WPF + WinForms tool. By this point the shell (Phase 10), editor canvas hosting (Phase 15), and property grid + views (Phase 20) work on the Avalonia head. This phase closes the remaining gaps: port or verify every plugin through the Phase 5 seams, re-author the WinForms project tree view as a native Avalonia `TreeView`, retire the WinForms helper projects for the Avalonia head, port the ~14 WPF theme resource dictionaries to Avalonia Styles/ControlThemes with dark/light parity, and run build + smoke tests on macOS and Linux. The headless core (`GumCommon`, `Gum.ProjectServices`, `ObjectFinder`) is shared, so save/load and codegen output must remain byte-identical across all three OSes.

## Decisions & rationale
- **Classify every plugin (clean / needs-port / deep-coupling) and route all UI through the Phase 5 seams. Reason:** an explicit audit surfaces the deep offenders — `TextureCoordinateSelectionPlugin` (`WindowsFormsHost`), `MenuStripPlugin` (WinForms menu strip), and any tree-bound code — instead of discovering them mid-port, and routing through the panel-host/dialog/theming seams keeps each plugin head-agnostic. **Direction:** produce a checked-off table; mechanical XAML re-author for "needs-port", dedicated re-authoring for "deep-coupling", documented WPF-only exclusion otherwise.
- **Re-author the WinForms `MultiSelectTreeView` as a native Avalonia `TreeView` bound to the existing node model. Reason:** this removes the `WindowsFormsHost` dependency (the last big WinForms surface in the shell) while keeping the existing selection, context-menu, and drag-drop handlers feeding the same downstream cascade. **Direction:** bind to the data `ElementTreeViewManager` already builds; reuse population logic, replace only the WinForms-specific node/image plumbing.
- **Map the WPF dark/light dictionaries to Avalonia `ThemeVariant` + `ThemeDictionaries`, and script-convert the 357 KB `GumIcons.xaml`. Reason:** WPF `DynamicResource` maps ~1:1 to Avalonia dynamic resources and the dark/light split lines up cleanly with `ThemeVariant.Dark`/`Light` under `ResourceDictionary.ThemeDictionaries`; the icon file is far too large to hand-port and is best converted by script and spot-checked. **Direction:** port palette/brushes/styles by hand, generate the icon geometries.
- **Remove the Avalonia head's dependence on WinForms helpers, but do NOT delete them. Reason:** the WPF head must stay shippable throughout the migration, and it still backs `CommonFormsAndControls`/`InputLibrary`/`XnaAndWinforms`. **Direction:** strip these from `Gum.Avalonia`'s project graph only; the helpers stay in `GumFull.sln` and are deleted at [Phase 30](phase-30-cutover.md).

## Scope

### In scope
- **Plugin audit and port.** Walk every plugin (internal plugins under `Gum\Plugins\InternalPlugins\`, the external tool-side plugin projects under `Gum\*Plugin\` and `Tool\EditorTabPlugin_XNA\`) and confirm each routes its UI through the Phase 5 panel-host / dialog / theming seams. Port the ones still presenting WPF/WinForms `UserControl`s directly; flag the ones with deep framework coupling.
- **Project tree view re-authoring.** Replace the WinForms `MultiSelectTreeView` (`Gum\CommonFormsAndControls\`) hosted via `WindowsFormsHost` with a native Avalonia `TreeView` bound to the existing tree view-model / node data used by `ElementTreeViewManager`.
- **Retire WinForms helpers for the Avalonia head.** Remove `Gum.Avalonia`'s dependence on `CommonFormsAndControls`, `InputLibrary`, and `XnaAndWinforms` — their responsibilities move into the Avalonia tree view (here) and canvas hosting (Phase 15). The WPF head keeps using them unchanged.
- **Theming port.** Convert the 14 WPF resource dictionaries under `Gum\Themes\` to Avalonia `Styles` / `ControlThemes` plus the Fluent base theme, and provide the Avalonia implementation of the `IThemingService` seam (defined in Phase 5), driving the same dark/light theme switch the `ThemingDialog` exposes today.
- **Cross-platform validation.** Build and smoke-test `Gum.Avalonia` on macOS and Linux, not just Windows.
- **Parity checklist.** Run the full editor flow (load `.gumx`, select/move/resize, edit a variable, undo/redo, save, codegen) on Windows + macOS + Linux and confirm parity with the WPF tool, including byte-identical save and codegen output.

### Out of scope
- New editor features. This phase is parity and platform reach only.
- Re-architecting the headless core or `ObjectFinder.Self` (intentionally still a static singleton per CLAUDE.md).
- Changing the WPF + WinForms tool's behavior. During the migration `GumFull.sln` continues to build and ship the existing tool exactly as before; retiring the WPF head is [Phase 30](phase-30-cutover.md), not this phase.
- Replacing the seam interfaces themselves (that was Phase 5); this phase only adds Avalonia implementations and consumers.
- Mobile/web Avalonia heads. Target is cross-platform desktop (Windows/macOS/Linux) only.
- **Packaging, installers, code-signing, and CI pipeline** for the per-OS Avalonia head (macOS `.app`/`.dmg`/notarization, Linux AppImage/`.deb`, Windows installer), plus the decision of *when* to make the Avalonia head the default download and retire the WPF head. This phase proves parity; the PR-time CI build matrix + purity guard are owned by **Phase 7**, and the release-time packaging/signing/distribution and cutover decision are owned by **Phase 27**.

## Tasks

### Plugin audit
Enumerate and classify every plugin, then verify routing through the Phase 5 seams (panel host for docked/tabbed UI, dialog service for windows, theming service for resources).

Internal plugins live under `Gum\Plugins\InternalPlugins\` (22 folders): `AlignmentButtons`, `Behaviors`, `CirclePlugin`, `Delete`, `DuplicateVariablePlugin`, `Errors`, `FavoriteComponentPlugin`, `FileWatchPlugin`, `HideShowTools`, `Hotkey`, `Inheritance`, `LoadRecentFilesPlugin`, `MenuStripPlugin`, `NineSlicePlugin`, `Output`, `ParentPlugin`, `ProjectPropertiesWindowPlugin`, `StatePlugin`, `SvgExportPlugin`, `TreeView`, `Undos`, `VariableGrid`. Plus the in-tree `ImportPlugin` and `Fonts` under `Gum\Plugins\`.

External tool-side plugin projects (separate csprojs, copied into `Gum\bin\<Config>\Plugins\...` via post-build):
- `Gum\CodeOutputPlugin\CodeOutputPlugin.csproj`
- `Gum\EventOutputPlugin\EventOutputPlugin.csproj`
- `Gum\GumFormsPlugin\GumFormsPlugin.csproj`
- `Gum\ImportFromGumxPlugin\ImportFromGumxPlugin.csproj`
- `Gum\PerformanceMeasurementPlugin\PerformanceMeasurementPlugin.csproj`
- `Gum\StateAnimationPlugin\StateAnimationPlugin.csproj`
- `Gum\SvgPlugin\SkiaPlugin.csproj` (folder `SvgPlugin`, assembly `SkiaPlugin`)
- `Gum\TextureCoordinateSelectionPlugin\TextureCoordinateSelectionPlugin.csproj`
- `Tool\EditorTabPlugin_XNA\EditorTabPlugin_XNA.csproj` (the editor canvas itself, ~49 `.cs` files) — ported in Phase 15; verify here only.

For each plugin, record one of three states:
1. **Clean** — already presents via view-models + seam-routed views; works on the Avalonia head with at most a small XAML re-author. Examples: plugins whose `.xaml` already live under `Views\` folders (`VariableGrid`, `StatePlugin`, `Errors`, `Output`, `AlignmentButtons`, `LoadRecentFilesPlugin`, `Behaviors`, `ProjectPropertiesWindowPlugin`).
2. **Needs port** — presents a WPF `UserControl`/`Window` directly; port the view to Avalonia AXAML against the same VM and route through the panel-host/dialog seam.
3. **Deep coupling** — flag explicitly. Known offenders: `TextureCoordinateSelectionPlugin` (uses `WindowsFormsHost`), `MenuStripPlugin` (WinForms menu strip), and anything binding straight to `MultiSelectTreeView` (`TreeView` plugin). These get dedicated re-authoring (the tree view has its own task below).

Deliverable: a checked-off table of all plugins with state and the seam(s) each uses. Any plugin still importing `System.Windows`, `System.Windows.Forms`, or `WindowsFormsHost` for the Avalonia head is a defect to fix or a documented WPF-only exclusion.

### Project tree view
The project/hierarchy explorer is today a WinForms `MultiSelectTreeView` (`Gum\CommonFormsAndControls\MultiSelectTreeView.cs` ~38 KB, plus `MultiSelectTreeView.Theming.cs` ~48 KB) hosted via `WindowsFormsHost` from `Gum\MainWindow.xaml.cs` and driven by `Gum\Plugins\InternalPlugins\TreeView\` (`ElementTreeViewManager`, `ElementTreeViewCreator`, `TreeViewStateService`, `CollapseToggleService`).

Re-author it as a native Avalonia `TreeView`:
- Bind to the existing node data the tree manager already builds; keep the node model (icons, names, hierarchy) so right-click context menus, drag/drop, and selection events feed the same downstream handlers.
- Preserve multi-select semantics (the WinForms control's whole reason for existing) using Avalonia's selection model.
- Honor `ITreeViewStateService` / `ICollapseToggleService` (expansion/collapse persistence) and the search/filter behavior backed by `FlatSearchListBox.xaml`.
- Wire icons via the ported `GumIcons` resources (see Theming) rather than the WinForms `ImageList`.
- Keep the selection-sync cascade intact: selecting a node still raises the plugin selection events and syncs canvas + property grid.

The Avalonia tree binds to the same VM-facing data the WPF head uses, so `ElementTreeViewManager`'s population logic should be largely reusable; the WinForms-specific node/image plumbing is what gets replaced.

### Retire WinForms helpers
For the Avalonia head only, eliminate references to:
- `Gum\CommonFormsAndControls\CommonFormsAndControls.csproj` (`<UseWindowsForms>`) — contains `MultiSelectTreeView.cs` + `MultiSelectTreeView.Theming.cs` (replaced above) and the framework-neutral `EnumerableExtensionMethods.cs`. Move `EnumerableExtensionMethods` into a neutral library (or `GumCommon`) so the Avalonia head can use it; drop the WinForms `MultiSelectTreeView`.
- `InputLibrary\InputLibrary.csproj` — `Cursor.cs`, `Keyboard.cs`. Input now flows through Avalonia / the Phase 15 canvas-hosting input layer. Port any logic the canvas still needs into the Avalonia input path.
- `XnaAndWinforms\XnaAndWinforms.csproj` — `GraphicsDeviceControl`, `GraphicsDeviceService`, `ServiceContainer`, `RenderingError`. Its WinForms graphics-device hosting is superseded by Phase 15 canvas hosting; retire it from the Avalonia graph.

During the migration these projects stay in `GumFull.sln` and continue to back the WPF head (the WPF head must remain shippable until cutover). They are **retired at [Phase 30](phase-30-cutover.md)** once the Avalonia head is the sole head — this phase does not delete them, it only removes the Avalonia head's dependence on them. The goal here is that `Gum.Avalonia`'s project graph contains no `UseWindowsForms`/`UseWPF` reference, transitive or direct. Verify with a build that fails fast on any WinForms/WPF reference creeping into the cross-platform graph.

### Theming
Today's theming uses WPF `DynamicResource` / merged `ResourceDictionary`s. The 14 dictionaries under `Gum\Themes\`:
`Converters.xaml`, `Frb.Accents.xaml`, `Frb.Brushes.xaml`, `Frb.Brushes.Dark.xaml`, `Frb.Brushes.Light.xaml`, `Frb.Buttons.xaml`, `Frb.Converters.xaml`, `Frb.Styles.xaml`, `Frb.Styles.Defaults.xaml` (~98 KB — the bulk), `Frb.Styles.Variants.xaml`, `Frb.Theming.xaml`, `GumIcons.xaml` (~357 KB of vector icon geometries), `MainPanelControl.Styles.xaml`, and `Palette.xaml`.

Port plan:
- **Palette + brushes** (`Palette.xaml`, `Frb.Brushes*.xaml`, `Frb.Accents.xaml`) → Avalonia resource dictionaries. Dark/light split (`Frb.Brushes.Dark.xaml` / `Frb.Brushes.Light.xaml`) maps to Avalonia theme variants (`ThemeVariant.Dark` / `ThemeVariant.Light`) under a `ResourceDictionary.ThemeDictionaries`.
- **Control styles** (`Frb.Styles*.xaml`, `Frb.Buttons.xaml`, `Frb.Styles.Defaults.xaml`, `MainPanelControl.Styles.xaml`) → Avalonia `Styles` / `ControlTheme` layered on `Avalonia.Themes.Fluent`. WPF `Style`/`Setter`/`ControlTemplate` and triggers convert to Avalonia selectors + pseudo-classes; `DynamicResource` largely maps 1:1.
- **Icons** (`GumIcons.xaml`, codegen helper `GumIconKind.g.cs`) → Avalonia geometries/`PathIcon` resources. Large file; consider scripted conversion of the `<Geometry>`/`PathData` entries.
- **Converters** (`Converters.xaml`, `Frb.Converters.xaml`) → Avalonia `IValueConverter` resources (most converter classes are framework-neutral and can be shared).
- **Theming service.** Implement the Avalonia `IThemingService` (seam from Phase 5; the WPF side and `ThemingDialogViewModel` already consume the abstraction) to set `Application.RequestedThemeVariant` and persist the choice, replacing the WPF `DynamicResource`-swap mechanism. Drive it from the existing `ThemingDialog`.
- Goal: dark and light parity with the WPF tool — same accent, same panel chrome, same icon set.

### Cross-platform validation
- Build `Gum.Avalonia` on macOS and Linux (CI runners and/or local). The Avalonia head must target `net8.0` (not `net8.0-windows`) and pull in no WPF/WinForms transitively, so it should build clean on non-Windows.
- Smoke test launch + open a project on each OS; confirm the canvas renders (Phase 15 backend), tree view populates, property grid edits, and theming switches.
- Confirm font/DPI rendering is acceptable per OS (Avalonia `Inter` font + Fluent handle most of this).

### Cross-platform OS integration (must-fix, currently Windows-only)
These touch points are verified Windows-only today and are currently unowned by any other phase. Each must get a cross-platform implementation or documented fallback before parity is claimed.
- **OS dark-mode detection.** `Gum\Dialogs\ThemingDialogViewModel.cs:431-443` (`IsSystemInDarkMode`) reads the Windows registry (`HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize`, value `AppsUseLightTheme`). The registry path does not exist on macOS/Linux. The Avalonia `IThemingService` implementation must detect system dark mode cross-platform via Avalonia's `PlatformSettings.GetColorValues()` (`PlatformThemeVariant`) / `Application.ActualThemeVariant`, **not** the registry. Route the "follow OS theme" behavior through that.
- **File watching.** `Gum\Plugins\InternalPlugins\FileWatchPlugin\FileWatchManager.cs` uses `FileSystemWatcher` (line 44, created line 120-122), which behaves differently on Linux (inotify watch limits, missed events under load) and macOS (FSEvents coalescing/latency). Validate external-edit pickup on each OS and either confirm reliability or document the degraded behavior.
- **AppData / settings paths.** Settings persistence uses `Environment.SpecialFolder.ApplicationData` / `UserApplicationDataForThisApplication` (e.g. `Builder.cs:44`, `Gum\StateAnimationPlugin\Managers\SettingsManager.cs:21`, `Gum\Plugins\PluginManager.cs:105`). These resolve to different per-OS locations (`%APPDATA%` vs `~/.config` / `~/Library/Application Support`). Confirm tool settings, plugin settings, and recent-files all write and reload from the correct per-OS appdata location, watching for case-sensitivity issues on Linux (folder/file name casing must match exactly).
- **Drag-and-drop.** `IDragDropManager` / `DragDropManager` plus the tree-view and variable-grid drop targets use WPF `DataObject` / `DragDrop`, which do not exist on the Avalonia head. Port these to Avalonia's drag-drop (`DragDrop.DoDragDrop`, `DataObject`, `AddHandler(DragDrop.DropEvent, ...)`). (Canvas drop targets are owned by Phase 15.)
- **macOS global menu bar.** The application menu must appear in the macOS global menu bar via Avalonia `NativeMenu`, not as an in-window menu strip. The `MenuStripPlugin` re-author must target `NativeMenu` on macOS.
- **Hotkeys / accelerators.** `IHotkeyManager` and the `Hotkey` internal plugin define keyboard accelerators with Windows modifier conventions. On macOS the primary modifier is Cmd (not Ctrl); map modifiers per-OS so accelerators (e.g. save, copy/paste, undo) reach parity and feel native.

### Parity checklist (vs WPF tool, per OS)
Run end to end and compare to the WPF tool:
- Load a `.gumx` project; hierarchy tree matches.
- Select an element; canvas highlight + property grid populate.
- Move / resize / rotate a selected instance on the canvas.
- Edit a variable in the property grid; canvas updates live.
- Undo / redo the edit.
- Save the project — saved `.gumx`/`.gucx`/etc. files are byte-identical to the WPF tool's output (shared headless save classes).
- Trigger codegen (CodeOutputPlugin) — generated code is byte-identical to the WPF tool's output (shared codegen core).

## Key files & projects

Plugins:
- `Gum\Plugins\PluginManager.cs`, `IPluginManager.cs`, `IPlugin.cs`, `PluginContainer.cs`, `PluginTab.cs` — plugin host/registration; UI registration goes through the Phase 5 panel-host seam.
- `Gum\Plugins\InternalPlugins\` — 22 internal plugin folders (see Plugin audit).
- External plugin csprojs: `Gum\CodeOutputPlugin\`, `Gum\EventOutputPlugin\`, `Gum\GumFormsPlugin\`, `Gum\ImportFromGumxPlugin\`, `Gum\PerformanceMeasurementPlugin\`, `Gum\StateAnimationPlugin\`, `Gum\SvgPlugin\` (assembly `SkiaPlugin`), `Gum\TextureCoordinateSelectionPlugin\`, `Tool\EditorTabPlugin_XNA\`.

Tree view:
- `Gum\CommonFormsAndControls\MultiSelectTreeView.cs` + `MultiSelectTreeView.Theming.cs` — WinForms control being replaced.
- `Gum\Plugins\InternalPlugins\TreeView\ElementTreeViewManager.cs`, `ElementTreeViewCreator.cs`, `TreeViewStateService.cs` (+ `ITreeViewStateService.cs`), `CollapseToggleService.cs` (+ `ICollapseToggleService.cs`), `FlatSearchListBox.xaml` — tree population/state logic to reuse.
- `Gum\MainWindow.xaml.cs` — current `WindowsFormsHost` host site (WPF head).

Helpers to retire (Avalonia head):
- `Gum\CommonFormsAndControls\CommonFormsAndControls.csproj`
- `InputLibrary\InputLibrary.csproj`
- `XnaAndWinforms\XnaAndWinforms.csproj`

Theming:
- `Gum\Themes\` — the 14 `.xaml` dictionaries listed above, plus `GumIconKind.g.cs`.
- `Gum\Dialogs\ThemingDialogView.xaml` + `ThemingDialogViewModel.cs` — theme-switch UI consuming `IThemingService`.
- `Gum\Controls\ThemedScrollbar.cs`, `Gum\Controls\ThemedScrollContainer.cs` — themed control references to check during the port.

## Dependencies
- **Needs Phase 10 (Avalonia shell skeleton)** — the window + `TabControl`/`GridSplitter` panel layout the ported plugins and tree view attach to.
- **Needs Phase 15 (Editor canvas hosting)** — `EditorTabPlugin_XNA` already ported and the cross-platform input path in place, so retiring `InputLibrary`/`XnaAndWinforms` is safe.
- **Needs Phase 20 (Property grid + views)** — the property grid and shared views the tree-view selection cascade feeds.
- **Builds on Phase 5 seams** — panel host, dialog service, and `IThemingService` interfaces this phase implements for Avalonia.
- This is the functional-parity finish line. Packaging is [Phase 27](phase-27-packaging-distribution.md) and the final WPF retirement/cutover is [Phase 30](phase-30-cutover.md), both of which depend on this phase.

## Risks & notes
- **`$(SolutionDir)` plugin post-build caveat (CLAUDE.md).** The external tool-side plugin projects post-build-copy their DLLs into `$(SolutionDir)Gum\bin\<Config>\Plugins\...`, which only resolves when building through the solution. The WPF tool must keep building via `GumFull.sln`. When wiring these plugins for the Avalonia head, do not break or reorder the existing post-build entries; the Avalonia head needs its own plugin-discovery/copy story rather than relying on the WPF tool's `$(SolutionDir)` post-builds.
- **Per-OS quirks.** Menus: macOS uses a global menu bar — the `MenuStripPlugin` re-author must use Avalonia's `NativeMenu`/menu so it lands in the macOS menu bar. File dialogs, path separators, and case-sensitive filesystems (Linux) can surface bugs the Windows-only tool never hit. DPI/font hinting differs per OS; verify icon and text crispness.
- **Theme parity is fiddly.** WPF triggers, `MultiTrigger`, and implicit-key styles do not map 1:1 to Avalonia selectors/pseudo-classes. `Frb.Styles.Defaults.xaml` (~98 KB) is the largest risk surface; expect manual reconciliation. `GumIcons.xaml` (~357 KB) is best converted with a script and spot-checked, not hand-ported.
- **Plugins with deep WPF/WinForms coupling.** `TextureCoordinateSelectionPlugin` (`WindowsFormsHost` inside its `MainControl.xaml`), `MenuStripPlugin` (WinForms menu), and any tree-bound plugin need real re-authoring, not a mechanical XAML port. Budget for these explicitly and consider feature-flagging them off the Avalonia head until ported rather than blocking the release. Any such plugin left WPF-only/flagged-off here must be resolved (ported or dropped) by [Phase 30](phase-30-cutover.md), because after cutover there is no WPF head to host it.
- **Keep WPF/WinForms out of the cross-platform graph.** Any transitive `CommonFormsAndControls`/`InputLibrary`/`XnaAndWinforms` reference will break macOS/Linux builds. The build check that fails on `UseWindowsForms`/`UseWPF` (or `*-windows` TFMs) in the Avalonia graph is the **Phase 7** cross-platform-purity guard — this phase relies on it catching any WinForms/WPF reference that creeps back in while plugins and the tree view are ported.
- **Byte-identical output is the parity gate.** Save and codegen come from the shared headless core; any diff in saved-file or generated-code bytes between heads/OSes is a regression, most likely from line endings, culture-sensitive formatting, or path handling — verify with a byte compare, not a visual diff.
- **`ObjectFinder.Self` stays static** (CLAUDE.md) across both heads; do not refactor it during this phase.
- **CI matrix, packaging, and cutover are owned by other phases.** The "Cross-platform validation" task here assumes a cross-platform CI matrix exists but does not stand it up — that matrix (Windows/macOS/Linux build + the cross-platform-purity guard) is **Phase 7**. Per-OS installers/code-signing/notarization are **Phase 27**. The decision is already made (full cutover, see plan.md), and the actual retirement of the WPF head + re-pointing every solution/workflow at the Avalonia head is **[Phase 30](phase-30-cutover.md)**. Phase 25 is the functional parity finish line; it depends transitively on the spine prior phases (1 → 5 → 10 → 15 → 20) completing, and Phases 27 and 30 in turn depend on this phase.

## Done / verification checklist
- [ ] Plugin audit table complete: every internal + external plugin classified (clean / needs-port / deep-coupling) with the seam(s) it uses.
- [ ] All "needs-port" plugins ported to Avalonia views routed through the Phase 5 panel-host/dialog seams.
- [ ] Deep-coupling plugins (`TextureCoordinateSelectionPlugin`, `MenuStripPlugin`, tree-bound) re-authored or explicitly documented as WPF-only / feature-flagged off.
- [ ] Project tree view re-authored as an Avalonia `TreeView` with multi-select, context menus, drag/drop, expansion-state persistence, search/filter, and selection-sync cascade working.
- [ ] `Gum.Avalonia` project graph has no reference (direct or transitive) to `CommonFormsAndControls`, `InputLibrary`, or `XnaAndWinforms`, and no `UseWPF`/`UseWindowsForms`.
- [ ] All 14 `Gum\Themes\` dictionaries ported to Avalonia Styles/ControlThemes; `GumIcons` available as Avalonia geometries.
- [ ] Avalonia `IThemingService` implemented; dark/light switch via `ThemingDialog` reaches parity with the WPF tool.
- [ ] `Gum.Avalonia` builds clean on Windows, macOS, and Linux.
- [ ] Smoke test passes on Windows, macOS, and Linux (launch, open project, canvas renders, tree populates, property grid edits, theme switches).
- [ ] OS dark-mode detection works cross-platform via Avalonia `PlatformSettings` / `ThemeVariant` (not the Windows registry); "follow OS theme" flips correctly on macOS and Linux.
- [ ] File-watch picks up external edits on Linux and macOS (or degraded behavior is documented).
- [ ] Settings, plugin settings, and recent-files persist and reload from the correct per-OS appdata location on all three OSes (no case-sensitivity breakage on Linux).
- [ ] Drag-and-drop works on the Avalonia head: drag a component from the tree into a container, and reorder items via drag (tree + variable grid).
- [ ] On macOS the application menu appears in the global menu bar (`NativeMenu`), not as an in-window menu.
- [ ] Hotkeys/accelerators map correctly per-OS (Cmd vs Ctrl on macOS) with accelerator parity to the WPF tool.
- [ ] **Full parity run on Windows:** load `.gumx`, select, move/resize/rotate, edit variable, undo/redo, save, codegen.
- [ ] **Full parity run on macOS:** same steps.
- [ ] **Full parity run on Linux:** same steps.
- [ ] Saved project files are byte-identical to the WPF tool's output (all three OSes).
- [ ] Generated code (CodeOutputPlugin) is byte-identical to the WPF tool's output (all three OSes).
- [ ] `GumFull.sln` (WPF + WinForms tool) still builds and ships unchanged, with plugin DLL output under `Gum\bin\<Config>\Plugins\` unaffected.
