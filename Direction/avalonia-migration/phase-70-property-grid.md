# Phase 70 — Property grid re-author and the Variables tab

## Purpose

Replace `WpfDataUi`, the bespoke WPF property grid behind the Variables tab and several plugin
panels, with an Avalonia implementation driven by the same member model, so the Variables tab,
the Code Output, Gum Forms, Import-from-gumx, and Skia plugin panels work in the Avalonia head.
After the canvases, this is the largest job in the plan.

## Builds on

- The Variables tab's logic is largely headless already: `Gum.Presentation/Plugins/InternalPlugins/VariableGrid`
  (33 files), `PropertyGridHelpers/Converters`, the Variables-tab `MainControlViewModel` (#3796),
  `IExposeVariableService`, `EditVariableService`, `VariableReferenceLogic`, and the recent filter
  box and Ctrl+E focus (#4632, #4644).
- `WpfDataUi` is small and self-contained: 51 source files, 18 XAML, 16 editor controls, the
  `DataUiGrid` `ItemsControl`, `SingleDataUiContainer`, `IDataUi`, and the model types
  `InstanceMember` / `CompositeInstanceMember` / `MemberCategory` / `IMemberDefinition`.
- Consumers: `Gum.csproj`, `CodeOutputPlugin`, `GumFormsPlugin`, `ImportFromGumxPlugin`, `SkiaPlugin`.
- `InlineChannelsDisplay` (#4661) is the newest editor and shows the current editor pattern.

## Decisions

- **Re-author, do not adopt a third-party grid.** `DataUiGrid`'s value is Gum's member model:
  categories with ordering, delegate-driven optional visibility, multi-select editing, custom
  getters/setters, per-member editor selection, and 16 custom editors (angle, color channels,
  file pickers, string lists, toggle-button options, plus/minus boxes, sliders, nullable bools).
  A generic reflection grid would still need every one of those ported.
- **Port the model first, as a `net10.0` assembly.** `InstanceMember`, `CompositeInstanceMember`,
  `MemberCategory`, `IMemberDefinition*`, `TypeMemberDisplayProperties`, and the editor-selection
  logic move out of `WpfDataUi` into a neutral project (`DataUi.Core`, or into `Gum.Presentation`
  if the dependency direction allows), with the `System.Windows` usings on `MemberCategory` /
  `InstanceMember` replaced by neutral types (ADR-0004). The WPF grid consumes the neutral model
  first, proving the split before any Avalonia code.
- **Editors implement the same `IDataUi` contract** so the grid drives them identically; the WPF
  and Avalonia editor sets are two implementations of one contract.
- **Logic-bearing editors reuse their extracted logic classes** (`TextBoxDisplayLogic`,
  `InlineChannelsDisplayLogic`, `FilePickingLogic`) unchanged; if an editor's logic is only in
  code-behind, extract it WPF-side with a test first.
- **Test every editor against the same fixture** in both heads: a member of each type, set and
  read back, with undo.
- **Two Windows-only mechanics inside the editors are redesigned, not ported.**
  `TextBoxDisplay` warps the mouse with `user32 SetCursorPos` during drag-to-change-value; there
  is no cross-platform cursor warp, so the Avalonia editor uses pointer capture and relative
  deltas. `FilePickingLogic` opens `Microsoft.Win32` dialogs directly; both heads go through
  `IDialogService`. The installed-font list (`FontTypeConverter` uses GDI+ `FontFamily.Families`)
  moves behind a small font-enumeration seam with a SkiaSharp implementation
  (`SKFontManager.Default.FontFamilies`), shared by both heads.

## Scope

**In:** neutral model project; `DataUiGrid` and `SingleDataUiContainer` in Avalonia; all 16 editors
in Avalonia; the Variables tab view; the four plugin consumers' grids; multi-select editing;
optional-visibility and category ordering; the filter box; keyboard focus behavior.

**Out:** changing what variables are shown or how defaults/overrides are computed (headless
already); theming of the grid (90).

## Editor inventory (WpfDataUi/Controls, 2026-09-09)

`AngleSelectorDisplay`, `CheckBoxDisplay`, `ComboBoxDisplay`, `EditableComboBoxDisplay`,
`FileSelectionDisplay`, `InlineChannelsDisplay`, `ListBoxDisplay`, `MultiFileDisplay`,
`MultiLineTextBoxDisplay`, `NullableBoolDisplay`, `PlusMinusTextBox`, `SliderDisplay`,
`StringListTextBoxDisplay`, `TextBoxDisplay`, `ToggleButtonOptionDisplay`, plus
`SingleDataUiContainer` (the row host) and `DataUiGrid` (the categorized list).

## Tasks

1. Extract the neutral model project; make the WPF grid consume it; `WpfDataUi` keeps only views.
2. Avalonia `DataUiGrid` + `SingleDataUiContainer`: categories, ordering, optional visibility,
   multi-select, editor selection by member type/attribute.
3. Editors, simplest first (`CheckBox`, `TextBox`, `ComboBox`, `NullableBool`, `Slider`,
   `PlusMinus`), then composite (`InlineChannels`, `AngleSelector`, `ToggleButtonOption`,
   `StringList`, `ListBox`, `MultiLineTextBox`), then file pickers (`FileSelection`, `MultiFile`)
   through `IDialogService`.
4. Variables tab view in Avalonia bound to the existing `MainControlViewModel`; filter box; Ctrl+E.
5. Plugin consumers: Code Output, Gum Forms, Import-from-gumx, Skia panels on the Avalonia grid.
6. Shared editor fixture test run in both heads; undo verified per editor.

## Key files

- `WpfDataUi/**`, especially `DataUiGrid.xaml`, `SingleDataUiContainer.xaml`, `IDataUi.cs`, `DataTypes/*`
- `Tools/Gum.Presentation/Plugins/InternalPlugins/VariableGrid/**`, `PropertyGridHelpers/Converters/*`
- `Gum/Plugins/InternalPlugins/VariableGrid/*.xaml` (3), `Gum/PropertyGridHelpers/Converters/*`
- `.claude/skills/gum-tool-variable-grid`, `gum-variable-deep-dive`

## Dependencies

Needs phases 30, 40. Independent of 50 and 60. Blocks phase 100's edit-variable parity run and
the four plugin panels.

## Risks

- `MemberCategory`/`InstanceMember` carry WPF types on their public surface today; the model
  extraction is the place this shows up, and it may ripple into `Gum.Presentation` callers.
- Focus and keyboard behavior in a virtualized Avalonia list differ from WPF; the filter box and
  tab-order behavior need explicit tests.
- The recent stale-`IndexEditing` crash fix in `ListBoxDisplay` (#4664) is a reminder that editor
  pooling/reuse has sharp edges; carry the fix's test into the Avalonia editor.

## Done when

- [ ] Neutral model project exists; WPF grid consumes it; WPF tool identical.
- [ ] All 16 editors + grid work in Avalonia; shared fixture test green in both heads.
- [ ] Variables tab and the four plugin panels function on all three OSes with undo.

## Status (2026-09-10)

Work is on the phase-70 branch, one commit per part.

- **Part 1: neutral model project.** `DataUi.Core` (net10.0, banned-API analyzer) now holds
  `InstanceMember`, `MemberCategory`, the composite and multi-select members, `DataUiGridModel`
  (categories, filter, expansion memory, multi-select grouping, reflection population),
  `DisplayerRegistry` with the neutral `StandardDisplayers` keys, and the editor logic
  (`TextBoxDisplayLogic` behind `IDataUiTextBox`, `FilePickingLogic` behind `IDataUiFilePicker`,
  `InlineChannelsDisplayLogic`, `LabelDragScrubLogic`). Namespaces stay `WpfDataUi.*`. The model's
  WPF types are gone: `HeaderColor` is `System.Drawing.Color?`, category visibility is
  `IsVisible`, `FirstGridLength` is a `double`, context-menu handlers are `EventHandler`, and
  `MemberCategoryContextMenuItem` raises its own `CanExecuteChanged` (the WPF template wraps it for
  `CommandManager.RequerySuggested`). `WpfDataUi` keeps only the views over the model. File
  pickers now go through `IDialogService` in both heads (`DialogServiceFilePicker`); the unused
  folder mode and the never-called `SetCursorPos` import are deleted.
- **Part 2: the Variables tab's logic is shared.** `PropertyGridManager`, `StateReferencingInstanceMember`,
  the composite logic and registry, the category row adapter, `StandardElementsManagerGumTool`, the
  drop-down type converters, `VariableTypeConverterProvider`, and `FilePickingFolderProvider` moved
  to `Gum.Presentation` and register in `AddGumCore`; five head-provided placeholders are gone. The
  plugin logic is `VariableGridPluginBase`/`ExclusionsPluginBase`, each head exporting a thin
  subclass. A head supplies one contract, `IVariableGridHead`: the tab view (`IVariablesTabView`)
  and its controls for the Gum-specific `GumDisplayers` keys. The filter box's predicate moved from
  the WPF view into `PropertyGridManager`. The installed-font list goes through
  `IInstalledFontProvider` (SkiaSharp).
- **Part 3: the Avalonia grid and the simple editors.** `AvaloniaDataUi` (net10.0, Avalonia,
  banned-API analyzer, views built in C#) holds `DataUiGrid` (an `IDataUiGrid` over
  `DataUiGridModel`: collapsible categories bound to `IsExpanded`/`IsVisible`/`HeaderColor`,
  category right-click menus built when they open), `SingleDataUiContainer` (editor choice through
  the grid's `DisplayerRegistry`, `PropertiesToSetOnDisplayer`, `UiCreated`, tooltips), and the
  text, multi-line, check box, nullable bool, combo box (and editable), slider, and plus/minus
  editors on a shared `DataUiDisplayBase` (member tracking, read-only disabling, detail text,
  right-click "Make Default" plus the member's entries). Label scrubbing uses pointer capture and
  relative deltas through `LabelDragScrubLogic`. The combo box's option list and the slider's
  display-multiplier math were extracted to `ComboBoxDisplayLogic`/`SliderDisplayLogic` and the WPF
  editors now use them. The Avalonia grid tints default and indeterminate values on every field;
  the WPF Variables tab's "is edited" row icon (`OverridesIsDefaultStyling`) is not reproduced, a
  theming decision left to phase 90. Rows are not pooled (Avalonia editors are cheap to build);
  re-binding a displayer resets its per-member state, as the WPF pooling fix (#4664) requires.
- **Part 4: the composite and file editors.** `AvaloniaDataUi` gains the angle (dial plus
  text), toggle-button option, string list, list box, file selection, multi-file, and inline
  channels editors, completing the standard registry. The logic that lived only in WPF
  code-behind was extracted first and the WPF editors now use it: `AngleSelectorLogic` (unit
  conversion, typed text and arithmetic, and dial winding past 180 degrees),
  `StringListLogic`, `ListBoxDisplayLogic`, and `MultiFileDisplayLogic`; `ToggleButtonOption`
  is the neutral option record. The dial drag uses pointer capture. The list box's bad-input
  message shows inline under the list instead of a WPF message box.
