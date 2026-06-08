# Phase 5 — Extract UI-framework seams

## Purpose

Make the existing WPF Gum tool depend on UI-neutral interfaces at every UI boundary, with a WPF implementation of each seam wired in first so there is **zero behavior change**. This proves each abstraction inside the shipping WPF build before any Avalonia code exists. The phase also splits the single `AddGum()` composition root into a shared, UI-neutral `AddGumCore()` plus a per-head `AddGumWpf()` (and a stubbed `AddGumAvalonia()` for later), pushing every WPF-typed registration into the WPF-specific extension.

The success criterion is concrete and binary: after this phase, `dotnet build GumFull.sln` succeeds and the WPF tool behaves identically to before — same tabs, dialogs, theming, property grid, and plugin registration.

## Decisions & rationale

- **Decision:** Abstraction-first, but the WPF implementation lands first with zero behavior change. **Reason:** Defining a seam and immediately wiring a WPF impl behind it proves the abstraction inside the shipping build before any Avalonia code exists, so a defective contract surfaces as a compile/behavior failure in a build we can actually run today. **Direction:** Success is binary — `dotnet build GumFull.sln` is green and the WPF tool behaves identically (same tabs, dialogs, theming, property grid, plugin registration). Treat any observable delta as a regression, not a "fix."
- **Decision:** Split the single composition root into `AddGumCore()` (UI-neutral) + `AddGumWpf()` (WPF head) + a stub `AddGumAvalonia()`, with `AddGumCore()` and its helpers moved to a neutral shared assembly. **Reason:** This shape lets the compiler and the DI container enforce head purity — `AddGumCore()` becomes the single, audited "no `System.Windows`" surface, and the Avalonia head can compose the exact same core services WITHOUT referencing the WPF `Gum` project. It also matches the repo's existing constructor-injection direction. **Direction:** Move `AddGumCore()` (and `ForEachConcreteTypeAssignableTo` / `AddViewModelFuncFactories`) into the neutral shared assembly (`Gum.UiAbstractions` if it stays dependency-light, else a small `Gum.Composition` project); keep `AddGumWpf()` in `Gum`. Change `file static` → `public`/`internal` as needed. The purity guarantee is enforced by a real test (see checklist), not a grep.
- **Decision:** Plugin tab/panel content flows through a neutral `IPanelContent` handle, and `MainPanelViewModel` is the **WPF implementation of `ITabManager`** — registered ONLY in `AddGumWpf()`, not reused as-is by Avalonia. **Reason:** `MainPanelViewModel` holds six `System.Windows.Data.ICollectionView` collections, performs `WindowsFormsHost` wrapping, and materializes `FrameworkElement` — all of which are WPF-only. Naming it the WPF `ITabManager` impl (rather than a shared component) keeps those types out of the neutral surface. **Direction:** Neutralize `PluginTab.Content` / `CustomHeaderContent` and the `Func<FrameworkElement, PluginTab>` factory to the `IPanelContent` handle; extract a neutral base view-model (column widths, the `IPanelContent`-typed tab model, teardown / layout persistence) shared by both heads, with a thin per-head subclass owning the framework-specific bits. The Avalonia head gets its OWN `ITabManager` implementation (Phase 10).
- **Decision:** Property grid is contract-only this phase (`IPropertyGrid` + `IDataUi`); the Avalonia impl is Phase 20. **Reason:** Defining the contract while only the WPF impl exists means a contract defect surfaces in the green WPF build, before any second implementation can mask it. **Direction:** Express the contract in neutral terms over the data-driven member model and wrap the existing `DataUiGrid`/`MainPropertyGrid` so the Variables tab is byte-for-byte unchanged. The neutral member model (`MemberCategory`/`InstanceMember`) currently carries `System.Windows` usings, so any neutral mirror or relocation must NOT carry WPF types into `Gum.UiAbstractions` — the relocate-vs-mirror choice is a Phase 20 decision.

## Scope

### In scope

- Defining UI-neutral seam interfaces (Dispatcher, Dialogs, Theming, Panel/tab host, Property grid, plus the OS-integration seams: save/folder pickers, clipboard, reveal-in-file-manager) in the `Gum.UiAbstractions` project (`Gum.UiAbstractions\`, at the repo root) created in Phase 1.
- Providing a **WPF implementation** of each new/changed seam and registering it through DI.
- Removing UI-framework type leakage from the interfaces themselves: `System.Windows.Window` from `IDialogService`, `System.Windows.FrameworkElement` / `System.Windows.Forms.Control` from `ITabManager` and `PluginTab`.
- Removing the stray `Application.Current.Dispatcher` reference in `Builder.cs` by routing it through a WPF-head factory.
- Splitting `Builder.cs` `AddGum()` into `AddGumCore()` + `AddGumWpf()` (and a placeholder `AddGumAvalonia()`), and updating `GumBuilder.CreateHostBuilder` to call the WPF head extension.

### Out of scope

- Any Avalonia control, view, or window. `AddGumAvalonia()` is a stub/placeholder only.
- Re-implementing the Variables property grid on a new framework — this phase only **defines** the `IPropertyGrid` contract; the full per-head implementation is Phase 20.
- Hosting the XNA editor canvas on a new framework (Phase 15) and the Avalonia shell skeleton (Phase 10).
- Changing plugin authoring APIs in a breaking way. The public plugin tab entry points `PluginBase.CreateTab(...)` (`Gum\Plugins\BaseClasses\PluginBase.cs:346`) and `PluginBase.AddControl(...)` (`:354`) — both currently typed `System.Windows.FrameworkElement` — and all internal-plugin call sites must keep working.
- Theming visual redesign — only the contract/implementation boundary is touched.

## Tasks

### 1. Dispatcher seam

The seam already exists and is already UI-neutral.

- `c:\git\Gum\Gum\Services\IDispatcher.cs` — `Invoke(Action)` / `Post(Action)`. Confirm this covers every usage in the tool (grep for `IDispatcher` consumers); no member additions expected.
- `c:\git\Gum\Gum\Services\AppDispatcher.cs` — wraps `System.Windows.Threading.Dispatcher` (ctor takes `Func<Dispatcher>`). This is the WPF implementation; it stays in the WPF tool project (`Gum\`) and is registered only from the WPF head, not the core. (Physically moving it to a separate head assembly is a later concern — in this phase it just must not be registered by `AddGumCore()`.)
- **Remove the stray WPF reference in the core registration.** `c:\git\Gum\Gum\Services\Builder.cs:183` registers `new AppDispatcher(() => Application.Current.Dispatcher)`. Move this registration into `AddGumWpf()` so `AddGumCore()` has no `System.Windows` dependency. The only `Application.Current.Dispatcher` reference in tool source is this line (verified by grep), so removing it here clears the dispatcher seam entirely.

### 2. Dialogs seam

- `c:\git\Gum\Gum\Services\Dialogs\DialogService.cs` — the `IDialogService` interface (lines 11–18) is already free of `System.Windows.Window`: `ShowMessage`, `Show<T>`, `GetUserString`, `OpenFile` all use neutral types. **No interface change needed** — confirm and document this.
- The leakage is entirely in the `internal class DialogService` implementation (same file): `System.Windows.Window`, `Application.Current.MainWindow`, `WindowInteropHelper.EnsureHandle()`, `DialogWindow`, `WindowStartupLocation`, and `Microsoft.Win32.OpenFileDialog` (see `CreateDialogWindow`, `OpenFile`). Keep this whole class as the **WPF implementation**.
- The `AddDialogs()` helper (`Builder.cs:188`) registers two things: `IDialogViewResolver -> DialogViewResolver` (line 190) and `IDialogService -> DialogService` (line 191).
- Keep `IDialogViewResolver` / `DialogViewResolver` and `DialogAttribute` mapping — they are already view-agnostic. The resolver picks the View type per DialogViewModel; an Avalonia head will supply its own resolver/registrations later.
- Action: move the `IDialogService -> DialogService` registration (`Builder.cs:191`) into `AddGumWpf()`. The `IDialogViewResolver -> DialogViewResolver` registration stays in core (or in the head if it carries WPF view types — verify the resolver's dependencies during implementation). Splitting `AddDialogs()` accordingly is acceptable.

### 3. Theming seam

- `c:\git\Gum\Gum\Dialogs\ThemingDialogViewModel.cs` — `IThemingService` (lines 251–263) and `IEffectiveThemeSettings` (lines 265–275) are **already UI-neutral**: they expose `System.Drawing.Color`, `ThemeMode`, `IsSystemInDarkMode`, and `ApplyInitialTheme()`. No `Application.Current.Resources` on the contract. **No interface change needed.**
- The WPF coupling is in `class ThemingService : IThemingService` (line 278): pack URIs (`pack://application:,,,/Gum;component/Themes/Frb.Brushes.*.xaml`) and resource-dictionary swapping via `Application.Current`. Keep this as the WPF implementation and register it in `AddGumWpf()` (currently `Builder.cs:185`).
- Confirm consumers go through the interface only: `Gum\Program.cs:109` (`ApplyInitialTheme()`), `Gum\Controls\ThemedScrollbar.cs:97`, the texture-coordinate and XNA-editor `BackgroundManager` classes, and `MainEditorTabPlugin.cs`. These already depend on `IThemingService` / `IEffectiveThemeSettings`, so they remain head-agnostic.
- Note: `IThemingService` itself is fine; the related `IUiSettingsService` (`c:\git\Gum\Gum\Services\IUiSettingsService.cs`, just `BaseFontSize`) is neutral but its impl `UiSettingsService.cs` touches `Application.Current.Resources` — move that registration to `AddGumWpf()` too (`Builder.cs:184`).

### 4. Panel / tab host seam

This is the largest change — it is where the UI-framework types actually leak through an interface.

- `c:\git\Gum\Gum\Managers\ITabManager.cs` leaks both `System.Windows.Forms.Control` and `System.Windows.FrameworkElement`:
  ```
  PluginTab AddControl(System.Windows.Forms.Control control, string tabTitle, TabLocation tabLocation);
  PluginTab AddControl(FrameworkElement element, string tabTitle, TabLocation tabLocation = TabLocation.CenterBottom);
  ```
- `c:\git\Gum\Gum\ViewModels\MainPanelViewModel.cs` (implements `ITabManager`) leaks the same types. The WinForms `AddControl` overload wraps the control in a `WindowsFormsHost` (line 108). The `FrameworkElement` overload (lines 110–124) contains the airspace hack that sets `host.Margin` when the element is itself a `WindowsFormsHost` **and** the tab title is `"Editor"` (lines 116–120) — see the note below about this branch.
- `c:\git\Gum\Gum\Plugins\PluginTab.cs` exposes `FrameworkElement Content` (line 29, read-only) and `FrameworkElement? CustomHeaderContent` (line 35), takes a `FrameworkElement` in its ctor (line 85), and derives from the neutral `Gum.Mvvm.ViewModel`. `MainPanelViewModel` builds tabs through a `Func<FrameworkElement, PluginTab>` factory (`MainPanelViewModel.cs:43` field, `:59` ctor param). That factory is synthesized by `AddViewModelFuncFactories` (`Builder.cs:182`) because `PluginTab` is a `ViewModel` picked up by the transient scan.

**Approach:** introduce a neutral "panel content handle" so plugins register content without naming a UI framework.

- Define a neutral content abstraction in `Gum.UiAbstractions`, e.g. `IPanelContent` (an opaque handle, or `object Content` plus a discriminator for "native WinForms control" vs "framework element"). The head is responsible for materializing it into a real control.
- Re-declare `ITabManager.AddControl(...)` overloads in terms of the neutral handle (one path for an arbitrary native control, one for a framework-neutral view object), keeping `TabLocation` and `PluginTab` return type.
- Change `PluginTab.Content` / `CustomHeaderContent` to the neutral handle type and update the factory from `Func<FrameworkElement, PluginTab>` to the neutral handle type.
- **`MainPanelViewModel` is the WPF implementation of `ITabManager` — it is NOT reused unchanged by Avalonia.** It holds six `System.Windows.Data.ICollectionView` collections, does `WindowsFormsHost` wrapping, and materializes `FrameworkElement`, so it is registered **only** in `AddGumWpf()`. The decision (locked with Phase 10): extract a **neutral base view-model** — column widths, the `IPanelContent`-typed tab model, teardown / layout persistence — shared by both heads, plus a **thin per-head subclass** owning the framework-specific bits. The WPF subclass converts the neutral handle into a `FrameworkElement` / `WindowsFormsHost` and keeps the `ICollectionView` plumbing (`CenterView`, `RightBottomView`, `RightTopView`, `CenterTopView`, `CenterBottomView`, `LeftView`); the Avalonia head gets its **own** `ITabManager` subclass (Phase 10). The `MainPanelControl.xaml` binding to `PluginTab.Content` must still resolve to a `FrameworkElement` in the WPF head — do this via a converter/template, not by leaking WPF onto the interface.
- The airspace hack (the `WindowsFormsHost`+`tabTitle == "Editor"` branch, lines 110–124) stays inside the WPF impl as a WPF-only detail. Document that it disappears entirely under Avalonia (no WinForms interop / airspace problem there). (See the airspace note in Risks: this branch does not actually fire for the real Editor tab, which passes a `Grid`.)
- **`ICollectionView` → tab-view adaptation ownership.** `MainPanelViewModel` exposes its six tab collections as WPF `ICollectionView` (`CenterView`/`RightBottomView`/`RightTopView`/`CenterTopView`/`CenterBottomView`/`LeftView`), which Avalonia has no equivalent for. **The decision and implementation of how the Avalonia head consumes these lives in Phase 10** (Phase 10 Task 6: default to direct-binding the `ICollectionView` as an `IEnumerable`+`INotifyCollectionChanged`; escalate to a neutral filtered-tab-view seam only if live filtering / selection round-trip misbehaves). **If** that neutral seam is needed, it is part of *this* phase's panel/tab-host seam family (it belongs in `Gum.UiAbstractions` alongside the panel-content handle and changes the shared `MainPanelViewModel`), so it must be coordinated back here. Phase 5 does not pre-build it; it just owns it if option (b) is taken. Phase 10 and Phase 5 agree: try direct-bind first (Phase 10), promote to a Phase 5 seam only if forced.
- **Do not break plugin registration.** Verify all `_tabManager.AddControl(...)` / `PluginBase` call sites still compile and behave identically. The complete set in this repo (verified by grep):
  - Public plugin surface: `Gum\Plugins\BaseClasses\PluginBase.cs` — `CreateTab(...)` (`:346`, calls `_tabManager.AddControl` at `:348`) and `AddControl(...)` (`:354`, calls at `:356`). Both take `System.Windows.FrameworkElement` only; there is **no** WinForms overload on `PluginBase`.
  - Direct `_tabManager.AddControl(...)` internal call sites:
    - `Gum\Plugins\InternalPlugins\VariableGrid\PropertyGridManager.cs:170` ("Variables", passes the WPF `MainPropertyGrid`)
    - `Gum\Plugins\InternalPlugins\TreeView\ElementTreeViewManager.cs:765` ("Project")
    - `Gum\Plugins\InternalPlugins\Output\MainOutputPlugin.cs:19`, `Errors\MainErrorsPlugin.cs:81`, `StatePlugin\MainStatePlugin.cs:124`, `Behaviors\MainBehaviorsPlugin.cs:50`, `FileWatchPlugin\MainFileWatchPlugin.cs:45`, `ProjectPropertiesWindowPlugin\MainPropertiesWindowPlugin.cs:82`, `AlignmentButtons\AlignmentMainPlugin.cs:24`
    - `Gum\CodeOutputPlugin\MainCodeOutputPlugin.cs:485`, `Gum\StateAnimationPlugin\MainStateAnimationPlugin.cs:301`, `Gum\TextureCoordinateSelectionPlugin\Logic\ControlLogic.cs:139`
    - `Tool\EditorTabPlugin_XNA\MainEditorTabPlugin.cs:1166` — the `"Editor"` tab. **It passes a `System.Windows.Controls.Grid` (a `FrameworkElement`) that has a `WindowsFormsHost` nested *inside* it** (the host wraps `gumEditorPanel`, the XNA canvas; see `MainEditorTabPlugin.cs:1152-1166`). Because the argument is a `Grid`, not a bare `WindowsFormsHost`, the airspace branch in `MainPanelViewModel.AddControl` (line 116, `element is WindowsFormsHost host && tabTitle == "Editor"`) does **not** fire for this call — see the airspace note in Risks.
  - Sites that go through `PluginBase` (not directly through `_tabManager`): `Gum\PerformanceMeasurementPlugin\MainPlugin.cs:34` and `Gum\Plugins\InternalPlugins\Undos\MainPlugin.cs:22` both call `PluginBase.AddControl(FrameworkElement, ...)`.

  Internal sites pass either `System.Windows.Forms.Control` (the WinForms `AddControl` overload) or WPF `FrameworkElement`s. The WPF head's `AddControl` overloads must continue to accept those exact types (wrapping WinForms in `WindowsFormsHost`) so every call site compiles and behaves identically. Preserve the `PluginBase.CreateTab`/`AddControl` `FrameworkElement` signatures.

### 5. Property grid contract

This phase **defines the contract only**; the Avalonia implementation is Phase 20.

- The Variables tab is built in `c:\git\Gum\Gum\Plugins\InternalPlugins\VariableGrid\PropertyGridManager.cs` (`mainControl = new Gum.MainPropertyGrid()` at line 168, then `_tabManager.AddControl(mainControl, "Variables", ...)` at line 170; `mVariablesDataGrid = mainControl.DataGrid` at line 172, typed `WpfDataUi.DataUiGrid` — field declared at line 48). The grid itself is the WpfDataUi `DataUiGrid` (`c:\git\Gum\WpfDataUi\DataUiGrid.cs`), driven by a `MemberCategory` / `InstanceMember` member model (the category type is `WpfDataUi.DataTypes.MemberCategory` — there is no `DataUiCategory` type).
- The concrete surface `PropertyGridManager` uses on `DataUiGrid` (the contract `IPropertyGrid` must cover): `Instance` (set; `DataUiGrid.cs:73`), `Categories` (`BulkObservableCollection<MemberCategory>`; `:122`), `SetCategories(IList<MemberCategory>)` (`:189`), `SetMultipleCategoryLists(List<List<MemberCategory>>)` (`:665`), `Refresh()` (`:575`), and `IsEnabled`. See `PropertyGridManager.cs:306,380,401,405,423,476-483,504` for the call sites.
- Define an `IPropertyGrid` seam (in `Gum.UiAbstractions`) over this **data-driven member model**, not over the WPF control. Caveat: both `MemberCategory` and `InstanceMember` currently live in `WpfDataUi` and carry WPF usings (`System.Windows`, `System.Windows.Controls`, `System.Windows.Media`), so they are **not** drop-in neutral types. The contract should be expressed in neutral terms (set/replace the categorized member lists, set the bound instance, refresh, expose change events); whether `MemberCategory`/`InstanceMember` can be relocated to a neutral assembly or need a thin neutral mirror is a decision for Phase 20 — this phase only fixes the boundary.
- **Per-editor `IDataUi` contract.** The grid drives each member editor through the `IDataUi` interface (`c:\git\Gum\WpfDataUi\IDataUi.cs`), not the WPF base class. This phase establishes `IDataUi` (or a relocated/neutral equivalent in `Gum.UiAbstractions`) as the canonical editor contract that the grid talks to, so the Avalonia editors Phase 20 authors implement the same contract. Like `MemberCategory`/`InstanceMember`, `IDataUi` currently lives in `WpfDataUi`; whether it is relocated to a neutral assembly or mirrored is a Phase 20 implementation decision — this phase only names it as the seam boundary so Phase 20's "Phase 5 equivalent" of `IDataUi` is owned and not orphaned.
- The WPF implementation of `IPropertyGrid` wraps the existing `DataUiGrid`/`MainPropertyGrid` so the Variables tab is byte-for-byte the same in this phase. **No visual or behavioral change.**
- Document that an Avalonia `IPropertyGrid` implementation (a new data-driven grid) plus Avalonia `IDataUi` editors are the Phase 20 deliverable; this phase just locks the boundary so `PropertyGridManager` no longer references `DataUiGrid` directly (or references it only through the WPF head).

### 6. OS-integration seams (save/folder pickers, clipboard, reveal-in-file-manager)

Several cross-platform OS-integration surfaces bypass the existing seams and call Windows-only APIs directly. Add a seam for each with a **WPF implementation now**; the Avalonia implementations are deferred (Phase 20 for save pickers that ride `IStorageProvider`, Phase 25 for the rest). All call sites below are verified.

- **Save / folder pickers.** `IDialogService` only exposes `OpenFile` — it has no save path. The tool constructs native save dialogs directly at:
  - `c:\git\Gum\Gum\Managers\ProjectManager.cs:852` — `new SaveFileDialog()` (WinForms; "Where would you like to save the Gum project?", `*.gumx`).
  - `c:\git\Gum\Gum\Plugins\InternalPlugins\SvgExportPlugin\MainSvgExportPlugin.cs:75` — `new Microsoft.Win32.SaveFileDialog { ... }` (`*.svg`).

  Add `SaveFile(...)` to `IDialogService` (and a folder picker if a call site needs one) and route both sites through it. **Flag for Phase 20:** the Avalonia implementation uses `IStorageProvider`, which is **async**, while the WPF `SaveFileDialog` is synchronous. The sync-vs-async signature of `SaveFile(...)` on the neutral interface is a Phase 20 decision — record it here so the WPF seam shape does not silently lock out the async Avalonia impl.

- **Clipboard.** `System.Windows.Clipboard` is called from 5+ sites, including non-view shared logic that should not name a UI framework:
  - Shared logic (route through the seam): `c:\git\Gum\Gum\Plugins\InternalPlugins\VariableGrid\CompositeMemberLogic.cs:235` and `c:\git\Gum\Gum\Plugins\InternalPlugins\VariableGrid\StateReferencingInstanceMember.cs:540`.
  - View / code-behind sites: `c:\git\Gum\Gum\Controls\TitleFilePathDisplay.xaml.cs:59`, `c:\git\Gum\Gum\Plugins\InternalPlugins\TreeView\ElementTreeViewManager.RightClick.cs:127`, `c:\git\Gum\Gum\Services\Dialogs\DialogWindow.xaml.cs:75`.

  Add an `IClipboardService` seam in `Gum.UiAbstractions` (WPF impl now, registered in `AddGumWpf()`) and route at minimum the non-view shared-logic call sites through it. View code-behind sites may stay on `System.Windows.Clipboard` in the WPF head, but routing them through the seam is preferred where it is cheap.

- **Reveal in file manager.** `Process.Start("explorer.exe", "/select," + ...)` is Windows-only and appears at:
  - `c:\git\Gum\Gum\Managers\ProjectManager.cs:914`
  - `c:\git\Gum\Gum\Plugins\InternalPlugins\TreeView\ElementTreeViewManager.RightClick.cs:195`

  Add a small "reveal file / open folder" seam (e.g. `IFileSystemRevealService`) with a WPF/Windows impl now (`explorer.exe /select,`). Per-OS implementations come later: `open -R` / `open` on macOS, `xdg-open` on Linux.

### 7. DI composition split (`AddGumCore` / `AddGumWpf` / `AddGumAvalonia`)

- `c:\git\Gum\Gum\Services\Builder.cs` currently has one `AddGum()` (the `ServiceCollectionExtensions.AddGum` method, lines 75–186) called from `GumBuilder.CreateHostBuilder` via `services.AddGum()` (line 66).
- Split into:
  - `AddGumCore()` — everything UI-neutral: the `ViewModel` transient scan (lines 78–81), `Lazy<>`/`Lazier<>` (line 82), all logic/services/managers singletons, `IObjectFinder`/`PluginManager`/`IMessenger` (lines 86–88, 173), `IDialogViewResolver` (from `AddDialogs`, line 190), `AddViewModelFuncFactories` (line 182), etc. Note: `IObjectFinder` stays a core singleton wrapping `ObjectFinder.Self` — that static singleton is intentional repo-wide and is not changed here.
  - `AddGumWpf()` — every WPF-typed registration: `IDispatcher -> AppDispatcher(() => Application.Current.Dispatcher)` (line 183), `IUiSettingsService -> UiSettingsService` (line 184), `IThemingService -> ThemingService` (line 185), `IDialogService -> DialogService` (from `AddDialogs`, line 191), `MainPanelViewModel` + `ITabManager -> MainPanelViewModel` (lines 175–176), `MainWindow` (line 177), the new WPF `IPropertyGrid` implementation, and the new OS-integration seam impls from Task 6 (`IClipboardService`, `IFileSystemRevealService`, and the `SaveFile` path on the WPF `IDialogService`). (`MainWindowViewModel`, line 178, is a plain `ViewModel` and could stay in core; place it wherever its dependencies resolve — verify during implementation.)
  - `AddGumAvalonia()` — stub that throws `NotImplementedException` (or registers nothing yet) so the split is structurally in place for Phase 10.
- `GumBuilder.CreateHostBuilder` (line 66) changes from `services.AddGum();` to `services.AddGumCore(); services.AddGumWpf();`.
- Decide where these extension methods live: they are currently in a `file static class ServiceCollectionExtensions` (line 73), which is invisible outside `Builder.cs`. If a separate Avalonia head assembly must call `AddGumCore()`, that scoping must change to `public`/`internal` in a shared location — verify accessibility during implementation. `ForEachConcreteTypeAssignableTo` and `AddViewModelFuncFactories` (the other `file static` helpers, lines 202, 229) are core helpers and must move with `AddGumCore`.

## Key files & projects

- `c:\git\Gum\Gum\Services\Builder.cs` — DI composition root to split.
- `c:\git\Gum\Gum\Services\IDispatcher.cs` — dispatcher seam (already neutral).
- `c:\git\Gum\Gum\Services\AppDispatcher.cs` — WPF dispatcher impl.
- `c:\git\Gum\Gum\Services\Dialogs\DialogService.cs` — `IDialogService` (neutral) + WPF impl.
- `c:\git\Gum\Gum\Dialogs\ThemingDialogViewModel.cs` — `IThemingService` / `IEffectiveThemeSettings` (neutral) + `ThemingService` (WPF impl).
- `c:\git\Gum\Gum\Services\IUiSettingsService.cs` / `UiSettingsService.cs` — neutral contract, WPF impl.
- `c:\git\Gum\Gum\Managers\ITabManager.cs` — tab/panel host contract (leaks WPF/WinForms today).
- `c:\git\Gum\Gum\ViewModels\MainPanelViewModel.cs` — WPF `ITabManager` impl, airspace hack.
- `c:\git\Gum\Gum\Plugins\PluginTab.cs` — tab content model (leaks `FrameworkElement`).
- `c:\git\Gum\Gum\Plugins\BaseClasses\PluginBase.cs` — public plugin `AddControl` entry points.
- `c:\git\Gum\Gum\Plugins\InternalPlugins\VariableGrid\PropertyGridManager.cs` + `c:\git\Gum\WpfDataUi\DataUiGrid.cs` — property grid contract source.
- `c:\git\Gum\Gum\Managers\ProjectManager.cs` (`:852` save dialog, `:914` reveal) and `c:\git\Gum\Gum\Plugins\InternalPlugins\SvgExportPlugin\MainSvgExportPlugin.cs:75` — OS-integration call sites that bypass the seams today (save picker, reveal-in-file-manager).
- `c:\git\Gum\Gum\Plugins\InternalPlugins\VariableGrid\CompositeMemberLogic.cs:235`, `c:\git\Gum\Gum\Plugins\InternalPlugins\VariableGrid\StateReferencingInstanceMember.cs:540`, `c:\git\Gum\Gum\Plugins\InternalPlugins\TreeView\ElementTreeViewManager.RightClick.cs:127,195` — clipboard / reveal call sites to route through the new seams.
- `Gum.UiAbstractions\Gum.UiAbstractions.csproj` — target project at the repo root (created empty in Phase 1) for all neutral seam interfaces.
- `GumFull.sln` — the build target that must stay green.

## Dependencies

- **Needs Phase 1 (Scaffolding):** the `Gum.UiAbstractions` project must exist and be referenced by the tool so the neutral interfaces have a home.
- **Unblocks Phase 10 (Avalonia shell skeleton):** the `AddGumCore` / `AddGumAvalonia` split lets an Avalonia head compose the same core services.
- **Unblocks Phase 15 (Editor canvas hosting):** the neutral panel-content handle replaces `WindowsFormsHost`/`FrameworkElement`, letting the Avalonia head host the editor canvas its own way.
- **Unblocks Phase 20 (Property grid + views):** the `IPropertyGrid` contract defined here is what the Avalonia property grid implements.

## Risks & notes

- **`MainPanelViewModel` is the WPF `ITabManager` impl, not a shared component.** Do not treat it as "reused as-is" by Avalonia. It carries six `System.Windows.Data.ICollectionView`, `WindowsFormsHost` wrapping, and `FrameworkElement` materialization, so it is registered only in `AddGumWpf()`. The resolution (locked with Phase 10) is a **neutral base view-model** (column widths, `IPanelContent`-typed tab model, teardown / layout persistence) plus a **thin per-head subclass**; the Avalonia head gets its own subclass in Phase 10. Any plan that registers `MainPanelViewModel` from the core or hands it to the Avalonia head unchanged is wrong.
- **Airspace hack disappears under Avalonia.** The `WindowsFormsHost` + `host.Margin` workaround in `MainPanelViewModel.cs:116-120` exists only because WPF/WinForms interop blocks mouse hits on the grid splitter and window resize handle. It must stay inside the WPF head and must not be promoted onto the neutral interface. Avalonia has no WinForms airspace problem.
- **The airspace branch may be partially dead — preserve behavior exactly, don't "fix" it.** The branch at `MainPanelViewModel.cs:116` only fires when the `element` argument *is itself* a `WindowsFormsHost` and the title is `"Editor"`. The actual Editor tab (`MainEditorTabPlugin.cs:1166`) passes a `Grid` with the `WindowsFormsHost` nested inside, so this branch does not run for it. Whatever the current behavior is, this phase must reproduce it identically — do not remove or "repair" the branch as part of the seam extraction; treat any behavior delta as a regression. Flag the apparent mismatch to the architect rather than changing it here.
- **Do not break plugin registration.** External and internal plugins call `_tabManager.AddControl(...)` directly (WinForms `Control` or WPF `FrameworkElement`) or via `PluginBase.CreateTab`/`AddControl` (`FrameworkElement` only). There are ~16 internal call sites (enumerated in Task 4) plus the public `PluginBase` surface. Keep WPF-head overloads that accept those exact types and wrap them so every call site compiles and behaves identically. Treat any change to the public plugin surface as a regression.
- **`PluginTab.Content` is data-bound in XAML.** `MainPanelControl.xaml` binds to `Content` as a `FrameworkElement`. When `Content` becomes a neutral handle, materialize it to a `FrameworkElement` via a value converter / DataTemplate in the WPF head rather than leaking the type back onto the model.
- **Several contracts are already neutral.** `IDispatcher`, `IDialogService`, `IThemingService`/`IEffectiveThemeSettings`, and `IUiSettingsService` already avoid WPF types on their interfaces; the work for those is mostly **relocation of registrations** into `AddGumWpf()`, not interface surgery. Don't over-engineer them.
- **`file static class` scoping.** `ServiceCollectionExtensions` (and the `ServiceCollectionHelpers` / `ViewModelFuncFactoryRegistration` helpers) in `Builder.cs` are all `file`-scoped, so nothing outside `Builder.cs` can call them. If a separate Avalonia head assembly must call `AddGumCore()`, that scoping must change to `public`/`internal` and likely move out of the WPF tool project. Decide deliberately during implementation.
- **`Application.Current.Dispatcher` is the only stray reference** in tool source (`Builder.cs:183`); the grep hit in `docs_projects\11_WpfDataUi.md` is documentation, not code. Removing the `Builder.cs` reference fully clears the dispatcher seam.
- **Property grid is contract-only this phase.** Resist implementing a new grid. The WPF `IPropertyGrid` impl must wrap the existing `DataUiGrid` so the Variables tab is unchanged.

## Done / verification checklist

- [ ] `Gum.UiAbstractions` holds neutral interfaces for the panel-content handle and `IPropertyGrid` (Dispatcher/Dialogs/Theming/UiSettings interfaces already neutral — confirmed in place, no surgery needed).
- [ ] `IDialogService`, `IThemingService`/`IEffectiveThemeSettings`, `IUiSettingsService`, and `IDispatcher` expose **no** `System.Windows.*` / `System.Windows.Forms.*` types (verified by inspection — already true today).
- [ ] `ITabManager` and `PluginTab` no longer reference `System.Windows.FrameworkElement` or `System.Windows.Forms.Control` on their public surface; they use the neutral panel-content handle. (`MainPanelViewModel`, the WPF impl, may still use those types internally.)
- [ ] `IPropertyGrid` covers the surface `PropertyGridManager` needs (`Instance`, `Categories`, `SetCategories`, `SetMultipleCategoryLists`, `Refresh`, `IsEnabled`); `PropertyGridManager` talks to `IPropertyGrid`, not directly to `DataUiGrid` (or only through the WPF head). No new grid is implemented (that is Phase 20).
- [ ] `IPropertyGrid` itself exposes no WPF types; the WPF impl wraps the existing `DataUiGrid`/`MainPropertyGrid` (`MemberCategory`/`InstanceMember` relocation deferred to Phase 20).
- [ ] `Application.Current.Dispatcher` no longer appears in core registration; the `IDispatcher -> AppDispatcher` registration lives in `AddGumWpf()`.
- [ ] `Builder.cs` is split into `AddGumCore()` + `AddGumWpf()` + stub `AddGumAvalonia()`; `GumBuilder.CreateHostBuilder` calls `AddGumCore()` then `AddGumWpf()`. The extension/helper classes are reachable from a future head assembly (no longer `file`-scoped if needed).
- [ ] `AddGumCore()` references no `System.Windows.*` / `System.Windows.Forms.*` type (grep the method body / its file region as a first pass).
- [ ] **Purity test seeded.** Add a real automated test (not just a grep) that resolves the `AddGumCore()` service graph and asserts no resolved type lives in `System.Windows.*` / `System.Windows.Forms.*`. Seed it as Phase 5 lands; ongoing ownership is Phase 22. This is the enforcement mechanism for the "single audited no-`System.Windows` surface" guarantee.
- [ ] Save/folder picker seam added: `IDialogService.SaveFile(...)` (and folder picker if needed) exists with a WPF impl; `ProjectManager.cs:852` and `MainSvgExportPlugin.cs:75` route through it. Sync-vs-async signature decision flagged for Phase 20 (Avalonia `IStorageProvider` is async).
- [ ] `IClipboardService` seam added (WPF impl); the non-view shared-logic call sites (`CompositeMemberLogic.cs:235`, `StateReferencingInstanceMember.cs:540`) route through it.
- [ ] Reveal-in-file-manager seam added (WPF/Windows impl using `explorer.exe /select,`); `ProjectManager.cs:914` and `ElementTreeViewManager.RightClick.cs:195` route through it.
- [ ] All internal `_tabManager.AddControl(...)` call sites (Task 4 list) and `PluginBase.CreateTab`/`AddControl` compile unchanged (no plugin API regression).
- [ ] The airspace hack remains inside the WPF `ITabManager` implementation only, with identical behavior (branch unchanged).
- [ ] `dotnet build GumFull.sln` succeeds (tool work must build via the solution, not individual csprojs — `$(SolutionDir)` plugin post-builds).
- [ ] Manual smoke test: tabs (Project, States, Variables, Behaviors, Output, Errors, Code, File Watch, Project Properties, Alignment, Texture Coordinates, Animations, Editor) appear and dock in the same locations; dialogs open and center correctly; theming (light/dark/accent) applies; Variables grid edits work — i.e. **no observable behavior change**.
