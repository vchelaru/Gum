# WPF features the Avalonia tool lacks

> Living document. Created 2026-09-26 for #5121 from static reads of both heads on `main`
> (2e7c997e0); nothing was run in either tool. Update a row when its issue closes. Per-gap work
> is tracked in the linked issues, not here.

**Calls:** *blocker* holds the release; *blocker candidate* is the maintainer's call; *follow-up*
ships without it (umbrella #5128); *dropped* is intentional. No confirmed blockers.

| Gap | WPF | Avalonia | Call |
|---|---|---|---|
| Holding Shift at launch skips loading the last project | `ProjectManager.Initialize` checks `IsPressedInControl(Shift)` | `AvaloniaModifierKeyState` only updates on window key events, so Shift reads as up at startup | Blocker candidate, #5129 |
| Opening a `.gumx` from macOS Finder opens that project | n/a (Windows passes it as an argument) | `App` forwards Avalonia file activations to `ProjectOpenRequestRouter`. Covered by headless tests; needs a Mac run | Fixed pending Mac check, #5130 |
| Dropping a `.gumx`/`.gumj` on the window opens it | `MainWindow.xaml.cs` via `IProjectFileDropLogic` | `AppWideWindowGestures` on the same logic; the canvas and tree ignore a drop carrying a project file. Pinned by `AppWideWindowGesturesTests` | Fixed, #5128 |
| Mouse back/forward buttons step selection history | `MainWindow.OnPreviewMouseDown` | `AppWideWindowGestures`, tunneling so the tree and canvas can't swallow them. Pinned by `AppWideWindowGesturesTests` | Fixed, #5128 |
| Ctrl+C copies a message dialog's text | `DialogWindow.xaml.cs` | `DialogWindow.cs` copies the whole message, or the selection: the message is a `SelectableTextBlock`. Pinned by `DialogKeyboardTests` | Fixed, #5128 |
| Y/N (and Alt+Y/N) answer the delete dialog | `DeleteOptionsWindow.xaml.cs` | "_Yes"/"_No" access keys, plus `DialogViewModel.TryAnswerFromAccessKey` for the bare letter. Pinned by `DialogKeyboardTests` | Fixed, #5128 |
| Clear (X) button in the tree search box | `WpfElementTreeView` `HasClearButtonProperty` | (X) inside the box while it has text. Pinned by `ProjectSearchBoxTests` | Fixed, #5128 |
| Timeline time box applies while typing | `Timeline.xaml` | `TimelineView.cs` applies each keystroke; the time is view state, so no undo is recorded (as WPF). Pinned by `TimelineEndToEndTests` | Fixed, #5128 |
| Timeline scrubber value tooltip and tick marks | `Timeline.xaml` | Ticks every 0.1s, the time as a tooltip above the thumb while dragging, click-to-point. Pinned by `TimelineEndToEndTests` | Fixed, #5128 |
| Editor toolbar +/- buttons scale with UI font size | Resize with the base font | `EditorToolbar.UpdateButtonSizes`. Pinned by `UiFontSizeEndToEndTests` | Fixed, #5128 |
| Tree collapse-button icons scale with font size | `WpfElementTreeView.UpdateCollapseButtonSizes` | Icon and button height scale. Pinned by `UiFontSizeEndToEndTests` | Fixed, #5128 |
| Plugins dialog layout (tabs, monospace scan text, Copy Scan in the button row) | `PluginsDialogView.xaml` | Same tabs, unwrapped monospace scan, Copy Scan beside Close, fixed size across tab switches. Pinned by `PluginsDialogViewTests` | Fixed, #5128 |
| Color picker HSV/HSL entry | PixiEditor PortableColorPicker: HSV/HSL square, RGB/HSV/HSL slider tabs, hex box | Variables tab `CompactColorPicker`: SV square, hue bar, RGB sliders with 0-255 fields; hex box in the row. The Theming dialog uses Avalonia's `ColorPicker` | Dropped: every color is reachable by the square and exactly enterable as RGB or hex, and Gum stores R/G/B, so HSV/HSL numeric entry would be a second way to type the same thing |
| "Add" cursor while dragging a node or file over the canvas | `GiveFeedback` sets `AddCursor.cur` | The OS copy cursor (arrow with +), since the canvas answers `DragDropEffects.Copy` | Dropped: Avalonia 11 has no `GiveFeedback` or other hook to set the cursor during a platform drag |
| Import from .gumx dialog scrolls only its tree | `ScrollContent=False` | Opens at 600x560, shrinks on a short screen, and only the tree scrolls (`DialogWindow.SetPreferredHeight`). Pinned by `PluginDialogTests` | Fixed, #5128 |
| Telemetry and crash reporting (AppCenter) | Shipped | None | Dropped (2026-09-14, see README open decisions) |
| WPF-only third-party plugins | Load | Don't load | Dropped (ADR-0018) |

## Checked, no gap

Main menu, tree and canvas right-click menus, hotkeys (shared `HotkeyManager`) and every
view-specific key handler, command-line options, Project Properties fields, the MEF plugin set,
every head DI contract, every WPF dialog view, the Variables grid and its displayers, the element
tree, the States tab (right-click selects the row; pinned by `StateTreeViewTests`), the canvas and
Texture Coordinates tab, the Animations panel, and the Output, File Watch, Performance, Errors,
Undo history, Alignment, Behaviors and Code panels. Context-menu shortcut labels all parse as
Avalonia key gestures.
