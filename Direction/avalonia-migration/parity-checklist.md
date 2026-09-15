# Avalonia head parity checklist

> The manual half of phase 100: interaction the automated layers cannot drive. Run it before phase
> 110 (preview channel) and again before phase 120 (cutover), on each OS, against a copy of a real
> project. Mark each cell `pass`, `fail (#issue)`, or `n/a`, and date the column header. The
> automated layers are in `phase-100-testing-and-parity.md`.
>
> **Status 2026-09-14:** no cell below has been filled in by hand on any OS, and the Avalonia
> packages became the release anyway (#4699). What exists is automated evidence: on Windows the
> `Tools/ParityShots/` driver and `timings.ps1` walk both heads through open project, tree
> selection, search, the File/Edit/context menus, the delete dialog and undo/redo, and the
> screenshot pairs match; on Linux (WSL, WSLg) the head was run unattended and the quirks below
> were fixed; macOS has never been run. Filling this table in, on a real Mac in particular, is the
> first open item of phase 120.

| Area | What to check | Windows | macOS | Linux |
|---|---|---|---|---|
| Open project | File > Open, recent files, command-line path, legacy-version prompt | | | |
| Tree navigation | expand/collapse, arrow keys, Home/End/PageUp/PageDown, collapse buttons | | | |
| Select / multi-select | click, Ctrl+click, Shift+click range, selection follows the canvas | | | |
| Canvas edit | move, resize, rotate, marquee, nudge with arrows, snap to grid, rulers | | | |
| Add instance by drag | tree node, search result and Standards chip onto tree and canvas; files from the OS | | | |
| Variable editors | every editor type sets and reads back; filter box; Ctrl+E | | | |
| States and categories | add, rename, delete, reorder, copy/paste, variables-per-state markers | | | |
| Animations and timeline | create, keyframes, preview, scrub | | | |
| Behaviors | add, reference, required-state markers | | | |
| Undo / redo | across every row above | | | |
| Copy / paste | instances, elements, states, categories | | | |
| Rename and cascade | element, instance, state, category, folder | | | |
| Delete with references | delete dialog options, reference listing | | | |
| Texture coordinates | region select, drag edges, zoom, scroll, snapping | | | |
| Import from gumx | pick project, choose elements, import | | | |
| Import HTML | Content > Import > HTML: options dialog with Browse, converter progress in the Output tab, result dialog with the log | | | |
| Code output tab | settings grid, generated code, save | | | |
| Gum Forms tab | add Forms, theme import | | | |
| Errors tab | errors listed, click navigates, "!" tree icons | | | |
| Output tab | messages, clear | | | |
| Hotkeys | app-wide and per-panel, including tree and states hotkeys | | | |
| Theme switch | light, dark, system; accent color; icons re-tint | | | |
| App scale | Ctrl+= / Ctrl+- outside the canvas; canvas zoom inside it | | | |
| Save | XML and JSON projects, element save, save-all | | | |
| Codegen | generate for a code-generation project | | | |
| File watch | external edit reloads; own saves ignored | | | |
| Plugins dialog | lists plugins, reports WPF-only plugins as not hostable | | | |
| Project properties | dialog opens, settings persist | | | |
| Per-OS input | right-click / Ctrl+click on macOS, trackpad scroll and pinch, modifier keys | | | |

## Per-OS quirks found

Record anything that behaves differently on one OS here, with the issue number.

- **Linux, 2026-09-11 (WSL Ubuntu, WSLg):** the head crashed at startup on an account with no
  `~/.config` (`GetFolderPath` returned an empty path; fixed, the folder is created on first use).
  Building `GumFormsPlugin` with an SDK that is not on `PATH` failed in its nested build (fixed,
  `$(DOTNET_HOST_PATH)`). The head then failed to start because it carried SkiaSharp's 2.88 Linux
  native beside the 3.119 managed assembly (fixed: `SkiaSharp.NativeAssets.Linux` pinned to the
  managed version in `Gum.ImageDiff`). External file changes were never reloaded because the
  watcher checked the lowercased path (fixed: `FilePath.FullPath`). Tests that assumed Windows
  (drive-letter paths, an installed Arial, `BmFont` being supported) were made OS-neutral. With the
  Output tab echoed to stderr (`GUM_ECHO_OUTPUT=1`), the run showed the orphan code file plugin
  dying on an unreadable folder under the code root (fixed: skipped) and Arial failing to generate
  (fixed: the platform's substitute face is used, with an Output line). No issue numbers: fixed on
  the branch before any release.

## Timings (Windows, Release, 2026-09-14)

Measured with `Tools/ParityShots/timings.ps1` on the GameUiSamples project: the same scripted
actions in both heads, each timed until the process's CPU rate returned to its idle rate (100 ms
resolution), menus and dialogs also timed to when UI Automation finds them. Both heads render their
canvas continuously and sit at about one core when idle (Avalonia 98%, WPF 106%), which is a
finding in itself: neither renders only when something changed.

| | Avalonia | WPF |
|---|---|---|
| Launch to window | 1.2 s | 11.6 s (the window shows once the project is loaded) |
| Launch to project loaded | 3.2 s (3.1 s CPU) | 11.6 s (13.2 s CPU) |
| Idle working set | 309 MB | 335 MB |
| Select a screen or component, median settle | 28 ms | 357 ms |
| Select StardewInventoryScreen / HyTaleInventoryScreen (heaviest) | 23 ms / 20 ms | 1.9 s / 1.8 s |
| Select DialogBox | 0.9 s | 1.0 s |
| Search box: type "Button" | 15 ms | 131 ms |
| File / Edit menu appear | 55 / 6 ms | 51 / 4 ms |
| Tree context menu appear | 83 ms | 53 ms |
| Delete dialog appear | 34 ms | 119 ms |
| Delete confirm (delete + save) settle | 27 ms | 0.9 s |
| Undo the delete / redo / undo again | 0.8 s / 0.25 s / 0.13 s | 0.46 s / 18 ms / 13 ms |
| Whole scripted run, wall / CPU | 28.0 s / 22.1 s | 46.7 s / 46.3 s |
| Working set after the run | 539 MB | 478 MB |
| Shutdown | 0.7 s | 0.6 s |

Open points from the numbers: the idle render loop in both heads; the Avalonia head's undo of an
instance delete (0.8 s against 0.46 s) and its larger working-set growth over the run (+230 MB
against +143 MB), neither investigated yet.
