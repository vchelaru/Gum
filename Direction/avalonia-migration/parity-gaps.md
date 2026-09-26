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
| Dropping a `.gumx`/`.gumj` on the window opens it | `MainWindow.xaml.cs` via `IProjectFileDropLogic` | Canvas drop rejects project files | Follow-up, #5128 |
| Mouse back/forward buttons step selection history | `MainWindow.OnPreviewMouseDown` | Buttons are mapped but nothing reads them; Alt+Left/Right works | Follow-up, #5128 |
| Ctrl+C copies a message dialog's text | `DialogWindow.xaml.cs` | `DialogWindow.cs` handles Escape only; message isn't selectable | Follow-up, #5128 |
| Y/N (and Alt+Y/N) answer the delete dialog | `DeleteOptionsWindow.xaml.cs` | Enter and Esc only | Follow-up, #5128 |
| Clear (X) button in the tree search box | `WpfElementTreeView` `HasClearButtonProperty` | Plain TextBox; Escape and Ctrl+Backspace clear it | Follow-up, #5128 |
| Timeline time box applies while typing | `Timeline.xaml` | `TimelineView.cs` applies on Enter or focus loss | Follow-up, #5128 |
| Timeline scrubber value tooltip and tick marks | `Timeline.xaml` | Plain slider | Follow-up, #5128 |
| Editor toolbar +/- buttons scale with UI font size | Resize with the base font | No `OnUiBaseFontSizeChanged` override; fixed width | Follow-up, #5128 |
| Tree collapse-button icons scale with font size | Not compared | Icon created at a fixed 16 (unverified at runtime) | Follow-up, #5128 |
| Plugins dialog layout (tabs, monospace scan text, Copy Scan in the button row) | `PluginsDialogView.xaml` | Everything stacked, paths wrap | Follow-up, #5128 |
| Color picker HSV/HSL entry and hex box | Third-party PortableColorPicker | `CompactColorPicker`: SV square, hue bar, RGB sliders, swatches. Open question whether anything used is missing | Follow-up, #5128 |
| "Add" cursor while dragging onto the tree | `AddCursor.cur` | Default cursor | Follow-up (cosmetic), #5128 |
| Import from .gumx dialog doesn't scroll as a whole | `ScrollContent=False` | Not set; probably harmless | Follow-up, #5128 |
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
