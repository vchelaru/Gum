# Phase 80 — Views and dialogs

## Status (2026-09-10, in progress on branch `phase-80-views-and-dialogs`)

The Avalonia head builds its views in C#, not AXAML (the phase 30 convention); "AXAML twin"
below means a C# Avalonia view bound to the same VM. Progress, in the order the work landed:

- **Dialog infrastructure.** `DialogWindow` gained the `DialogTitle` and `AuxiliaryActions`
  attached properties (twins of WPF's `Dialog.DialogTitle` / `Dialog.AuxiliaryActions`).
  `DialogViewRegistry` can be introspected, and `DialogViewRegistryTests` fails when any concrete
  `DialogViewModel` in `Gum.Presentation` has neither a registered view nor a named owner in its
  `OwnedElsewhere` list. Shared converters live in `Tool/Gum.Avalonia/Converters/`.
- **New Project** (`NewProjectDialogView` + `ThemeSelectionView`), the first dialog users hit.
- **Every other dialog whose VM is headless**: Expose Color, Display References, Theming (Avalonia's
  own `ColorPicker` replaces PixiEditor's; package `Avalonia.Controls.ColorPicker`), Load Recent,
  the three Import dialogs (one view over `ImportBaseDialogViewModel`), and State Animation's
  New Animation, Add State Keyframe and Add Sub-Animation. The Add/Rename string dialogs were
  already covered by the phase 30 `GetUserStringDialogBaseViewModel` view. `OwnedElsewhere` now
  holds only phase 70's four (Add Variable, Add Forms, Import from .gumx and its diff details).
- **Tab content is a ViewModel, resolved per head** (the phase 40 decision, now implemented). Each
  head has a `TabViewRegistry` (`Gum/Controls/TabViewRegistry.cs`,
  `Tool/Gum.Avalonia/Shell/TabViewRegistry.cs`) mapping a tab VM to its view and an optional custom
  header; `MainPanelViewModel` / `AvaloniaTabManager` consult it when `AddControl` gets a non-control.
- **Built-in plugins shared by both heads live in Gum.Presentation** as `CorePriorityPlugin`s, and
  both heads list Gum.Presentation in `InternalPluginAssemblies`. Moved, class names and namespaces
  unchanged (so plugin enablement settings carry over): Errors, History (Undos), Alignment, Hotkeys,
  File Watch, Behaviors, Output, Load Recent, and the view-less Inheritance, Parent, Nine Slice,
  Selection History, Favorite Component, Duplicate Variable, Orphan Code File, Hide/Show Tools and
  SVG Export. The Avalonia `OutputPlugin` twin is deleted. Code-behind logic moved to VMs with tests
  first: Behaviors' Edit/OK/Cancel (`BehaviorsViewModel` commands), the Errors help link
  (`AllErrorsViewModel.OpenHelpCommand`), and History's focus-on-tab (`UndosViewModel.FocusCurrentItem`).
  `AlignmentViewModel` and `IToolsVisibility` are now bridged to plugins.
- **Delete confirmation is neutral.** `PluginBase.DeleteOptionsShow` / `DeleteOptionsConfirmed` carry a
  `DeleteOptionsDialogViewModel` (check boxes and pick-one groups). The Avalonia head shows it through
  `IDialogService` (`DeleteOptionsDialogView`); the WPF head renders the same options into its
  `DeleteOptionsWindow`, which stays a WPF `Window` only because CodeOutputPlugin (phase 70's area)
  still adds WPF controls through `WpfPluginBase`'s pair. `DeleteObjectPlugin` moved to
  Gum.Presentation on the neutral events; State Animation switched too and no longer needs
  `WpfPluginBase`.
- **State Animation is a neutral core plus two heads**, like the editor tab.
  `Tool/StateAnimationPlugin.Core` (net10.0) holds `StateAnimationPluginBase`, the managers, the
  list hotkeys (`AnimationTabKeyHandler`, extracted from the WPF code-behind) and the timeline math
  (`TimelineLayout`, `InterpolationCurve`, extracted from the WPF timeline's code-behind and
  converters). The WPF plugin keeps its views and derives `MainStateAnimationPlugin`; the Avalonia
  head adds `AvaloniaStateAnimationPlugin` with `AnimationsView`, `TimelineView` and
  `KeyframeDetailView`. **The Skia-in-WPF surface was dead code** (`TimedStateMarkerDisplay`, only
  referenced from a commented-out line), so it is deleted with `SkiaSharp.Views.WPF` and the win10
  SDK TFM. The "native Avalonia draw" is a plain `Render` override over the shared geometry, not an
  `ICustomDrawOperation` Skia lease: nothing here needs raw Skia. Also fixed: the plugin built
  `DuplicateService` and `ElementDeleteService` in its constructor with `_dialogService`, which MEF
  only sets after construction.
- **Performance Measurement is net10.0** and loads in both heads: it ships no views, its tab is
  `PerformanceViewModel`, and each head registers a view. Its timer is a `PeriodicUiTimer` over the
  bridged `IDispatcher` (now an `IUiTimer`), owned by the plugin so its interval is its own. The dead
  `Gum/Controls/ColorPickerSwatch` (no consumers since #1467) is deleted.

## Purpose

Re-author every remaining WPF view as Avalonia AXAML bound to the existing ViewModels: the
internal-plugin panels, the dialogs, the small custom controls, the converters and behaviors they
depend on, and the plugin projects' own views. This is the bulk conventional porting work. It is
low risk per file and wide.

## Builds on

- ~35 ViewModels are headless in `Gum.Presentation` and expose neutral state (ADR-0004);
  `DialogViewResolver` resolves views across assemblies (#3781); `IDialogService` and its
  generic dialog types are headless (#3354).
- The phase-40 contract: a tab is a VM resolved to a per-head view.
- The phase-30 head provides the generic dialogs, dispatcher, clipboard, spinner, and file-reveal
  seams.
- Delete-options flow, references dialog, add/import dialogs, project properties, theming dialog,
  hotkeys, recent files, errors, output, undos, alignment, behaviors, and file-watch panels all have
  headless VMs already.

## Decisions

- **One AXAML per XAML, same VM, same name, in the Avalonia head** (or in the plugin project once
  it is `net10.0`), so the mapping is mechanical and reviewable file by file.
- **Converters are rewritten as Avalonia `IValueConverter`s once, in a shared head folder.** Many of
  the 14 `Gum/Converters` plus 8 `PropertyGridHelpers/Converters` and 5 theme converters exist to
  turn neutral VM state into WPF types; where a converter only existed to bridge a type ADR-0004 has
  since neutralized, delete it rather than port it.
- **Behaviors move to `Avalonia.Xaml.Interactivity` equivalents** only where the behavior is
  genuinely view-side (focus, drag thresholds, key routing); a behavior that carries logic is
  extracted to the VM first.
- **Windows become `IDialogService` dialogs.** The two remaining `System.Windows.Window` subclasses
  and any direct `ShowDialog` calls route through the service with a VM, so the Avalonia head
  never needs a `Window` type in shared code.
- **Order: panels users touch most first** (Errors, Output, Undos, Alignment, Project Properties,
  Recent Files), then dialogs, then the State Animation plugin's seven views, then the rest.
- **State Animation's Skia-in-WPF surface becomes a native Avalonia draw.** The plugin targets the
  Windows 10 SDK only because of `SkiaSharp.Views.WPF`; Avalonia renders on Skia already, so the
  preview uses an `ICustomDrawOperation` with the Skia API lease instead of a hosted view. That
  removes the last reason for the win10 TFM.

## Scope

**In (counts on `main` at 2026-09-09):**

| Location | XAML | What |
|---|---|---|
| `Gum/Controls` | 9 | color display/swatch, corner radius, grid-snap warning bar, spinner, state-editing bar, theme selector, title path, main panel |
| `Gum/Services/Dialogs` + `Gum/Dialogs` + `Gum/Gui/Windows` + `Gum/Views` | 10 | generic dialogs (phase 30 does 4), delete options, references, dialog window host |
| `Gum/Plugins/InternalPlugins/**` | 19 | Errors 3, AlignmentButtons 3, VariableGrid 3 (phase 70), LoadRecent 2, Undos, TreeView (60), StatePlugin (60), ProjectProperties, Output, Hotkey, FileWatch, Behaviors |
| `Gum/Plugins/ImportPlugin/Views` | 1 | import |
| `Gum/StateAnimationPlugin` | 7 | animations, keyframes, timeline |
| `Gum/ImportFromGumxPlugin` | 2 | |
| `Gum/CodeOutputPlugin`, `GumFormsPlugin`, `PerformanceMeasurementPlugin` | 1 each | |

Themes (15) are phase 90; `WpfDataUi` (18) is phase 70; the two canvas views are phase 50.

**Out:** any VM change without a WPF-side test first; new features; visual polish beyond parity.

## Tasks

1. Inventory each XAML: VM bound, converters used, behaviors used, dialogs opened, code-behind
   lines. Flag code-behind with logic for extraction.
2. Converters: port or delete, per the rule above, into one head folder.
3. Panels in the order above; register each through the phase-40 tab contract.
4. Dialogs: delete-options, references, add/import/rename, project properties, theming, hotkeys,
   plugins; all through `IDialogService`.
5. State Animation plugin views; make the project `net10.0` when its last WPF reference is gone.
6. Remaining plugin views; flip each project to `net10.0` as it clears.
7. Small controls (spinner, warning bars, color swatch) as Avalonia `UserControl`s or templated controls.

## Key files

- Every `.xaml`/`.xaml.cs` listed above; `Gum/Converters/*`, `Gum/Behaviors/*`, `Gum/Extensions/*`
- `Gum/Services/Dialogs/DialogViewResolver.cs`, `Tools/Gum.Presentation/Dialogs/*`
- `.claude/skills/gum-tool-dialogs`, `gum-tool-viewmodels`, `gum-tool-animations`

## Dependencies

Needs phases 30 and 40. Independent of 50/60/70 except the VariableGrid and tree views owned there.
Blocks phase 100's per-panel parity checklist.

## Risks

- Code-behind that quietly holds logic (the "view-wall" classes ADR-0005 warned about). The
  inventory step exists to find them; each is a WPF-side extraction PR before its AXAML.
- `[DependsOn]` name collisions with framework attributes (#3948) recur in Avalonia; check usings.

## Done when

- [ ] Every XAML in the table has an AXAML twin bound to the same VM, or a documented reason it does not exist.
- [ ] Converters folder has no bridge-only converters left.
- [ ] Every plugin project that has no canvas is `net10.0`.
