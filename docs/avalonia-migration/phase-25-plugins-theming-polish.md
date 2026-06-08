# Phase 25 — Plugins, theming, polish

## Purpose
Bring the Avalonia head (`Gum.Avalonia`) to full cross-platform parity with the shipping WPF + WinForms tool. By this point the shell (Phase 10), editor canvas hosting (Phase 15), and property grid + views (Phase 20) work on the Avalonia head. This phase closes the remaining gaps: port or verify every plugin through the Phase 5 seams, re-author the WinForms project tree view as a native Avalonia `TreeView`, retire the WinForms helper projects for the Avalonia head, port the ~14 WPF theme resource dictionaries to Avalonia Styles/ControlThemes with dark/light parity, and run build + smoke tests on macOS and Linux. The headless core (`GumCommon`, `Gum.ProjectServices`, `ObjectFinder`) is shared, so save/load and codegen output must remain byte-identical across all three OSes.

## Decisions & rationale
- **Classify every plugin (clean / needs-port / deep-coupling / second-canvas) and route all UI through the Phase 5 seams. Reason:** an explicit audit surfaces the deep offenders — `MenuStripPlugin` (WinForms menu strip) and any tree-bound code — instead of discovering them mid-port, and routing through the panel-host/dialog/theming seams keeps each plugin head-agnostic. `TextureCoordinateSelectionPlugin` is in a category of its own (see next decision) and is not a generic deep-coupling row. **Direction:** produce a checked-off table; mechanical XAML re-author for "needs-port", dedicated re-authoring for "deep-coupling", the canvas-hosting solution for the second-canvas plugin, documented WPF-only exclusion otherwise.
- **Treat `TextureCoordinateSelectionPlugin` as a SECOND live XNA/GraphicsDevice canvas, not a deep-coupling panel. Reason:** its `Views\MainControl.xaml` hosts `FlatRedBall.SpecializedXnaControls.ImageRegionSelectionControl` (a separate WinForms project) inside a `WindowsFormsHost`, and that control owns its own `SystemManagers`, `Renderer.Camera`, XNA cursor, `ScrollBar`s, and overlay managers (Background / LineGrid / NineSliceGuide / TextureOutline) — i.e. it is the same rendering-surface problem solved for the main editor canvas in [Phase 12](phase-12-canvas-spike.md)/[Phase 15](phase-15-editor-canvas-hosting.md), instanced a second time, not a `UserControl` that re-routes through a seam. **Direction:** apply the Phase 12/15 canvas-hosting solution a second time (a second hosted XNA surface with its own `SystemManagers`/camera/input/overlays); budget its cost with the canvas work, not the plugin-port work. **Do not silently feature-flag it off** — the goal is full cutover, so the [Phase 30](phase-30-cutover.md) "zero deferred rows" gate forces an explicit decision: port it as a second canvas, or list it as a named cutover blocker. (Cross-reference Phase 12/15 in the plugin audit row.)
- **Replace the third-party WPF UI libraries, not just Gum's own dictionaries. Reason:** the theme/control budget is dominated by libraries that have no drop-in Avalonia equivalent. `MaterialDesignThemes` is woven directly into the theme dictionaries — `Frb.Styles.Defaults.xaml` merges `MaterialDesignThemes.Wpf` dictionaries and `BasedOn`s its styles (`MaterialDesignTextBox`, `MaterialDesignScrollBar`, `MaterialDesignScrollViewer`, `MaterialDesignToolBar`), and views use the `materialDesign:` namespace and styles like `MaterialDesignToolForegroundButton`. On top of that the tool pulls in `Xceed.Wpf.Toolkit`/`Xceed.Wpf.DataGrid`, `ControlzEx` (window chrome), and `Microsoft.Xaml.Behaviors`. Porting only the Gum-authored XAML would leave the Avalonia head referencing WPF-only packages. **Direction:** for each, pick an Avalonia replacement and re-author against it — reference the Phase 1 third-party UI dependency inventory ([phase-1](phase-1-scaffolding.md)) for the per-library decisions; Gum's `BasedOn`-MaterialDesign styles must be rebuilt on the Avalonia base theme, and Xceed/ControlzEx/Behaviors usages replaced with Avalonia-native equivalents (Fluent controls, Avalonia window chrome, `Avalonia.Xaml.Behaviors`).
- **Re-author the WinForms `MultiSelectTreeView` as a native Avalonia `TreeView` bound to the existing node model, and verify each replaced edge case explicitly. Reason:** this removes the `WindowsFormsHost` dependency (the last big WinForms surface in the shell) while keeping the existing selection, context-menu, and drag-drop handlers feeding the same downstream cascade. The WinForms original is rich and has **no automated coverage**, and it is deleted at [Phase 30](phase-30-cutover.md), so a single "TreeView: pass" feature-matrix row is not enough — every behavior it provides must be enumerated and checked individually before the original is removed. **Direction:** bind to the data `ElementTreeViewManager` already builds; reuse population logic, replace only the WinForms-specific node/image plumbing; verify multi-select, rubber-band/marquee selection, ctrl/shift-click range and toggle, drag-drop (including onto collapsed nodes), expansion-state persistence, and search/filter one by one.
- **Map the WPF dark/light dictionaries to Avalonia `ThemeVariant` + `ThemeDictionaries`, and script-convert the 357 KB `GumIcons.xaml`. Reason:** WPF `DynamicResource` maps ~1:1 to Avalonia dynamic resources and the dark/light split lines up cleanly with `ThemeVariant.Dark`/`Light` under `ResourceDictionary.ThemeDictionaries`; the icon file is far too large to hand-port and is best converted by script and spot-checked. **Direction:** port palette/brushes/styles by hand, generate the icon geometries.
- **Remove the Avalonia head's dependence on WinForms helpers, but do NOT delete them. Reason:** the WPF head must stay shippable throughout the migration, and it still backs `CommonFormsAndControls`/`InputLibrary`/`XnaAndWinforms`. **Direction:** strip these from `Gum.Avalonia`'s project graph only; the helpers stay in `GumFull.sln` and are deleted at [Phase 30](phase-30-cutover.md).

## Scope

### In scope
- **Plugin audit and port.** Walk every plugin (internal plugins under `Gum\Plugins\InternalPlugins\`, the external tool-side plugin projects under `Gum\*Plugin\` and `Tool\EditorTabPlugin_XNA\`) and confirm each routes its UI through the Phase 5 panel-host / dialog / theming seams. Port the ones still presenting WPF/WinForms `UserControl`s directly; flag the ones with deep framework coupling; route `TextureCoordinateSelectionPlugin` through the canvas-hosting work (it is a second XNA canvas, see below).
- **Second XNA canvas (`TextureCoordinateSelectionPlugin`).** Re-host its `ImageRegionSelectionControl` surface — its own `SystemManagers`, `Renderer.Camera`, XNA cursor, `ScrollBar`s, and overlay managers (Background / LineGrid / NineSliceGuide / TextureOutline) — by applying the Phase 12/15 canvas-hosting solution a second time. This is canvas-hosting cost, not plugin-port cost, and must be ported (or named as a [Phase 30](phase-30-cutover.md) cutover blocker), not feature-flagged off.
- **Third-party WPF UI library replacement.** Replace the WPF-only third-party UI libraries the tool depends on with Avalonia equivalents, per the Phase 1 inventory: `MaterialDesignThemes` (merged into and `BasedOn`'d by `Frb.Styles.Defaults.xaml`, and used via `materialDesign:` markup + styles such as `MaterialDesignToolForegroundButton` in plugin views), `Xceed.Wpf.Toolkit`/`Xceed.Wpf.DataGrid`, `ControlzEx` (window chrome), and `Microsoft.Xaml.Behaviors`. None has a drop-in Avalonia port; each feeds the theme/control budget alongside the Gum-authored dictionaries below.
- **Project tree view re-authoring.** Replace the WinForms `MultiSelectTreeView` (`Gum\CommonFormsAndControls\`) hosted via `WindowsFormsHost` with a native Avalonia `TreeView` bound to the existing tree view-model / node data used by `ElementTreeViewManager`, and individually verify the specific behaviors it provides (multi-select, rubber-band selection, ctrl/shift-click, drag-drop onto collapsed nodes, expansion-state persistence, search/filter) — there is no automated coverage and the original is deleted at Phase 30.
- **Retire WinForms helpers for the Avalonia head.** Remove `Gum.Avalonia`'s dependence on `CommonFormsAndControls`, `InputLibrary`, and `XnaAndWinforms` — their responsibilities move into the Avalonia tree view (here) and canvas hosting (Phase 15). The WPF head keeps using them unchanged.
- **Theming port.** Convert the 14 WPF resource dictionaries under `Gum\Themes\` to Avalonia `Styles` / `ControlThemes` plus the Fluent base theme, and provide the Avalonia implementation of the `IThemingService` seam (defined in Phase 5), driving the same dark/light theme switch the `ThemingDialog` exposes today. This includes replacing the `MaterialDesignThemes` foundation the Gum dictionaries `BasedOn` (the `Frb.Styles.Defaults.xaml` styles derive from `MaterialDesignTextBox`/`MaterialDesignScrollBar`/etc.) — porting only the Gum-authored XAML on top of MaterialDesign is not an option, the base must be rebuilt on the Avalonia theme.
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
3. **Deep coupling** — flag explicitly. Known offenders: `MenuStripPlugin` (WinForms menu strip) and anything binding straight to `MultiSelectTreeView` (`TreeView` plugin). These get dedicated re-authoring (the tree view has its own task below).
4. **Second canvas** — `TextureCoordinateSelectionPlugin` only. Its `Views\MainControl.xaml` hosts `FlatRedBall.SpecializedXnaControls.ImageRegionSelectionControl` (a separate WinForms project) in a `WindowsFormsHost`, and that control owns its own `SystemManagers`, `Renderer.Camera`, XNA cursor, `ScrollBar`s, and overlay managers (Background / LineGrid / NineSliceGuide / TextureOutline). It is a second live XNA/GraphicsDevice surface, so it is solved by applying the [Phase 12](phase-12-canvas-spike.md)/[Phase 15](phase-15-editor-canvas-hosting.md) canvas-hosting solution a second time, not by a XAML re-author. Its cost belongs with the canvas work. It must be **ported (second canvas)** or recorded as a **named [Phase 30](phase-30-cutover.md) cutover blocker** — it must not be silently feature-flagged off, because the goal is full cutover and Phase 30 allows zero deferred rows.

Deliverable: a checked-off table of all plugins with state (clean / needs-port / deep-coupling / second-canvas), the seam(s) each uses, and — for the second-canvas plugin — a cross-reference to Phase 12/15. Any plugin still importing `System.Windows`, `System.Windows.Forms`, or `WindowsFormsHost` for the Avalonia head is a defect to fix or a documented WPF-only exclusion.

### Third-party WPF UI library replacement
Porting the Gum-authored dictionaries (Theming task) is necessary but not sufficient: the tool depends on WPF-only third-party UI libraries that are woven into those dictionaries and into plugin views, and none has a drop-in Avalonia port. Using the Phase 1 third-party UI dependency inventory ([phase-1](phase-1-scaffolding.md)) as the source of per-library decisions, replace:
- **`MaterialDesignThemes`** — the largest entanglement. `Frb.Styles.Defaults.xaml` merges `pack://...MaterialDesignThemes.Wpf;component/...` dictionaries and `BasedOn`s their styles (`MaterialDesignTextBox`, `MaterialDesignScrollBar`, `MaterialDesignScrollViewer`, `MaterialDesignToolBar`); plugin views use the `materialDesign:` namespace and styles such as `MaterialDesignToolForegroundButton` (e.g. `TextureCoordinateSelectionPlugin\Views\MainControl.xaml`). Rebuild these Gum styles on the Avalonia base theme rather than on MaterialDesign.
- **`Xceed.Wpf.Toolkit` / `Xceed.Wpf.DataGrid`** — replace each used control with an Avalonia-native equivalent (Fluent controls / `DataGrid`).
- **`ControlzEx`** — window chrome; replace with Avalonia's window-chrome support.
- **`Microsoft.Xaml.Behaviors`** — replace with `Avalonia.Xaml.Behaviors`.

Deliverable: no WPF-only third-party UI package remains in the `Gum.Avalonia` graph; each replaced library has a recorded Avalonia target matching the Phase 1 inventory decision.

### Project tree view
The project/hierarchy explorer is today a WinForms `MultiSelectTreeView` (`Gum\CommonFormsAndControls\MultiSelectTreeView.cs` ~38 KB, plus `MultiSelectTreeView.Theming.cs` ~48 KB) hosted via `WindowsFormsHost` from `Gum\MainWindow.xaml.cs` and driven by `Gum\Plugins\InternalPlugins\TreeView\` (`ElementTreeViewManager`, `ElementTreeViewCreator`, `TreeViewStateService`, `CollapseToggleService`).

Re-author it as a native Avalonia `TreeView`:
- Bind to the existing node data the tree manager already builds; keep the node model (icons, names, hierarchy) so right-click context menus, drag/drop, and selection events feed the same downstream handlers.
- Wire icons via the ported `GumIcons` resources (see Theming) rather than the WinForms `ImageList`.
- Keep the selection-sync cascade intact: selecting a node still raises the plugin selection events and syncs canvas + property grid.

Because the WinForms `MultiSelectTreeView` is a rich custom control with **no automated coverage**, and it is deleted at [Phase 30](phase-30-cutover.md), a single "TreeView works" check is insufficient. Enumerate the specific behaviors it provides and verify each one explicitly on the Avalonia control:
- **Multi-select** — selecting multiple nodes (the WinForms control's whole reason for existing) via Avalonia's selection model.
- **Rubber-band / marquee selection** — click-drag in empty space selects the enclosed nodes.
- **Ctrl-click / Shift-click** — Ctrl toggles individual nodes; Shift extends a contiguous range; combinations behave as in the WinForms control.
- **Drag-drop, including onto collapsed nodes** — dropping onto a collapsed node retargets/expands correctly (a known WinForms edge case), not just onto expanded ones.
- **Expansion-state persistence** — `ITreeViewStateService` / `ICollapseToggleService` restore the prior expand/collapse state across refresh and reload.
- **Search / filter** — the search/filter behavior backed by `FlatSearchListBox.xaml` narrows the tree and clears back correctly.

The Avalonia tree binds to the same VM-facing data the WPF head uses, so `ElementTreeViewManager`'s population logic should be largely reusable; the WinForms-specific node/image plumbing is what gets replaced. Each behavior above is its own checklist row below — do not collapse them into one "TreeView: pass" entry.

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
- **Control styles** (`Frb.Styles*.xaml`, `Frb.Buttons.xaml`, `Frb.Styles.Defaults.xaml`, `MainPanelControl.Styles.xaml`) → Avalonia `Styles` / `ControlTheme` layered on `Avalonia.Themes.Fluent`. WPF `Style`/`Setter`/`ControlTemplate` and triggers convert to Avalonia selectors + pseudo-classes; `DynamicResource` largely maps 1:1. **Note the MaterialDesign foundation:** `Frb.Styles.Defaults.xaml` merges `MaterialDesignThemes.Wpf` dictionaries and its styles `BasedOn` `MaterialDesignTextBox`/`MaterialDesignScrollBar`/`MaterialDesignScrollViewer`/`MaterialDesignToolBar`. These bases do not exist on Avalonia, so the Gum styles must be re-based on the Avalonia/Fluent theme — this is part of the third-party replacement task above, not a mechanical dictionary conversion.
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

Second XNA canvas (`TextureCoordinateSelectionPlugin`):
- `Gum\TextureCoordinateSelectionPlugin\Views\MainControl.xaml` — hosts `ImageRegionSelectionControl` in a `WindowsFormsHost` (plus `materialDesign:` markup and `MaterialDesignToolForegroundButton`).
- `FlatRedBall.SpecializedXnaControls\ImageRegionSelectionControl.cs` — the second XNA surface: owns its own `SystemManagers`, `Renderer.Camera`, XNA cursor, and overlay managers (Background/LineGrid/NineSliceGuide/TextureOutline). Re-host via the Phase 12/15 canvas-hosting solution.

Third-party WPF UI libraries to replace (see Phase 1 inventory):
- `MaterialDesignThemes` — merged into and `BasedOn`'d by `Gum\Themes\Frb.Styles.Defaults.xaml`; `materialDesign:` markup in plugin views.
- `Xceed.Wpf.Toolkit` / `Xceed.Wpf.DataGrid`, `ControlzEx` (window chrome), `Microsoft.Xaml.Behaviors`.

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
- **`TextureCoordinateSelectionPlugin` is a second canvas, and its cost is canvas-sized.** Its `MainControl.xaml` hosts `ImageRegionSelectionControl` (separate WinForms project) in a `WindowsFormsHost` with its own `SystemManagers`, `Renderer.Camera`, XNA cursor, `ScrollBar`s, and Background/LineGrid/NineSliceGuide/TextureOutline overlay managers — the same rendering-surface problem as the main editor canvas, instanced twice. The risk is under-scoping it as a generic plugin port: budget it with the Phase 12/15 canvas work, not the plugin-port work. Because the goal is full cutover, it must be **ported as a second canvas or named as a [Phase 30](phase-30-cutover.md) cutover blocker** — *not* silently feature-flagged off (Phase 30 allows zero deferred rows).
- **Plugins with deep WPF/WinForms coupling.** `MenuStripPlugin` (WinForms menu) and any tree-bound plugin need real re-authoring, not a mechanical XAML port. Budget for these explicitly. Any such plugin left WPF-only here must be resolved (ported or dropped) by [Phase 30](phase-30-cutover.md), because after cutover there is no WPF head to host it.
- **Third-party WPF UI libraries have no drop-in Avalonia port.** `MaterialDesignThemes`, `Xceed.Wpf.Toolkit`/`Xceed.Wpf.DataGrid`, `ControlzEx`, and `Microsoft.Xaml.Behaviors` are WPF-only and are woven into both the theme dictionaries (Gum's styles `BasedOn` MaterialDesign) and plugin views (`materialDesign:` markup, `MaterialDesignToolForegroundButton`). Porting only Gum's own XAML leaves the Avalonia head referencing WPF-only packages, which the Phase 7 purity guard rejects. Treat these as a distinct line item driven by the Phase 1 inventory, separate from converting the Gum dictionaries — re-basing the MaterialDesign-derived styles onto the Avalonia theme is the largest and fiddliest part.
- **Re-authored tree view has no automated coverage to lean on.** The WinForms `MultiSelectTreeView` has no tests and is deleted at Phase 30, so behaviors it provides (multi-select, rubber-band, ctrl/shift-click, drag-drop onto collapsed nodes, expansion persistence, search) must be verified individually before the original is removed. A single "TreeView: pass" row will let a missing edge case ship; the checklist enumerates each one.
- **Keep WPF/WinForms out of the cross-platform graph.** Any transitive `CommonFormsAndControls`/`InputLibrary`/`XnaAndWinforms` reference will break macOS/Linux builds. The build check that fails on `UseWindowsForms`/`UseWPF` (or `*-windows` TFMs) in the Avalonia graph is the **Phase 7** cross-platform-purity guard — this phase relies on it catching any WinForms/WPF reference that creeps back in while plugins and the tree view are ported.
- **Byte-identical output is the parity gate.** Save and codegen come from the shared headless core; any diff in saved-file or generated-code bytes between heads/OSes is a regression, most likely from line endings, culture-sensitive formatting, or path handling — verify with a byte compare, not a visual diff.
- **`ObjectFinder.Self` stays static** (CLAUDE.md) across both heads; do not refactor it during this phase.
- **CI matrix, packaging, and cutover are owned by other phases.** The "Cross-platform validation" task here assumes a cross-platform CI matrix exists but does not stand it up — that matrix (Windows/macOS/Linux build + the cross-platform-purity guard) is **Phase 7**. Per-OS installers/code-signing/notarization are **Phase 27**. The decision is already made (full cutover, see plan.md), and the actual retirement of the WPF head + re-pointing every solution/workflow at the Avalonia head is **[Phase 30](phase-30-cutover.md)**. Phase 25 is the functional parity finish line; it depends transitively on the spine prior phases (1 → 5 → 10 → 15 → 20) completing, and Phases 27 and 30 in turn depend on this phase.

## Done / verification checklist
- [ ] Plugin audit table complete: every internal + external plugin classified (clean / needs-port / deep-coupling / second-canvas) with the seam(s) it uses.
- [ ] All "needs-port" plugins ported to Avalonia views routed through the Phase 5 panel-host/dialog seams.
- [ ] Deep-coupling plugins (`MenuStripPlugin`, tree-bound) re-authored or explicitly documented as WPF-only.
- [ ] `TextureCoordinateSelectionPlugin` (second XNA canvas) ported via the Phase 12/15 canvas-hosting solution — its own `SystemManagers`/`Renderer.Camera`/cursor/scrollbars/overlays hosted — OR recorded as a named Phase 30 cutover blocker. Not silently feature-flagged off.
- [ ] Third-party WPF UI libraries replaced per the Phase 1 inventory: `MaterialDesignThemes`, `Xceed.Wpf.Toolkit`/`Xceed.Wpf.DataGrid`, `ControlzEx`, `Microsoft.Xaml.Behaviors` — no WPF-only third-party UI package remains in the `Gum.Avalonia` graph.
- [ ] Project tree view re-authored as an Avalonia `TreeView`; selection-sync cascade and right-click context menus working.
- [ ] Tree view edge cases verified individually (not as one row): multi-select; rubber-band/marquee selection; ctrl-click toggle + shift-click range; drag-drop including onto collapsed nodes; expansion-state persistence across refresh/reload; search/filter narrowing and clearing.
- [ ] `Gum.Avalonia` project graph has no reference (direct or transitive) to `CommonFormsAndControls`, `InputLibrary`, or `XnaAndWinforms`, and no `UseWPF`/`UseWindowsForms`.
- [ ] All 14 `Gum\Themes\` dictionaries ported to Avalonia Styles/ControlThemes; the MaterialDesign-derived (`BasedOn`) styles re-based on the Avalonia theme; `GumIcons` available as Avalonia geometries.
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
