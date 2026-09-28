# Gum tool functionality inventory

> Living document. Created 2026-09-26 for #5141 from static reads of `main` (b3d7ffde3); nothing
> was run. It lists what the Avalonia head can do, as the source for the nightly end-to-end suite.

## How to use this list

- One item per user-visible action. IDs are stable: never renumber or reuse one. Add new items at
  the end of their area.
- `tested:` names a test class that already exercises the item, found by searching the test
  projects. Most are unit or headless-view tests, not end-to-end runs. A blank item has no test found.
- An end-to-end test tags itself `[Trait("Feature", "<ID>")]`. The end-to-end suite is
  `Tests/Gum.Avalonia.Tests/EndToEnd/`, tagged `[Trait("Category", "EndToEnd")]`. A skipped test
  does not make its item `tested:`.
- `pwsh Tools/e2e-coverage.ps1` lists the items no non-skipped end-to-end test tags, per area.
- Sources swept: `StandardMenuModelBuilder` and every plugin `AddMenuEntry`, every context menu
  builder, `HotkeyManager` and the per-view key handlers, `DialogViewRegistry`, `TabViewRegistry`
  and plugin `AddControl` calls, the displayer registries, every drag-drop handler, `HeadOptions`,
  `CommandLineManager`, `ProjectPropertiesViewModel`, and `Tools/Gum.Cli`.

## File menu (FILE)

- FILE-001 New Project. tested: NewProjectLogicTests, ProjectOracleTests, DialogScenarioTests, FormsAndImportScenarioTests
- FILE-002 Load Project. tested: DialogScenarioTests
- FILE-003 Load Recent, pick a listed project. tested: RecentFilesLogicTests
- FILE-004 Load Recent > More... dialog. tested: LoadRecentViewModelTests, DialogScenarioTests
- FILE-005 Save Project
- FILE-006 Save All. tested: ExternalChangeScenarioTests
- FILE-007 Export > Export as Image
- FILE-008 Export > Export to SVG. tested: SvgExportMenuLogicTests, FileMenuScenarioTests
- FILE-009 Auto-save after each edit. tested: UndoManagerTests, VariableScenarioTests
- FILE-010 Last project reopens on launch
- FILE-011 Legacy project upgrades on load. tested: OldProjectLoadTests
- FILE-012 Load a `.gumj` (JSON) project. tested: JsonProjectFormatRoundTripTests
- FILE-013 External file change reloads element. tested: FileChangeReactionLogicTests, ExternalChangeScenarioTests
- FILE-014 Save refuses with no project loaded
- FILE-015 External change to an element with unsaved edits (Auto Save off) asks to reload or keep. tested: FileChangeReactionLogicTests, ExternalChangeScenarioTests

## Edit menu (EDIT)

- EDIT-001 Undo. tested: UndoManagerTests, TreeScenarioTests, VariableScenarioTests, FormsAndImportScenarioTests
- EDIT-002 Redo. tested: UndoManagerTests, TreeScenarioTests, VariableScenarioTests, FormsAndImportScenarioTests
- EDIT-003 Undo/Redo enabled state follows history
- EDIT-004 Add > Screen. tested: AddScreenDialogViewModelTests
- EDIT-005 Add > Component. tested: AddComponentDialogViewModelTests
- EDIT-006 Add > Instance. tested: AddInstanceDialogViewModelTests
- EDIT-007 Add > State. tested: AddStateDialogViewModelTests
- EDIT-008 Remove > Element. tested: ElementDeleteServiceTests
- EDIT-009 Remove > State or category. tested: DeleteLogicStateTests
- EDIT-010 Remove > Variable (behavior variable)
- EDIT-011 Remove items enable by selection. tested: MenuStripStateLogicTests
- EDIT-012 Properties opens Project Properties

## View menu and shell (VIEW)

- VIEW-001 Theming dialog. tested: ThemingDialogViewModelTests, TabViewScenarioTests
- VIEW-002 Standards palette toggle. tested: ElementTreeViewManagerStandardsPaletteTests, TabViewScenarioTests
- VIEW-003 Hide Tools / Show Tools. tested: HideShowToolsLogicTests, TabViewScenarioTests
- VIEW-004 View Hotkeys toggles tab. tested: MainHotkeyPluginTests, TabViewScenarioTests
- VIEW-005 Show File Watch. tested: TabViewScenarioTests
- VIEW-006 View Animations toggles tab. tested: TabLifecycleTests, TabViewScenarioTests
- VIEW-008 Tab auto-selects on relevant selection. tested: TabAutoSelectLogicTests, TabViewScenarioTests
- VIEW-009 Window size and position persist. tested: WindowSettingsLogicTests
- VIEW-010 Title shows project and unsaved state. tested: ShellTitlePluginTests, TabViewScenarioTests
- VIEW-011 UI font size scales panels. tested: UiFontSizeEndToEndTests, TabViewScenarioTests
- VIEW-012 Light and dark theme. tested: ThemeResourceTests, TabViewScenarioTests
- VIEW-013 Accent color. tested: AccentPaletteTests, TabViewScenarioTests
- VIEW-014 macOS native menu with About Gum. tested: NativeMenuBuilderTests, TabViewScenarioTests
- VIEW-015 Mouse back/forward step selection history. tested: AppWideWindowGesturesTests, TabViewScenarioTests
- VIEW-016 Startup failure shows error panel

## Content menu (CONT)

- CONT-001 Find file references
- CONT-002 Clear Font Cache. tested: FontManagerTests
- CONT-003 Re-create missing font files
- CONT-004 Force re-create all font files
- CONT-005 View Font Cache
- CONT-006 Scan for Orphaned Code Files. tested: OrphanCodeFileReporterTests
- CONT-007 Import > HTML. tested: ImportHtmlOptionsViewModelTests
- CONT-008 Import > .gumx. tested: ImportFromGumxViewModelTests, FormsAndImportScenarioTests
- CONT-009 Convert to JSON. tested: ConvertToJsonLogicTests
- CONT-010 Add Forms Components. tested: GumFormsLogicTests, FormsAndImportScenarioTests

## Plugins and Help menus (PLUG, HELP)

- PLUG-001 Manage Plugins dialog. tested: PluginsDialogViewModelTests
- PLUG-002 Enable or disable a plugin. tested: PluginEnablementStoreTests
- PLUG-003 Plugin folder scan report. tested: PluginScanReportTests
- PLUG-004 Add Skia Standard Elements. tested: SkiaShapeStandardsLogicTests
- PLUG-005 Refuse WPF-built plugins with a reason. tested: PluginInstantiatorTests
- PLUG-006 Every plugin composes. tested: PluginHostTests
- HELP-001 About shows version. tested: ToolVersionTests
- HELP-002 Third-Party Licenses
- HELP-003 View Docs opens browser
- HELP-004 Open Settings Folder

## Project tree (TREE)

- TREE-001 Click selects element. tested: TreeNodeMouseDownSelectionLogicTests
- TREE-002 Ctrl+click multi-select. tested: TreeNodeClickDispatchLogicTests, TreeScenarioTests
- TREE-003 Shift+click range select. tested: TreeNodeRangeSelectionLogicTests
- TREE-004 Arrow-key navigation. tested: TreeNodeKeyNavigationLogicTests
- TREE-005 Expand and collapse nodes. tested: CollapseToggleServiceTests
- TREE-006 Expanded state persists. tested: TreeViewStateServiceTests
- TREE-007 Tree follows canvas selection. tested: TreeSelectionSyncTests
- TREE-008 Search box filters project. tested: ProjectSearchBoxTests, TreeScenarioTests
- TREE-009 Search "Include Variables". tested: TreeScenarioTests
- TREE-010 Clear search button. tested: ProjectSearchBoxTests, TreeScenarioTests
- TREE-011 Error icon on broken element. tested: MainTreeViewPluginErrorIndicatorTests, VariableScenarioTests
- TREE-012 Node icons by type. tested: TreeNodeImageLogicTests
- TREE-013 Project title: View in explorer. tested: ProjectTitleContextMenuBuilderTests
- TREE-014 Project title: Copy full path. tested: ProjectTitleContextMenuBuilderTests
- TREE-015 Screens node: Add Screen. tested: TreeScenarioTests
- TREE-016 Screens node: Import Screen. tested: ImportScreenDialogTests
- TREE-017 Screens/Components node: Add Folder. tested: AddFolderDialogViewModelTests, TreeScenarioTests
- TREE-018 Components node: Add Component. tested: TreeScenarioTests
- TREE-019 Components node: Import Components. tested: ImportComponentDialogTests, DialogScenarioTests
- TREE-020 Behaviors node: Add Behavior. tested: TreeScenarioTests
- TREE-021 Behaviors node: Import Behavior. tested: ImportBehaviorDialogTests
- TREE-022 Folder/category node: View in explorer. tested: ElementTreeViewManagerNullSafetyTests
- TREE-023 Folder: Rename Folder. tested: RenameFolderDialogViewModelTests, TreeScenarioTests
- TREE-024 Folder: Delete Folder(s). tested: TreeScenarioTests
- TREE-025 Element: View References. tested: DisplayReferencesDialogTests, TreeScenarioTests
- TREE-026 Element: Copy Full Path
- TREE-027 Element: Duplicate. tested: TreeScenarioTests, CopyPasteRenameScenarioTests
- TREE-028 Element: Delete (one or many). tested: ElementDeleteServiceTests, TreeScenarioTests
- TREE-029 Element: Force Save Object. tested: TreeScenarioTests
- TREE-030 Component: Add/Remove Favorites. tested: FavoriteComponentManagerTests, TreeScenarioTests
- TREE-031 Instance: Go to definition. tested: TreeScenarioTests
- TREE-032 Instance: Create Component. tested: CreateComponentDialogViewModelTests, TreeScenarioTests
- TREE-033 Instance: Lock / Unlock. tested: TreeScenarioTests
- TREE-034 Instance: Duplicate (one or many). tested: TreeScenarioTests
- TREE-035 Instance: Delete (one or many). tested: InstanceDeletionHelperTests, TreeScenarioTests
- TREE-036 Instance: Add parent object. tested: TreeScenarioTests
- TREE-037 Instance: Add to base element. tested: TreeScenarioTests
- TREE-038 Behavior: Rename. tested: EditCommandsTests, TreeScenarioTests
- TREE-039 Behavior: Delete. tested: DeleteObjectPluginTests, TreeScenarioTests
- TREE-040 Standard element: View in explorer / Force Save
- TREE-041 Mixed selection: Delete N items. tested: TreeScenarioTests
- TREE-042 Delete key deletes selection. tested: TreeScenarioTests, CopyPasteRenameScenarioTests
- TREE-043 F2 renames element. tested: RenameElementDialogViewModelTests, TreeScenarioTests, VariableScenarioTests, CopyPasteRenameScenarioTests
- TREE-044 Ctrl+C / Ctrl+V instances. tested: CopyPasteLogicDestinationTests, TreeScenarioTests, CopyPasteRenameScenarioTests
- TREE-045 Ctrl+X cut instances. tested: TreeScenarioTests
- TREE-046 Ctrl+D duplicate. tested: TreeScenarioTests
- TREE-047 Alt+Up / Alt+Down reorder instance. tested: TreeScenarioTests
- TREE-048 Newly added instance scrolls into view. tested: ElementTreeViewManagerAddInstanceScrollTests
- TREE-049 Tree refresh keeps selection. tested: ElementTreeViewManagerRefreshTests
- TREE-050 Element/instance: Add object (standard type or favorite). tested: TreeScenarioTests

## Standards palette (PAL)

- PAL-001 Chip click: Add to current element (inside a selected container). tested: StandardsPaletteAddTests, DisplayPropertiesScenarioTests
- PAL-002 Right-click: Add to current element. tested: StandardsPaletteAddTests
- PAL-003 Right-click: Edit defaults...
- PAL-004 Drag chip onto tree. tested: StandardsPaletteAddTests
- PAL-005 Drag chip onto canvas

## Editor canvas (CANV)

- CANV-001 Click selects instance. tested: CanvasScenarioTests
- CANV-002 Shift+click multi-select. tested: SelectionManagerRectangleTests, CanvasScenarioTests
- CANV-003 Marquee select. tested: SelectionManagerRectangleTests, CanvasScenarioTests
- CANV-004 Click nested component child. tested: SelectionManagerNestedComponentClickResolutionTests
- CANV-005 Drag to move. tested: CanvasScenarioTests
- CANV-006 Shift-drag locks to axis. tested: CanvasScenarioTests
- CANV-007 Resize handles. tested: CanvasScenarioTests
- CANV-008 Shift keeps aspect ratio. tested: CanvasScenarioTests
- CANV-009 Alt resizes from center. tested: CanvasScenarioTests
- CANV-010 Resize with non-pixel units. tested: ResizeInputHandlerNonPixelUnitTests
- CANV-011 Resize past zero flips. tested: ResizeInputHandlerFlipTests
- CANV-012 Rotation handle. tested: CanvasScenarioTests
- CANV-013 Shift snaps rotation to 15 degrees. tested: CanvasScenarioTests
- CANV-014 Polygon point drag, add, remove. tested: CanvasScenarioTests
- CANV-015 Locked instance can't move
- CANV-016 Hover highlight. tested: SelectionManagerHighlightTests
- CANV-017 Snap to Grid toggle. tested: MoveInputHandlerGridSnapTests
- CANV-018 Grid Size field. tested: GridSnapperTests
- CANV-019 Grid overlay. tested: GridOverlayCalculatorTests
- CANV-020 Zoom +/- buttons and zoom combo. tested: CanvasScenarioTests
- CANV-021 Ctrl+wheel zoom. tested: WheelZoomAccumulatorTests, CanvasScenarioTests
- CANV-022 Middle-drag / Space-drag pan. tested: CameraControllerTests, CanvasScenarioTests
- CANV-023 Scrollbars pan. tested: ScrollbarServiceTests, CanvasScenarioTests
- CANV-024 Canvas size preset combo
- CANV-025 Font Scale +/-
- CANV-026 Preview in runtime button. tested: PreviewLauncherTests
- CANV-027 Rulers. tested: CanvasScenarioTests
- CANV-028 Drag guide out of ruler. tested: CanvasScenarioTests
- CANV-029 Dimension and distance display
- CANV-030 Checkerboard / background color. tested: BackgroundManagerTests
- CANV-031 Right-click: Bring to Front. tested: RightClickViewModelTests
- CANV-032 Right-click: Move Forward. tested: RightClickViewModelTests
- CANV-033 Right-click: Move In Front Of. tested: RightClickViewModelTests
- CANV-034 Right-click: Move Backward. tested: RightClickViewModelTests
- CANV-035 Right-click: Send to Back. tested: RightClickViewModelTests
- CANV-036 Right-click: Add child object (standard type). tested: RightClickViewModelTests
- CANV-037 Right-click: Add child from Favorited Components
- CANV-038 Right-click: Lock / Unlock. tested: RightClickViewModelTests
- CANV-039 Right-click menu shows only with instance selected
- CANV-040 Zoom to fit selected element. tested: CanvasZoomToFitTests
- CANV-041 Canvas redraws on change. tested: CanvasRedrawTests
- CANV-042 Custom renderables (Skia shapes, Lottie). tested: WireframeObjectManagerCustomRenderableTests

## Variables grid (VAR)

- VAR-001 Edit a text value. tested: VariableEditScenarioTests, VariableScenarioTests
- VAR-002 Edit commits on focus loss. tested: VariableFocusLossScenarioTests, VariableScenarioTests
- VAR-003 Filter box. tested: VariableFilterServiceTests, VariableScenarioTests
- VAR-004 Escape clears filter. tested: VariablesTabTests, VariableScenarioTests
- VAR-005 Category collapse. tested: VariableScenarioTests
- VAR-006 Label drag scrubs number. tested: LabelDragScrubLogicTests, VariableScenarioTests
- VAR-007 Multi-select edit. tested: MultiSelectCommitLogicTests, VariableScenarioTests
- VAR-008 State banner shows edited state. tested: StateEditingIndicatorServiceTests, VariableScenarioTests
- VAR-009 Row: Make Default. tested: VariableEditScenarioTests, VariableScenarioTests
- VAR-010 Row: Copy Qualified Variable Name. tested: VariableGridEntryTests, VariableScenarioTests
- VAR-011 Row: Expose Variable. tested: VariableMenuScenarioTests, VariableScenarioTests
- VAR-012 Row: Un-expose Variable. tested: VariableMenuScenarioTests, VariableScenarioTests
- VAR-013 Row: Show on Instances. tested: VariableScenarioTests
- VAR-014 Row: Hide from Instances. tested: VariableMenuScenarioTests, VariableScenarioTests
- VAR-015 Row: Delete Variable. tested: VariableMenuScenarioTests, VariableScenarioTests
- VAR-016 Row: Edit / Rename Variable. tested: EditVariableServiceTests, VariableScenarioTests
- VAR-017 Row: copy variable reference. tested: VariableScenarioTests
- VAR-018 Composite row: Expose/Un-expose channels. tested: CompositeMemberLogicApplyTests, VariableScenarioTests
- VAR-019 Category: Copy Values. tested: VariableCategoryCopyPasteServiceTests, VariableScenarioTests
- VAR-020 Category: Paste Values. tested: VariableCategoryCopyPasteUndoTests, VariableScenarioTests
- VAR-021 Add Variable button. tested: AddVariableButtonVisibilityLogicTests, VariableScenarioTests
- VAR-022 Behavior variable: Edit Variable. tested: VariableGridMainControlViewModelTests, VariableScenarioTests
- VAR-023 Behavior variable: Delete Variable. tested: DeleteVariableServiceTests, VariableScenarioTests
- VAR-024 Variable references (VariableReferences row). tested: VariableReferenceLogicTests, VariableScenarioTests
- VAR-025 F12 on reference goes to source. tested: VariableScenarioTests
- VAR-026 Parent dropdown. tested: AvailableParentsTypeConverterTests, VariableScenarioTests
- VAR-027 State dropdown on instance. tested: StateReferencingInstanceMemberTests, VariableScenarioTests
- VAR-028 Base type change. tested: ElementSaveDisplayerBaseTypeChangeTests, VariableScenarioTests
- VAR-029 Hidden vars by type/version. tested: ShapeVariableExclusionLogicTests, VariableScenarioTests
- VAR-030 Font value change regenerates font. tested: FontTypeConverterTests, VariableScenarioTests
- VAR-031 Behavior-required rows. tested: BehaviorShowingLogicTests, VariableScenarioTests
- VAR-032 Duplicate variable warning. tested: VariableScenarioTests
- VAR-033 Error row for bad value. tested: ErrorCheckOncePerEditTests, VariableScenarioTests
- VAR-034 Ctrl+E focuses filter. tested: PropertyGridManagerTests, VariableScenarioTests

## Grid displayers (DISP)

- DISP-001 TextBox. tested: TextBoxDisplayLogicTests, VariableScenarioTests
- DISP-002 MultiLineTextBox. tested: PropertyGridManagerStringDisplayerTests, VariableScenarioTests
- DISP-003 CheckBox. tested: SimpleEditorTests, VariableScenarioTests
- DISP-004 NullableBool, on a behavior's `bool?` Forms property. tested: SimpleEditorTests, DisplayPropertiesScenarioTests
- DISP-005 ComboBox. tested: SimpleEditorTests, VariableScenarioTests
- DISP-007 ListBox. tested: CompositeEditorTests
- DISP-008 Slider. tested: SimpleEditorTests
- DISP-010 AngleSelector. tested: CompositeEditorTests
- DISP-011 FileSelection. tested: FilePickingTests
- DISP-012 MultiFile. tested: CompositeEditorTests
- DISP-013 StringList. tested: CompositeEditorTests, VariableScenarioTests
- DISP-015 ToggleButtonOption. tested: CompositeEditorTests
- DISP-016 Color (picker and hex). tested: CompactColorPickerTests, VariableScenarioTests
- DISP-017 CornerRadius (linked/unlinked). tested: VariablesTabTests
- DISP-018 RemoveButton. tested: VariablesTabTests
- DISP-019 ChildrenLayout toggles. tested: VariablesTabTests, VariableScenarioTests
- DISP-020 Width/Height Units toggles. tested: VariablesTabTests
- DISP-021 X/Y Units toggles. tested: VariableGridToggleOptionsTests
- DISP-022 X/Y Origin toggles. tested: VariableGridToggleOptionsTests
- DISP-023 Text H/V Alignment toggles. tested: VariablesTabTests
- DISP-024 Text overflow H/V mode toggles

## States tab (STATE)

- STATE-001 Select a state. tested: StateTreeViewModelTests, VariableScenarioTests
- STATE-002 "+ New category" button. tested: StateScenarioTests
- STATE-003 Add State. tested: StateTreeRightClickViewModelTests, StateScenarioTests
- STATE-004 Add Category. tested: AddCategoryDialogViewModelTests, StateScenarioTests
- STATE-005 Paste Category. tested: StateTreeRightClickViewModelTests, StateScenarioTests
- STATE-006 Rename state. tested: RenameLogicTests, StateScenarioTests
- STATE-007 Delete state. tested: CrossElementStateUndoTests, StateScenarioTests
- STATE-008 Duplicate state. tested: StateTreeRightClickViewModelTests, StateScenarioTests
- STATE-009 Set state variables to default. tested: StateTreeRightClickViewModelTests, StateScenarioTests
- STATE-010 Move Up / Move Down. tested: StateTreeRightClickViewModelTests, StateScenarioTests
- STATE-011 Move to category. tested: StateTreeRightClickViewModelTests, StateScenarioTests
- STATE-012 Rename category. tested: RenameLogicTests, StateScenarioTests
- STATE-013 Sort category alphabetically. tested: StateTreeRightClickViewModelTests, StateScenarioTests
- STATE-014 Copy category. tested: StateTreeKeyboardHandlerTests, StateScenarioTests
- STATE-015 Delete category. tested: CrossElementStateUndoTests, StateScenarioTests
- STATE-016 Keyboard: Delete, F2, Ctrl+C/V, Alt+Up/Down. tested: StateTreeKeyboardHandlerTests, StateScenarioTests
- STATE-017 Edited-state and behavior-required markers. tested: StateScenarioTests
- STATE-018 Category color and sort. tested: CategorySortAndColorLogicTests, VariableScenarioTests

## Animations tab (ANIM)

- ANIM-001 Add Animation. tested: AnimationListScenarioTests, AnimationScenarioTests
- ANIM-002 Rename Animation. tested: AnimationRenameManagerTests, AnimationScenarioTests
- ANIM-003 Delete Animation. tested: AnimationListScenarioTests, AnimationScenarioTests
- ANIM-004 Duplicate Animation. tested: DuplicateServiceTests, AnimationScenarioTests
- ANIM-005 Set to Looping / Single Play. tested: AnimationListScenarioTests, AnimationScenarioTests
- ANIM-006 Squash/Stretch Frame Times. tested: KeyframeEditingTests, AnimationScenarioTests
- ANIM-007 Add State keyframe. tested: KeyframeEditingTests, AnimationScenarioTests
- ANIM-008 Add Sub-Animation keyframe. tested: SubAnimationNestingTests, AnimationScenarioTests
- ANIM-009 Add Named Event keyframe. tested: KeyframeEditingTests, AnimationScenarioTests
- ANIM-010 Delete keyframe. tested: KeyframeEditingTests, AnimationScenarioTests
- ANIM-011 Edit keyframe time and interpolation. tested: DetailColumnTests, AnimationScenarioTests
- ANIM-012 Timeline scrub and time box. tested: TimelineEndToEndTests, AnimationScenarioTests
- ANIM-013 Play / stop. tested: PlaybackTests, AnimationScenarioTests
- ANIM-014 Broken keyframe marker. tested: AnimationErrorTests, AnimationScenarioTests
- ANIM-015 Uncategorized-state warning. tested: AnimationErrorTests, AnimationScenarioTests
- ANIM-016 List keys: reorder, delete, copy, paste. tested: AnimationTabKeyHandlerTests, AnimationScenarioTests
- ANIM-017 Instance sub-animations. tested: InstanceSubAnimationTests, AnimationScenarioTests
- ANIM-018 External file change reloads. tested: ExternalChangeTests, AnimationScenarioTests
- ANIM-019 Element rename/delete updates animations. tested: ElementLifecycleTests, CopyPasteRenameScenarioTests

## Texture Coordinates tab (TEX)

- TEX-001 Shows selected sprite/NineSlice texture. tested: TextureCoordinateDisplayControllerTests, TextureCoordinateTabScenarioTests
- TEX-002 Drag region to set coords. tested: RectangleSelectorTests, TextureCoordinateTabScenarioTests
- TEX-003 Resize region handles. tested: RectangleSelectorDragRoundingTests, TextureCoordinateTabScenarioTests
- TEX-004 Snap to grid. tested: MainControlViewModelTests, TextureCoordinateTabScenarioTests
- TEX-005 Zoom. tested: TextureCoordinateDisplayScaleTests, TextureCoordinateTabScenarioTests
- TEX-006 Exposed texture coordinates. tested: ExposedTextureCoordinateLogicTests, TextureCoordinateTabScenarioTests
- TEX-007 Background. tested: BackgroundManagerTests

## Code tab (CODE)

- CODE-001 Preview generated code. tested: CodeWindowViewModelTests, CodeGenScenarioTests
- CODE-002 Generate button. tested: CodeOutputTabTests, CodeGenScenarioTests
- CODE-003 Manual / Auto generation. tested: CodeOutputTabControllerTests, CodeGenScenarioTests
- CODE-004 Code Project Root. tested: CodeOutputSettingsMembersTests, CodeGenScenarioTests
- CODE-005 Output Library. tested: CodeOutputSettingsMembersTests, CodeGenScenarioTests
- CODE-006 Object Instantiation Type. tested: CodeOutputSettingsMembersTests, CodeGenScenarioTests
- CODE-007 Project-wide using statements. tested: CodeOutputSettingsMembersTests, CodeGenScenarioTests
- CODE-008 Root Namespace / Append Folder. tested: CodeOutputSettingsMembersTests, CodeGenScenarioTests
- CODE-009 Default Screen Base. tested: CodeOutputSettingsMembersTests, CodeGenScenarioTests
- CODE-010 Adjust pixel values for density. tested: CodeOutputSettingsMembersTests, CodeGenScenarioTests
- CODE-011 Base types ignored. tested: CodeGenScenarioTests
- CODE-012 Generate DataTypes code. tested: CodeOutputSettingsMembersTests, CodeGenScenarioTests
- CODE-013 Element: Generation Behavior. tested: CodeOutputSettingsMembersTests, CodeGenScenarioTests
- CODE-014 Element: Using statements / Namespace. tested: CodeOutputSettingsMembersTests, CodeGenScenarioTests
- CODE-015 Element: Generated File Name. tested: CodeOutputSettingsMembersTests, CodeGenScenarioTests
- CODE-016 Element: Localize Element. tested: CodeGeneratorLocalizeTextTests, CodeGenScenarioTests
- CODE-017 Auto setup on first generate. tested: CodeGenerationAutoSetupServiceTests, CodeGenScenarioTests
- CODE-018 Rename element renames code files. tested: RenameServiceTests, CodeGenScenarioTests
- CODE-019 Delete element offers code delete. tested: CodeOutputPluginDeleteOptionsTests, CodeGenScenarioTests

## Project Properties (PROP)

- PROP-001 Auto Save. tested: ProjectPropertiesViewModelTests, ExternalChangeScenarioTests
- PROP-002 Canvas Width / Height. tested: ProjectPropertiesGridPresenterTests
- PROP-003 Show Outlines. tested: ProjectPropertiesGridPresenterTests
- PROP-004 Show Canvas Outline. tested: ProjectPropertiesViewModelTests
- PROP-005 Show Checker Background. tested: ProjectPropertiesChangeLogicTests
- PROP-006 Texture Filter. tested: ProjectPropertiesGridPresenterTests
- PROP-007 Restrict To Unit Values. tested: DialogScenarioTests
- PROP-008 Restrict File Names For Android. tested: ProjectPropertiesGridPresenterTests
- PROP-010 Localization Files. tested: ProjectPropertiesChangeLogicTests
- PROP-011 Language. tested: ProjectPropertiesViewModelTests
- PROP-012 Show Localization. tested: DialogScenarioTests
- PROP-013 Font Ranges. tested: ProjectPropertiesViewModelTests
- PROP-014 Use Font Character File. tested: ProjectPropertiesChangeLogicTests
- PROP-015 Font Spacing H / V. tested: DialogScenarioTests
- PROP-016 Auto-Size Font Outputs. tested: ProjectPropertiesGridPresenterTests
- PROP-017 Font Generator. tested: ProjectPropertiesGridPresenterTests
- PROP-018 Single Pixel Texture file and bounds. tested: ProjectPropertiesChangeLogicTests
- PROP-019 Close button. tested: ProjectPropertiesViewTests, DialogScenarioTests

## Other tabs (TAB)

- TAB-001 Output: log lines. tested: MainOutputViewModelTests, TabViewScenarioTests
- TAB-002 Output: Clear. tested: ToolPanelViewsTests, TabViewScenarioTests
- TAB-003 Errors: list and count. tested: AllErrorsViewModelTests, TabViewScenarioTests
- TAB-004 Errors: click selects source. tested: ErrorsTabTests, TabViewScenarioTests
- TAB-005 Errors: Copy / Copy All. tested: TabViewScenarioTests
- TAB-006 History: undo list. tested: UndosViewModelTests, TabViewScenarioTests
- TAB-007 History: click entry. tested: TabViewScenarioTests
- TAB-008 Alignment: anchor buttons. tested: TabViewScenarioTests
- TAB-009 Alignment: dock buttons. tested: TabViewScenarioTests
- TAB-010 Alignment: size to children. tested: TabViewScenarioTests
- TAB-011 Behaviors: edit component behaviors. tested: BehaviorsViewModelTests, TabViewScenarioTests
- TAB-012 Hotkeys: list bindings. tested: HotkeyViewModelTests, TabViewScenarioTests
- TAB-013 File Watch: list and print toggle. tested: FileWatchViewModelTests, TabViewScenarioTests
- TAB-014 Performance: sort and cull options. tested: PerformanceViewModelTests, TabViewScenarioTests

## Hotkeys (KEY)

- KEY-001 Ctrl+Z undo. tested: TreeScenarioTests, CanvasScenarioTests
- KEY-002 Ctrl+Y / Ctrl+Shift+Z redo. tested: HotkeyManagerTests, TreeScenarioTests, CanvasScenarioTests
- KEY-003 Ctrl+C copy. tested: TreeScenarioTests
- KEY-004 Ctrl+X cut. tested: TreeScenarioTests
- KEY-005 Ctrl+V paste. tested: CopyPasteLogicDestinationTests, TreeScenarioTests
- KEY-006 Ctrl+D duplicate. tested: TreeScenarioTests
- KEY-007 Delete. tested: TreeScenarioTests
- KEY-008 F2 rename. tested: HotkeyManagerTests, TreeScenarioTests
- KEY-009 F12 go to definition. tested: TabViewScenarioTests
- KEY-010 Ctrl+F search
- KEY-011 Ctrl+E variable filter. tested: PropertyGridManagerTests
- KEY-012 Arrow nudge 1px. tested: HotkeyManagerNudgeTests, CanvasScenarioTests
- KEY-013 Shift+arrow nudge 5px. tested: CanvasScenarioTests
- KEY-014 Ctrl+arrow move camera. tested: CameraControllerTests, CanvasScenarioTests
- KEY-015 Ctrl+plus / minus zoom. tested: CameraControllerTests, CanvasScenarioTests
- KEY-016 Alt+Up / Alt+Down reorder. tested: TreeScenarioTests
- KEY-017 Alt+Left / Alt+Right selection history. tested: SelectionHistoryTests, TabViewScenarioTests
- KEY-018 Ctrl+? show hotkeys. tested: MainHotkeyPluginTests, TabViewScenarioTests
- KEY-019 Key mapping to Avalonia gestures. tested: AvaloniaKeyMappingTests, TabViewScenarioTests

## Drag and drop (DRAG)

- DRAG-001 Tree: reorder instances. tested: ElementTreeViewManagerProcessDropTests
- DRAG-002 Tree: reparent instance. tested: ElementTreeViewManagerProcessDropTests
- DRAG-003 Tree: component onto element adds instance
- DRAG-004 Tree: element into folder
- DRAG-005 Tree: folder into folder
- DRAG-006 Tree: behavior onto component
- DRAG-007 Tree: instance onto behavior
- DRAG-008 Tree: drop indicator placement. tested: TreeDropLogicIndicatorIndentTests
- DRAG-009 Tree: search result drag
- DRAG-010 Tree: external files drop. tested: DragDropManagerTreeFileDropTests
- DRAG-011 Canvas: node drop adds instance. tested: DragDropManagerWireframeDropParentingTests
- DRAG-012 Canvas: drop onto instance parents it. tested: DragDropManagerWireframeDropParentingTests, CanvasScenarioTests
- DRAG-013 Canvas: image file drop. tested: FileDropTargetFilterTests, CanvasScenarioTests
- DRAG-014 Canvas: animation-chain file drop defaults. tested: AnimationChainDropDefaultsTests
- DRAG-015 Window: drop project file opens it. tested: ProjectFileDropLogicTests
- DRAG-016 Standards palette chip drag. tested: StandardsPaletteAddTests, CanvasScenarioTests

## Dialogs (DLG)

- DLG-001 Message. tested: MessageDialogViewModelTests, AnimationScenarioTests
- DLG-002 Get user string. tested: GetUserStringDialogViewModelTests, TreeScenarioTests, AnimationScenarioTests
- DLG-003 Choice. tested: ChoiceDialogViewModelTests
- DLG-004 New Project (Forms, DemoScreen options). tested: NewProjectLogicTests, DialogScenarioTests, FormsAndImportScenarioTests
- DLG-005 Add Screen / Component / Instance / State / Category / Folder. tested: AddScreenDialogViewModelTests, TreeScenarioTests
- DLG-006 Rename element. tested: RenameElementDialogViewModelTests, TreeScenarioTests
- DLG-007 Rename folder. tested: RenameFolderDialogViewModelTests
- DLG-008 Create Component from instances. tested: CreateComponentDialogViewModelTests
- DLG-009 Import Screen / Component / Behavior. tested: ImportBaseDialogViewModelTests, DialogScenarioTests
- DLG-010 Delete options (Y/N keys). tested: DeleteOptionsDialogTests
- DLG-011 Display references. tested: DisplayReferencesDialogTests
- DLG-012 Expose color. tested: ExposeColorDialogViewModelTests
- DLG-013 Theming. tested: ThemingDialogViewModelTests, DialogScenarioTests
- DLG-014 Load recent. tested: LoadRecentViewModelTests, DialogScenarioTests
- DLG-015 Plugins. tested: PluginsDialogViewTests, DialogScenarioTests
- DLG-016 Add/Edit variable. tested: AddVariableViewModelTests
- DLG-017 Add animation. tested: DialogFocusTests, AnimationScenarioTests
- DLG-018 Add state keyframe. tested: AnimationScenarioTests
- DLG-019 Sub-animation selection. tested: SubAnimationSelectionDialogViewModelTests, AnimationScenarioTests
- DLG-020 Freeze diagnostics prompt. tested: FreezeDiagnosticsPromptServiceTests
- DLG-021 Add Forms (theme choice). tested: AddFormsViewModelTests, FormsAndImportScenarioTests
- DLG-022 Import from .gumx (file or URL, subfolder, picks). tested: ImportFromGumxViewModelTests, FormsAndImportScenarioTests
- DLG-023 Standard diff details. tested: StandardDiffDetailsViewModelTests
- DLG-024 Convert to JSON. tested: ConvertToJsonLogicTests
- DLG-025 Import HTML options. tested: ImportHtmlOptionsViewModelTests
- DLG-026 Import HTML result. tested: ImportHtmlResultViewModelTests
- DLG-027 Ctrl+C copies message text. tested: DialogKeyboardTests
- DLG-028 Every view model has a view. tested: DialogViewRegistryTests

## Head command line (CLI)

- CLI-001 Positional project path opens it. tested: CommandLineManagerTests, HeadCommandLineScenarioTests
- CLI-002 `--exit-after`. tested: HeadOptionsTests, HeadCommandLineScenarioTests
- CLI-003 `--screenshot`. tested: HeadProcessTests, HeadCommandLineScenarioTests
- CLI-004 `--select Element[#Instance]`. tested: HeadProcessTests, HeadCommandLineScenarioTests
- CLI-005 `--theme light|dark`. tested: HeadCommandLineScenarioTests
- CLI-006 `--zoom-to-fit`. tested: HeadOptionsTests, HeadCommandLineScenarioTests
- CLI-007 `--user-data`. tested: HeadOptionsTests, HeadCommandLineScenarioTests
- CLI-008 `--rebuildfonts`. tested: CommandLineManagerTests, HeadCommandLineScenarioTests
- CLI-009 `--generatecode`. tested: CommandLineManagerTests, HeadCommandLineScenarioTests
- CLI-010 OS file activation opens project. tested: FileActivationHandlerTests, HeadCommandLineScenarioTests
- CLI-011 `GUM_ECHO_OUTPUT` echoes output. tested: HeadCommandLineScenarioTests

## gumcli (GCLI)

- GCLI-001 `new`. tested: NewCommandTests, GumCliScenarioTests
- GCLI-002 `check`. tested: CheckCommandTests, GumCliScenarioTests
- GCLI-003 `check-references`. tested: CheckReferencesCommandTests, GumCliScenarioTests
- GCLI-004 `codegen`. tested: CodegenCommandTests, GumCliScenarioTests
- GCLI-005 `codegen-init`. tested: CodegenInitCommandTests, GumCliScenarioTests
- GCLI-006 `fonts`. tested: FontsCommandTests, GumCliScenarioTests
- GCLI-007 `add-forms`. tested: AddFormsCommandTests, GumCliScenarioTests
- GCLI-008 `convert-to-json`. tested: ConvertToJsonCommandTests, GumCliScenarioTests
- GCLI-009 `screenshot`. tested: ScreenshotCommandTests, GumCliScenarioTests
- GCLI-010 `diff-screenshots`. tested: DiffScreenshotsCommandTests, GumCliScenarioTests
- GCLI-011 `diff-standards`. tested: DiffStandardsCommandTests, GumCliScenarioTests
- GCLI-012 `import-screen`. tested: ImportScreenCommandTests, GumCliScenarioTests
- GCLI-013 `pack`. tested: PackCommandTests, GumCliScenarioTests
- GCLI-014 `resave`. tested: ResaveCommandTests, GumCliScenarioTests
- GCLI-015 `svg`. tested: SvgCommandTests, GumCliScenarioTests
- GCLI-016 `stage-forms-behaviors`. tested: StageFormsBehaviorsCommandTests, GumCliScenarioTests

## Combinations (COMBO)

Cross-feature scenarios the code lists don't show. Most need a real project, a save and a reload.

- COMBO-001 Copy instance with variable reference into another element. tested: CopyPasteRenameScenarioTests
- COMBO-002 Paste instance whose parent isn't copied. tested: CopyPasteRenameScenarioTests
- COMBO-003 Paste instance into element lacking its component type's state. tested: CopyPasteRenameScenarioTests
- COMBO-004 Rename element other elements inherit from. tested: VariableScenarioTests
- COMBO-005 Rename component used as instances elsewhere. tested: VariableScenarioTests
- COMBO-006 Rename component referenced by animations. tested: CopyPasteRenameScenarioTests
- COMBO-007 Rename instance referenced by variable references. tested: CopyPasteRenameScenarioTests
- COMBO-008 Rename state used by instances and animations. tested: CopyPasteRenameScenarioTests
- COMBO-009 Rename category with exposed state variable. tested: CopyPasteRenameScenarioTests
- COMBO-010 Delete component used as instances. tested: VariableScenarioTests
- COMBO-011 Delete base element of inherited elements
- COMBO-012 Delete state used by animation keyframes. tested: AnimationScenarioTests
- COMBO-013 Delete instance that is a parent of others. tested: CopyPasteRenameScenarioTests
- COMBO-014 Undo across a state switch. tested: VariableScenarioTests
- COMBO-015 Undo across an element switch. tested: VariableScenarioTests
- COMBO-016 Undo a rename, then save and reload. tested: VariableScenarioTests
- COMBO-017 Undo a cascading delete. tested: CrossElementStateUndoTests
- COMBO-018 Undo all edits returns byte-identical files. tested: TreeScenarioTests
- COMBO-019 Edit in category state, then Make Default. tested: VariableScenarioTests
- COMBO-020 Expose variable, then set it on an instance. tested: VariableScenarioTests
- COMBO-021 Un-expose variable set on instances. tested: VariableScenarioTests
- COMBO-022 Change base type with instances and states set
- COMBO-023 Reparent across element via tree drag, then undo
- COMBO-024 Multi-select edit across different types
- COMBO-025 Duplicate element with states, animations and code settings
- COMBO-026 Create component from instances with references
- COMBO-027 Move element to folder, then codegen
- COMBO-028 Rename element with generated code, then regenerate
- COMBO-029 Animation plays state after its variables change
- COMBO-030 External file edit while element has unsaved change
- COMBO-031 Import .gumx components that collide with existing names. tested: FormsAndImportScenarioTests
- COMBO-032 Add Forms twice (idempotent). tested: FormsAndImportScenarioTests
- COMBO-033 Change localization language, then edit Text
- COMBO-034 Font variable change, then save and `gumcli fonts`
- COMBO-035 Convert to JSON, reload, full edit cycle
- COMBO-036 Locked instance: tree drag, nudge, alignment buttons
- COMBO-037 Behavior added to component, then required state removed
- COMBO-038 Paste category values to multi-selected instances
- COMBO-039 Selection history after deleting a visited element
- COMBO-040 Tree search, then rename the found element. tested: TreeScenarioTests
