# MacPass: replayable checks of the packaged macOS app

The headless end-to-end suite (`Tests/Gum.Avalonia.Tests/EndToEnd/`) covers every inventory item's
logic on all three OSes. It cannot see what only a real Mac shows: the native menu bar, Cmd mappings,
native open and save panels, real drag and drop, window persistence, quitting. `macpass.py` covers
those by launching the packaged `Gum.app`, driving it with real input, and checking outcomes without
looking at pixels. A green run costs a reader nothing; a screenshot is taken only for a failure.

```bash
Tools/MacPass/build-app.sh                  # once per change under test, about 10 minutes
python3 Tools/MacPass/macpass.py            # all scenarios, about 2 minutes
python3 Tools/MacPass/macpass.py hotkeys    # scenarios whose name contains a word
python3 Tools/MacPass/macpass.py --list
```

Read `publish/macpass/report.txt`. `FAIL` lines carry the failed checks and the path of a screenshot of
the window; `KNOWN` is a scenario that reproduces an open issue (it does not fail the run); `FIXED?`
means a known bug no longer reproduces, so remove its `known_bug` argument.

## Prerequisites

- Accessibility permission for the app that runs the script (System Settings > Privacy & Security >
  Accessibility), for the synthetic input and the menu bar queries. Screen Recording permission for
  the failure screenshots.
- Xcode command line tools (`swiftc` builds `gumdrv.swift` into `~/Library/Caches/gum-macpass`).
- Nobody using the machine: the scripts move the real mouse and keyboard. It cannot run in CI, where
  a macOS runner has no Accessibility permission.

## What a scenario checks, cheapest first

1. **Disk.** Gum writes every edit to the project folder, so a new, renamed or deleted `.gucx`, an
   exported PNG, or an instance count in the element XML proves the gesture worked.
2. **The menu bar.** macOS exposes it fully through Accessibility: `Ctx.menu_items()` lists every item's
   path, enabled state and key equivalent, and `Gum.menu(...)` presses an item by its title path, with
   no coordinates.
3. **Window titles and frames** (`Gum.title()`, `Gum.windows()`).
4. **The clipboard** (`pbpaste`).

Window content (tree rows, buttons) is not in the Accessibility tree (#5969), so clicks use points
relative to the main window in the default 1280x720 layout (`TREE_ROW_SELECTED`, `CHIP_CIRCLE`,
`CANVAS_DROP`). A moved control means updating one constant.

## Gotchas

- `--select Element` selects after load but leaves keyboard focus nowhere, so hotkeys do nothing until `Gum.focus_tree()` (Cmd+F, Escape). Clicking the tree instead can clear the selection.
- A native panel's path field takes a typed path, then needs one Return to apply it and another to
  press Open or Save. Open panels: Cmd+Shift+G first.
- The drag in `gumdrv` is deliberately slow with a small wiggle. A fast one never starts the
  platform's drag session.
- Modifier keys must be real key events. A key event that only carries a Command flag leaves the
  system's modifier state stuck, every later mouse event then reports Command, and a Command drag is a
  move that the canvas's copy-only drop target rejects (the drag looks fine and the drop is cancelled,
  which read as a flaky drag for hours). `gumdrv key ... cmd` presses and releases the modifier;
  `gumdrv flags` shows the state and `gumdrv clearflags` releases everything; `macpass.py` clears it
  before each scenario and fails a scenario that leaves Command down. `gumdrv listen N` prints the mouse
  events the session sees, with the process that posted each.
- Type with `gumdrv type` (real key codes); the unicode fallback is dropped by some focused text boxes.
- One launch per scenario, on a fresh copy of the sample project and a fresh `--user-data` folder, so
  the run never touches `Samples/` or the real settings. Use `Ctx.launch()` again in a scenario to
  test what persists across a restart.
- A scenario for a bug that is not fixed yet passes `known_bug=<issue>` to `@scenario`.

## Adding a scenario

Write the check as an outcome table first (gesture, state, expected result), pick the cheapest
oracle above, and keep one launch to one behavior. Anything that is the same on every OS belongs in the
headless harness (`Tests/Gum.Avalonia.Tests/Harness/README.md`), not here.
