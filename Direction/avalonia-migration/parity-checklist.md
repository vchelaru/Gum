# Avalonia head parity checklist

> The manual half of phase 100: interaction the automated layers cannot drive. Run it before phase
> 110 (preview channel) and again before phase 120 (cutover), on each OS, against a copy of a real
> project. Mark each cell `pass`, `fail (#issue)`, or `n/a`, and date the column header. The
> automated layers are in `phase-100-testing-and-parity.md`.

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
