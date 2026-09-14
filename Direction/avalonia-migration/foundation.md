# Foundation — what is already done on `main` (build on it, do not redo it)

> Inventory of the UI/logic decoupling work landed on `main` between 2026-06-20 and 2026-09-09,
> almost entirely by Victor Chelaru, under `ui-decoupling-plan.md` and ADRs 0003/0004/0005. Every
> phase in this folder assumes this foundation exists. If a phase doc tells you to "extract" or
> "neutralize" something listed here, the doc is stale — fix the doc. Measured on `main` at
> commit 15f91ae8a (2026-09-09).

## The one-paragraph version

The tool went from a WPF + WinForms hybrid with ~475 static `.Self` calls, two `WindowsFormsHost`s,
and all logic inside the `net8.0-windows` `Gum` project, to: a headless `net10.0` `Gum.Presentation`
assembly holding the ViewModels, the service interfaces, and most concrete logic (356 source files,
939 tests); zero `WindowsFormsHost`s; a native WPF element tree bound to a plain observable node
model; a canvas that renders to a texture, reads pixels back to CPU through a neutral interface,
and blits into a WPF bitmap; and a `Gum/` project whose remaining WPF references are almost
entirely view code. Roughly 210 PRs.

## By area

### Phase 0/1 — instrument, kill the shallow WinForms scatter (June 2026)
- Baseline ratchet (#3751, later dropped to zero and retired in favor of the compiler boundary).
- `GumKey` enum replaces WinForms `Keys` in `KeyCombination` (#3267); drag-drop effects neutralized
  (#3273); dead WinForms windows/dialogs deleted (#3241, #3245, #3260).
- File dialogs routed through `IDialogService` (#3253, #3254, #3264).
- `ITabManager` sealed off `System.Windows.Forms.Control` (#3249) and later off all WPF types:
  `AddControl(object, ...)` returns `IPluginTab` (#3752, #3955).

### Phase 2 — drain the statics (June–July 2026)
- ~40 PRs (#3274–#3340, #3757–#3775) converting `.Self` singletons and `Locator` ctor lookups to
  constructor injection, including every MEF plugin ctor (`[ImportingConstructor]`).
- `AllPluginsCompositionTests` guards that every plugin still composes (#3330).
- **Remaining `.Self` in `Gum/` today: 152, of which 118 are `ObjectFinder.Self` and the rest are
  the sanctioned runtime singletons** (`StandardElementsManager`, `LoaderManager`, `Cursor`). These
  are intentionally kept (see `refactoring-direction` skill). Nothing here blocks Avalonia.

### Phase 3 — extract logic into headless `Gum.Presentation` (June–July 2026, issue #3754, closed)
- Assembly stood up (#3342). Service interfaces relocated: `IHotkeyManager`, `IFileCommands`,
  `IElementCommands`, `ISelectedState`, `IGuiCommands`, `IUndoManager`, `IWireframeObjectManager`,
  `IDialogService`, `IReferenceFinder`, and more (#3345–#3364).
- Concrete logic relocated: `UndoManager`, `DeleteLogic`, `CopyPasteLogic`, `ReferenceFinder`,
  `RenameLogic`, `DragDropManager`, `CameraController`, `SelectionManager`, the wireframe
  input-handler family, `HotkeyManager`, `PluginBase`, `IPlugin`, and dozens of others
  (#3740–#3746, #3850–#3855, #3938, #3941, #3955).
- ~35 ViewModels moved per ADR-0004 (neutral types: `bool` not `Visibility`, etc.) (#3768–#3796).
- `DialogViewResolver` resolves views across assemblies (#3781).
- Tests moved with the code into `Tests/Gum.Presentation.Tests` (net8.0, 939 tests).

### Phase 4a — element tree off WinForms `TreeNode` (July–Aug 2026, issues #3755, #3845, #4228, closed)
- `ElementTreeViewManager` typed against `ITreeNode`/`ITreeNodeMutable` (#3963); its search,
  refresh, sort, and navigation logic mirrored as headless extensions (#3814–#3820, #3847).
- `MultiSelectTreeView`'s click, range-select, and keyboard-walk decisions extracted into testable
  classes (#3821, #3822, #3824, #3830).
- **Ported to a native WPF `TreeView` (#4230).** `GumTreeNode` is now a plain observable class
  implementing `ITreeNodeMutable`. The last `WindowsFormsHost` is gone. Design record:
  `../treeview-wpf-port.md`.

### Phase 4b — canvas off WinForms (July–Aug 2026, issue #3833, closed)
- Pixel readback formalized behind `IRenderTargetPixelBufferWriter` (#3829); a `WriteableBitmap`
  writer (#3834); `WpfRenderSurfaceHost` at 45–60 fps including 4K (#3837); `WpfInputHostAdapter`
  implementing `IInputHostControl` (#3842); neutral drop payload + WPF reader (#3844);
  `CameraController` events off WinForms (#3843).
- **Wireframe canvas hosted natively in WPF (#4166); texture-coordinate canvas likewise (#4226);
  WinForms render/input path deleted (#4227).** Both derive from `WpfGraphicsDeviceControl` in
  `XnaAndWinforms`. WPF-native scroll bars (#4156). Grid overlay and snapping (#4381).
- Lessons recorded in `ui-decoupling-plan.md` Phase 4b: device is created against the main window
  handle and ref-counted via `GraphicsDeviceService`; size surface/cursor/viewport in one unit
  system; `CompositionTarget.Rendering` as the render trigger; re-apply drag effect on every move.

### Menus and plugins
- `MenuStripManager` and the state-tree right-click service use a neutral-VM menu pattern (#3954);
  `WpfPluginBase` split out of the headless `PluginBase` (#3943), so only WPF-menu-needing plugins
  inherit WPF.
- Plugin business logic pulled headless in bulk (#3949, #3946, #3937, #3953).

## What this leaves — the measured remainder in `Gum/` (net8.0-windows)

| Category | Count | Notes |
|---|---|---|
| Source files referencing `System.Windows.*` / WinForms | 146 of 349 | |
| …of which view code-behind (`.xaml.cs`) | 54 | expected: rewritten as AXAML |
| …of which ViewModels | 3 | decoupling gaps, fix WPF-side first |
| …of which converters / controls / themes / behaviors / dialogs / input glue | 89 | view layer, expected |
| XAML files in `Gum/` proper | 51 | Themes 15, Controls 9, Dialogs 8, plugin views 19 |
| XAML files in plugin projects | 14 | StateAnimation 7, ImportFromGumx 2, five others 1 each |
| XAML files in `WpfDataUi` | 18 | the property grid: 16 editors + container + grid |
| Plugin projects targeting `net8.0-windows` | 11 | all internal-in-repo; see phase 40 |
| DI registrations in `Gum/Services/Builder.cs` | 127 | still in the WPF project; see phase 20 |
| Third-party WPF-only packages / DLL refs | 10 | MaterialDesignThemes, ControlzEx, FluentIcons.Wpf, PixiEditor.ColorPicker, SharpVectors, SkiaSharp.Views.WPF, Xceed AvalonDock + Toolkit + DataGrid (vestigial), Microsoft.AppCenter, System.Management (dead) |
| GDI+ (`System.Drawing.Common`) call sites that throw off Windows | 3 live | `ImageHeader` fallback in **`Gum.Presentation`**, `FontTypeConverter`, `ThemedScrollbar`; see `coverage-matrix.md` §3 |
| P/Invoke / registry sites | 6 | window activation, monitor DPI, chrome hit-test, cursor warp, OS dark-mode registry read; see `coverage-matrix.md` §4 |
| Shell / process launches with Windows names | 4 | `explorer.exe`, `cmd.exe`, `gumcli.exe`, `Gum.exe`; plus shipped `bmfont.exe`; see `coverage-matrix.md` §5 |

Helper projects still `net8.0-windows`: `XnaAndWinforms` (device service + WPF surface host),
`InputLibrary` (`Cursor`, `WpfInputHostAdapter`; references KNI and `Gum.Presentation`),
`FlatRedBall.SpecializedXnaControls` (`ImageRegionSelectionControl`, the second canvas),
`WpfDataUi`, `CsvLibrary` (no actual WPF use; TFM only), `CommonFormsAndControls` (empty, net8.0).

The full sweep, with every site and its owning phase, is `coverage-matrix.md`.

## Canvas backend — the one thing the foundation did not touch

The editor's only graphics backend is `nkast.Kni.Platform.WinForms.DX11` at
`GraphicsProfile.FL10_0`, created against a window handle in `XnaAndWinforms/GraphicsDeviceService.cs`.
This is Windows-only and is the exact root cause of the macOS Wine failure (Wine offers only 9.3).
Nothing on `main` has attempted a non-Windows device *for the editor*. But `Gum.ProjectServices.MonoGame`
already creates one for `gumcli screenshot` with `MonoGame.Framework.DesktopGL`, and CI runs it on
macOS and under Mesa software GL on Windows. That precedent is phase 10's starting point.
