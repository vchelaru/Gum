# Phase 20 — Property grid + remaining views

## Purpose

Make the Avalonia head of the Gum tool functionally complete for everyday
editing. Two things happen in this phase:

1. **Property grid.** Stand up a real Avalonia property grid behind the
   `IPropertyGrid` seam defined in Phase 5, so the Variables tab does data-driven
   member display (type converters, custom editors, categories) instead of the
   placeholder panel from Phase 10.
2. **Remaining views.** Convert the rest of the tool's WPF XAML views to Avalonia
   AXAML — dialogs, the state-animation/timeline views, and the small custom
   controls (color pickers, spinner, alignment buttons) — reusing the existing,
   framework-neutral ViewModels unchanged.

After this phase the Avalonia shell hosts the editor canvas (Phase 15), a working
property grid, and native dialogs/controls. Phase 25 handles plugin/theming
polish.

## Decisions & rationale

- **Re-author the property grid (not adopt a third-party grid). Reason:**
  `DataUiGrid` is a bespoke `ItemsControl` with ~13 custom editors, a
  delegate-driven optional-visibility model (`_membersWithOptionalVisibility`),
  `MemberCategory` ordering, multi-select, and `InstanceMember` custom
  getters/setters. There is no realistic "adopt" path that wouldn't still
  require porting all of that. **Direction:** re-author `DataUiGrid` for
  Avalonia. *(Rejected option: adopt an existing Avalonia property grid —
  rejected because a generic reflection grid cannot reproduce Gum's member
  model and would still need every custom editor and the visibility/category
  behavior ported.)*
- **Port the framework-neutral member model first. Reason:** the member types
  (`InstanceMember`/`MemberCategory`/`TypeMemberDisplayProperties`) are mostly
  framework-neutral and are the foundation the visuals sit on. **Direction:**
  port the model types before the control visuals. Note that today
  `MemberCategory`/`InstanceMember` carry `System.Windows` usings — the neutral
  member model must NOT carry `System.Windows` types into the seam layer, so
  relocate/mirror those as neutral types as part of the port.
- **Avalonia editors implement the same `IDataUi` contract. Reason:** the grid
  drives editors through the `IDataUi` interface
  (`c:\git\Gum\WpfDataUi\IDataUi.cs`), not through a WPF base class.
  **Direction:** each Avalonia editor implements `IDataUi` (or its Phase 5
  equivalent) so the grid can drive it unchanged.
- **ViewModels are reused unchanged. Reason:** the VMs are framework-neutral by
  design from Phase 5. **Direction:** if a VM needs a tweak to be view-agnostic,
  treat it as a Phase 5 gap and flag it — do not fork the VM here.

## Scope

### In scope

- An Avalonia implementation of the `IPropertyGrid` seam (Phase 5) backed by a
  re-authored `DataUiGrid` (decision locked — see Decisions & rationale).
- Reproducing the data-driven member display the Variables tab needs: categories,
  per-member type converters, and the custom editor set that `WpfDataUi` provides
  today (combo box, slider, angle selector, file picker, color, toggle-button
  options, string list, etc.).
- Converting the remaining tool XAML views to AXAML against their existing
  ViewModels, grouped by cluster. The tool has 64 `.xaml` files total, but 14 are
  theme resource dictionaries under `Gum/Themes/` (Phase 25, not views) and
  `App.xaml` + `MainWindow.xaml` are shell-level (Phase 10), leaving **~48 actual
  views** to convert here. Group the work by cluster rather than enumerating each:
  - Dialog views under `Gum/Services/Dialogs/` (plus the `[Dialog]`/resolver
    wiring for the Avalonia head).
  - State-animation / timeline views under `Gum/StateAnimationPlugin/Views/`.
  - Small controls under `Gum/Controls/` and the alignment-button cluster under
    `Gum/Plugins/InternalPlugins/AlignmentButtons/`.
- Keeping `DialogViewResolver` working when the views it resolves are Avalonia
  `Control`s rather than WPF `FrameworkElement`s.

### Out of scope

- Editor canvas hosting / rendering surface (Phase 15).
- Plugin host model, theming pass, and final polish (Phase 25).
- Changing any ViewModel behavior. ViewModels (`Gum/Mvvm/ViewModel.cs` and the
  per-feature `*ViewModel`/`*DialogViewModel` classes) are reused as-is; only
  views change. If a VM needs a tweak to be view-agnostic that should have been
  caught in Phase 5 — flag it, don't fork the VM.
- Porting the legacy WinForms `Tool/EditorTabPlugin_XNA` UI and the
  Samples/Tests XAML (those are not part of the tool's view surface).

## Tasks

### Property grid (the deep one)

The Variables tab is driven by `WpfDataUi`'s `DataUiGrid`
(`c:\git\Gum\WpfDataUi\DataUiGrid.cs`), an `ItemsControl`-derived WPF control with
its own member model (`InstanceMember`, `MemberCategory`,
`TypeMemberDisplayProperties`) and a set of editor user-controls under
`c:\git\Gum\WpfDataUi\Controls\`. The Variables tab plugin that hosts it lives
under `c:\git\Gum\Gum\Plugins\InternalPlugins\VariableGrid\`
(`MainPropertyGrid.xaml`, `AddVariableWindow.xaml`, `VariableRemoveButton.xaml`).

1. **Re-author `DataUiGrid` for Avalonia (decision — see Decisions & rationale).**
   The grid is re-authored, not adopted: Gum's grid is not a generic reflection
   grid — it relies on Gum-specific concepts (`InstanceMember` with custom
   getters/setters, optional-visibility delegates, `MemberCategory` ordering,
   multi-select via `MultiSelectInstanceMember`, and a fixed set of custom
   editors), so adopting a third-party grid would still require porting all of
   that. Port the framework-neutral member model first
   (`InstanceMember`/`MemberCategory`/`TypeMemberDisplayProperties`, relocated/
   mirrored as neutral types with no `System.Windows` usings), then the visuals.
2. **Implement the `IPropertyGrid` seam for Avalonia.** Phase 5 defined the
   interface and the framework-neutral member model that sits over `WpfDataUi`.
   This phase provides the Avalonia-side implementation that the Phase 10 shell
   binds the Variables tab to. Keep the seam's member abstractions
   (`IMemberDefinition`, `InstanceMember`, `MemberCategory`, the
   `PropertyChanged`/`BeforePropertyChanged` event args under
   `WpfDataUi\EventArguments\`) — port the model types, not just the control.
3. **Reproduce the editor set.** Port the WPF editor displays under
   `c:\git\Gum\WpfDataUi\Controls\` to Avalonia equivalents and wire them to the
   member model the same way `DataUiGrid` does today. The set to cover:
   `TextBoxDisplay`, `ComboBoxDisplay` / `EditableComboBoxDisplay` (the latter is
   a code-only subclass of `ComboBoxDisplay` with no `.xaml`),
   `CheckBoxDisplay`, `NullableBoolDisplay`, `SliderDisplay`,
   `AngleSelectorDisplay` (a dial built from XAML `Ellipse`/`Line` with a
   `RotateTransform` bound to the angle — not raw `OnRender`, so it maps to an
   Avalonia `RotateTransform`), `FileSelectionDisplay` /
   `MultiFileDisplay` (file pickers; share `FilePickingLogic`), `ListBoxDisplay`,
   `StringListTextBoxDisplay`, `ToggleButtonOptionDisplay`, `PlusMinusTextBox`,
   `MultiLineTextBoxDisplay`, and the `SingleDataUiContainer` host row. That is
   **13 `.xaml` editor controls** plus the shared `TextBoxDisplayLogic` /
   `FilePickingLogic` helpers and the `EditableComboBoxDisplay` subclass. Every
   editor implements `IDataUi` (`c:\git\Gum\WpfDataUi\IDataUi.cs`) — that contract,
   not the WPF base class, is what the grid talks to, so the Avalonia editors must
   implement the same `IDataUi` (or its Phase 5 equivalent) for the grid to drive
   them.
4. **Categories + type converters.** Preserve auto-populate-by-category behavior
   (`DataUiGrid.PopulateCategories`, `IsAutoPopulateCategoriesEnabled`) and the
   per-member type-converter path (each `InstanceMember` can supply a
   `TypeConverter` that yields the editor's allowed values / formatting). These
   are what let the same grid show a color editor for one variable and a unit
   dropdown for another.
5. **Optional-member visibility.** Re-implement the delegate-driven visibility
   (`_membersWithOptionalVisibility` in `DataUiGrid`) that re-evaluates which
   members show whenever the instance changes — Gum hides/shows variables based on
   the current element state.
6. **Cross-platform font source (must-fix).**
   `Gum/PropertyGridHelpers/Converters/FontTypeConverter.cs:35` populates the font
   dropdown from `System.Drawing.FontFamily.Families`, which is GDI+ and
   Windows-only on .NET 8 — it will fail or return nothing on Linux/macOS.
   Replace it with a cross-platform font source (Avalonia
   `FontManager.Current.SystemFonts`) and verify the font dropdown populates on
   Linux and macOS, not just Windows. Keep the existing 10-second cache so the
   font enumeration still does not run on every drag.

### Dialog views

Dialog system is view-agnostic via `DialogViewResolver` + `DialogAttribute`
(`c:\git\Gum\Gum\Services\Dialogs\DialogViewResolver.cs`) and the
`DialogService`/`DialogWindow` pair. ViewModels derive from `DialogViewModel`.

1. Convert the dialog views under `c:\git\Gum\Gum\Services\Dialogs\` to AXAML,
   reusing their VMs: `MessageDialogView`, `ChoiceDialogView`,
   `GetUserStringDialogView`, `PluginsDialogView`, and the host `DialogWindow`.
2. **Keep the resolver working for the Avalonia head.** `DialogViewResolver.Scan`
   currently filters candidate views with
   `typeof(FrameworkElement).IsAssignableFrom(t)` and pairs them to VMs by name
   convention or `[Dialog(typeof(...))]` attribute. For Avalonia the type test
   must accept Avalonia `Control`/`UserControl` instead of WPF `FrameworkElement`.
   Prefer abstracting the base type behind the Phase 5 seam (or making the test
   head-specific) rather than duplicating the resolver. The VM-name convention
   (`XDialogViewModel` ↔ `XDialogView`, with the `GetUserStringDialogBaseViewModel`
   fallback) must keep working unchanged.
3. Convert the other dialog/window-style views that ride the same system or are
   small standalone windows: `Gum/Dialogs/` (`ThemingDialogView`,
   `ExposeColorDialogView`), `Gum/Views/DisplayReferencesDialogView`,
   `Gum/Gui/Windows/DeleteOptionsWindow`, and the per-plugin dialog views
   (`AddVariableWindow`, `AddFormsWindow`, `HotkeyView`, `ImportFileView`,
   `ImportFromGumxView`, `AddAnimationDialogView`, `AddStateKeyFrameDialogView`,
   `SubAnimationSelectionWindow`, the recent-files views).
4. **Implement the Avalonia file/folder pickers on `IDialogService`.** Phase 5
   added save-file and folder-picker members to `IDialogService`. Implement them
   for the Avalonia head using Avalonia `IStorageProvider`
   (`OpenFilePickerAsync` / `SaveFilePickerAsync` / `OpenFolderPickerAsync`),
   obtained from the top-level window. Note the impedance mismatch: the
   `IStorageProvider` API is **async**, but the existing seam exposes a
   synchronous signature (`List<string>? OpenFile(...)`). Record the bridging
   decision — recommended is to make the seam async (return `Task<...>` and
   await it from callers) rather than blocking on the async picker, which can
   deadlock on the UI thread; if async-all-the-way is too invasive for this
   phase, provide an explicit async-over-sync bridge that marshals correctly off
   the UI thread and document it as a known wart to revisit. Whichever path is
   chosen, apply it consistently across all the picker members Phase 5 added.

### State animation / timeline views

Under `c:\git\Gum\Gum\StateAnimationPlugin\Views\` — **7 `.xaml` views** plus
three drawing/helper `.cs` files: `MainWindow.xaml`, `Timeline.xaml`
(+ `Timeline.Resources.cs`, `TimelineOverlay.cs`, `InterpolationTrackControl.cs`),
`StateView.xaml`, `TimedStateMarkerDisplay.xaml`, and the three animation dialogs
listed in "Dialog views" above (`AddAnimationDialogView`,
`AddStateKeyFrameDialogView`, `SubAnimationSelectionWindow`).

1. Convert the straightforward list/editor views (`MainWindow`, `StateView`,
   `TimedStateMarkerDisplay`) to AXAML against existing VMs.
2. **Custom-drawn timeline.** `Timeline` is not a plain XAML layout — it has
   companion code that draws keyframes, the playhead/overlay, and interpolation
   tracks. Two of these are raw-drawing controls that override WPF
   `OnRender(DrawingContext)` and must move to Avalonia `Render(DrawingContext)`:
   - `TimelineOverlay` (a `FrameworkElement` → Avalonia `Control`) draws the
     playhead/overlay.
   - `InterpolationTrackControl` (a `Canvas` → Avalonia `Canvas`) draws the
     interpolation tracks.
   `Timeline.Resources.cs` holds brushes/pens/geometry resources, which map to
   Avalonia `Pen`/`IBrush`/`Geometry`. Pointer-input handling (scrub the playhead,
   select/drag keyframes) moves from WPF `MouseEventArgs` to Avalonia
   `PointerEventArgs`. Treat the timeline as its own work item, separate from the
   surrounding XAML, and budget for it like a custom control.

### Controls

Small custom controls under `c:\git\Gum\Gum\Controls\` plus a couple of
color-related views:

- `Spinner.xaml` — numeric up/down style control.
- `ColorPickerSwatch.xaml`, `ColorDisplay.xaml`
  (`Gum/Controls/`) and `ExposeColorDialogView` — color swatches / picker. Note
  whether any rely on WPF color types (`System.Windows.Media.Color`/`Brush`) that
  must map to Avalonia `Color`/`IBrush`.
- `TitleFilePathDisplay.xaml`, `MainPanelControl.xaml`.

Convert each to AXAML, reusing its existing VM/logic; flag any that do custom
drawing or rely on WPF-only primitives.

### Alignment buttons

Cluster under `c:\git\Gum\Gum\Plugins\InternalPlugins\AlignmentButtons\`:
`AlignmentControl.xaml`, `AlignmentPluginControl.xaml`, `AnchorControl.xaml`,
`DockControl.xaml`. These are grids of toggle/command buttons that set
anchor/dock/alignment on the selected instance. Convert to AXAML, mapping the
button grid and command bindings; confirm icon resources resolve under the
Avalonia theming set (final theming pass is Phase 25).

This phase is highly parallelizable: the property grid, the dialog cluster, the
timeline, and the small-control clusters can each be owned independently once the
Phase 5 seam and Phase 10 shell are in place.

## Key files & projects

Property grid:
- `c:\git\Gum\WpfDataUi\DataUiGrid.cs` — the control to re-author.
- `c:\git\Gum\WpfDataUi\DataTypes\` — `InstanceMember.cs`, `MemberCategory.cs`,
  `IMemberDefinition.cs`, `TypeMemberDisplayProperties.cs`,
  `MultiSelectInstanceMember.cs`, `CompositeInstanceMember.cs` (the member model
  to port).
- `c:\git\Gum\WpfDataUi\Controls\` — 13 editor `.xaml` controls (TextBox, ComboBox,
  CheckBox, NullableBool, Slider, AngleSelector, FileSelection, MultiFile,
  ListBox, StringListTextBox, ToggleButtonOption, PlusMinusTextBox,
  MultiLineTextBox) + `SingleDataUiContainer`, plus the code-only
  `EditableComboBoxDisplay` subclass and the `TextBoxDisplayLogic` /
  `FilePickingLogic` helpers.
- `c:\git\Gum\WpfDataUi\IDataUi.cs` — the editor contract the grid drives; the
  Avalonia editors must implement it (or its Phase 5 equivalent).
- `c:\git\Gum\WpfDataUi\EventArguments\` — `PropertyChangedArgs.cs`,
  `BeforePropertyChangedArgs.cs`.
- `c:\git\Gum\Gum\Plugins\InternalPlugins\VariableGrid\` — `MainPropertyGrid.xaml`
  (Variables tab host), `AddVariableWindow.xaml`, `VariableRemoveButton.xaml`.

Dialogs:
- `c:\git\Gum\Gum\Services\Dialogs\` — `DialogViewResolver.cs`,
  `DialogService.cs`, `DialogWindow.xaml(.cs)`, and the four built-in dialog
  views/VMs (`MessageDialog*`, `ChoiceDialog*`, `GetUserStringDialog*`,
  `PluginsDialog*`).

State animation / timeline:
- `c:\git\Gum\Gum\StateAnimationPlugin\Views\` — `Timeline.xaml(.cs)`,
  `Timeline.Resources.cs`, `TimelineOverlay.cs`, `InterpolationTrackControl.cs`,
  `MainWindow.xaml`, `StateView.xaml`, `TimedStateMarkerDisplay.xaml`, and the
  animation dialogs.

Controls / alignment:
- `c:\git\Gum\Gum\Controls\` — `Spinner.xaml`, `ColorPickerSwatch.xaml`,
  `ColorDisplay.xaml`, `TitleFilePathDisplay.xaml`, `MainPanelControl.xaml`.
- `c:\git\Gum\Gum\Plugins\InternalPlugins\AlignmentButtons\` — the four alignment
  views.

Reused, do not change:
- `c:\git\Gum\Gum\Mvvm\ViewModel.cs` and the per-feature ViewModels.

## Dependencies

- **Phase 5 (Extract UI seams)** — the `IPropertyGrid` seam and the
  framework-neutral member model must exist; this phase provides their Avalonia
  implementation. The view-agnostic dialog system (`DialogViewResolver` +
  `DialogAttribute`) was already in place and is relied on here.
- **Phase 10 (Avalonia shell skeleton)** — the shell with placeholder panels is
  where the property grid and converted views are slotted in.
- The property grid additionally depends on the **shell** being able to host the
  Variables tab panel (Phase 10) and on selection state flowing into it
  (selection plumbing established by the earlier phases).
- Independent of Phase 15 canvas internals, though end-to-end verification (edit a
  variable and see it reflect in the canvas) requires Phase 15's hosted canvas to
  be running.

## Risks & notes

- **`WpfDataUi` re-implementation is deep.** `DataUiGrid` is a custom
  `ItemsControl` with a sizeable member model and 13 bespoke editors, several of
  which are custom-drawn or have non-trivial input logic
  (`AngleSelectorDisplay`, the file pickers, `ToggleButtonOptionDisplay`). This is
  the single largest item in the phase; size it accordingly and consider porting
  the data-model types first (they are mostly framework-neutral) before the
  visuals.
- **Custom editors must round-trip Gum types.** The editors must keep using each
  member's `TypeConverter` and getter/setter so that color, unit, file-path, and
  enum variables edit and persist exactly as today. A generic Avalonia grid that
  reflects over properties will not reproduce this without the member model.
- **Timeline custom drawing.** WPF `OnRender(DrawingContext)` + WPF input maps to
  Avalonia `Render(DrawingContext)` + Avalonia pointer events; pens/geometries and
  hit-testing differ. The timeline (keyframes, overlay, interpolation tracks) is
  effectively a custom control port, not a XAML translation.
- **AXAML vs XAML differences.** Expect friction on: `x:Type`/`DataType`
  (Avalonia compiled bindings vs WPF), `Style` triggers (WPF `Trigger`/`DataTrigger`
  → Avalonia selectors/`Classes`/`Style` with property selectors), `DataTemplate`
  keying, `Converter` resource lookup, attached properties (`Grid.Row` etc. are
  fine, but many WPF-specific ones are not), and brush/color types
  (`System.Windows.Media` → Avalonia `Media`). Resource dictionaries under
  `Gum/Themes/` are a Phase 25 concern but converted views must not hard-depend on
  WPF-only resource keys.
- **Dialog resolver type test.** `DialogViewResolver.Scan` keys off
  `FrameworkElement`. Whichever way that's made head-aware (seam abstraction or
  head-specific build), keep the existing name-convention and `[Dialog]`-attribute
  matching intact so no VM-to-view wiring regresses.
- **Code-behind.** Several views have real code-behind
  (`*.xaml.cs`, plus the timeline's extra `.cs` files). Plain layout code-behind
  ports cleanly; anything touching WPF events, `Dispatcher`, or visual-tree APIs
  needs Avalonia equivalents.

## Done / verification checklist

- [ ] Re-author decision for the property grid recorded (with the rejected
      "adopt" option and rationale) in this doc — see Decisions & rationale.
- [ ] `IPropertyGrid` Avalonia implementation compiles against the Phase 5 seam
      and is bound by the Phase 10 shell's Variables tab.
- [ ] Member model ported: categories, per-member type converters, multi-select,
      and optional-member visibility behave as in the WPF grid.
- [ ] Concrete category check: members group under the right `MemberCategory`,
      categories expand/collapse, and changing the selected instance re-evaluates
      optional-visibility members (a variable that is hidden in one state appears
      in another) — matching `DataUiGrid.PopulateCategories`.
- [ ] All 13 editor displays render and edit in the Avalonia grid (text, combo,
      check, nullable bool, slider, angle, file/multi-file, list, string list,
      toggle-button options, plus/minus, multiline), each implementing `IDataUi`.
- [ ] **Edit a variable in the Avalonia Variables tab → value persists to the
      element (re-select the instance and the new value is shown) AND reflects live
      in the hosted canvas.** The canvas half of this check requires Phase 15's
      hosted canvas to be running; if Phase 15 is not yet integrated, verify
      persistence only and note the canvas check as blocked on Phase 15.
- [ ] Font dropdown populates from a cross-platform font source
      (`FontTypeConverter` no longer uses `System.Drawing.FontFamily.Families`);
      verified on Linux/macOS, not just Windows.
- [ ] Dialog views converted; `DialogViewResolver` resolves VM→view for the
      Avalonia head via both name convention and `[Dialog]` attribute.
- [ ] Each built-in dialog opens, returns its result, and closes via
      `DialogWindow`/`DialogService` on the Avalonia head.
- [ ] `IDialogService` file/folder pickers implemented via Avalonia
      `IStorageProvider`; the async/sync bridging decision is recorded and a
      save/open/folder picker each round-trips a path on the Avalonia head.
- [ ] State-animation views converted; timeline draws keyframes, playhead/overlay,
      and interpolation tracks with Avalonia drawing, and responds to pointer input
      (scrub, select/move keyframes).
- [ ] Controls converted (spinner, color swatch/display, title-path) and behave;
      WPF color/brush usages mapped to Avalonia types.
- [ ] Alignment-button cluster converted; anchor/dock/alignment commands apply to
      the selected instance.
- [ ] No ViewModel was modified to accommodate a view (any needed VM change is
      flagged as a Phase 5 gap, not forked here).
- [ ] All ~48 tool views are accounted for: each is either converted to AXAML or
      explicitly deferred with a reason (no silent gaps). The 14 `Gum/Themes/`
      dictionaries and `App.xaml`/`MainWindow.xaml` are excluded by design.
- [ ] Avalonia head builds and runs with the new grid + converted views; WPF head
      still builds (until WPF removal in a later phase).
