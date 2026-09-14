# Comparing the WPF and Avalonia Gum tools

The scripts here drive the two running tool heads through the same states and capture matching
screenshots, so the heads can be compared side by side, and they carry the recipes for building,
running and testing both heads on Windows and Linux. Nothing here is part of the build; the
scripts read the running apps through UI Automation and Win32, so they are Windows-only. Output
goes to `out\` beside the scripts (ignored by git).

## 1. Build

Close both apps first: they lock their output files, and the plugin copy steps fail with "file in use".

```powershell
cd <repo>
dotnet build GumFull.sln
```

One solution build covers both heads: the WPF tool (`Gum\bin\Debug\Gum.exe`), the Avalonia head
(`Tool\Gum.Avalonia\bin\Debug\net10.0\`), and the neutral plugins, which copy themselves into each
head's `Plugins` folder. For timing or performance comparisons build and run `-c Release`, never Debug.

## 2. Run

Always open a copy of a sample project, never `Samples\` itself (opening re-saves the project).
`new-scratch-projects.ps1` copies `Samples\GameUiSamples\Content\GumProject` to
`%TEMP%\GumParity\wpf` and `%TEMP%\GumParity\avalonia` and prints the two `.gumx` paths.

```powershell
# WPF
Gum\bin\Debug\Gum.exe "<path>\wpf\GameUiSamplesGumProject.gumx"

# Avalonia
dotnet run --project Tool\Gum.Avalonia --no-build -- "<path>\avalonia\GameUiSamplesGumProject.gumx"
```

Avalonia head options: no path starts empty; `--select "Controls/DialogBox#NineSliceInstance"`
selects an element (and instance) once loaded; `--theme light|dark` shows a variant without saving
it; `--exit-after 8 --screenshot out.png` runs unattended and writes a screenshot of the window.
`GUM_ECHO_OUTPUT=1` in the environment also writes every Output tab line to stderr, which is how an
unattended run's log shows what the tool saw (plugin failures, font generation, and so on).

## 3. Automated tests

```powershell
dotnet test Tests\Gum.Presentation.Tests          # shared logic, both heads (runs on any OS)
dotnet test Tests\Gum.Avalonia.Tests              # Avalonia head, headless; includes the real-head startup test
dotnet test Tests\Gum.ProjectServices.Tests       # project load/save/codegen, save byte-parity corpus
dotnet test Tool\Tests\GumToolUnitTests\GumToolUnitTests.csproj -p:SolutionDir='<repo>\'   # WPF head
```

Add `-p:BuildProjectReferences=false` to the WPF test command while the WPF tool is running (the
plugin copy step is skipped, so the tests run against the already-built output). Building any test
project that references the neutral plugins fails while the Avalonia head is running, for the same
reason: close it first.

## 4. Screenshot comparison

Both apps must be running, maximized on the primary monitor, and left alone while a script runs
(the scripts take over the mouse and keyboard for about 90 seconds).

```powershell
# Drives both heads through the same states and saves matching shots to out\wpf and out\avalonia
.\capture.ps1                 # -Heads avalonia,wpf  -Element DialogBox  -Instance NineSliceInstance  -OutRoot <folder>

# Side-by-side pairs (WPF left, Avalonia right) into out\pairs
python pair.py                # or: python pair.py file-menu variables delete-dialog
```

Shot names: `main`, `file-menu`, `edit-menu`, `variables`, `tree-context`, `add-instance`,
`width-units`, `combo-open`, `delete-dialog`, `project-properties`, `add-component`,
`tree-collapsed`, `main-after`. Read pairs one at a time; each is a full-size image.

One-off tools (each takes `-ProcessName Gum` for WPF or `Gum.Avalonia` for the Avalonia head):

```powershell
.\menu-capture.ps1 -ProcessName Gum -Path Content,Import,"HTML…" -Out out\x.png            # opens a menu path, captures the dialog that appears
.\select-and-capture.ps1 -ProcessName Gum.Avalonia -Element TestScreen -X 900 -Y 30 -W 1000 -H 800 -Out out\y.png   # selects via the tree search, captures a region
.\click-capture.ps1 -ProcessName Gum -ClickX 1090 -ClickY 858 -X 895 -Y 848 -W 900 -H 300 -Out out\z.png           # clicks a point (a tab header), captures a region
.\probe.ps1 -ProcessName Gum.Avalonia -Depth 4 [-Class X] [-Id X] [-Raw]                     # dumps the UI Automation tree
.\dialog-first-click-probe.ps1 -Path Edit,Add,Screen [-Button Cancel] [-PreClickCaption]   # opens a dialog, reports the foreground window and whether one click on a button closes it
.\menu-open-probe.ps1 -Menu Edit                                                             # clicks a top-level menu and screenshots the drop-down
```

When a menu path is passed from a shell other than PowerShell, quote it as one argument per item
or call through `powershell -Command "& .\menu-capture.ps1 -Path Edit,Add,Screen"`; a single
`"Edit,Add,Screen"` string is not split.

## 5. How the driver works, and its gotchas

- Menus are found by name through UI Automation in both heads; the Avalonia tree exposes its rows
  as Text elements, the WPF tree does not, so WPF tree actions go through the keyboard.
- Elements are selected with the tree search: Ctrl+F, type the name, Enter (the first result is
  pre-selected), then Escape to move focus into the tree. Ctrl+E focuses the variable filter,
  Shift+F10 opens the WPF tree's context menu, Delete opens the delete dialog.
- Run `avalonia` first; the WPF head reuses coordinates found there.
- Never leave keyboard focus in an editable combo: typed search text once changed a Base Type in
  the scratch project. The driver clicks the Variables tab header to drop focus.
- Dialogs need about 1.5 s before capture; the first run right after launching an app can fail
  to bring the window to the front, so just run it again.
- The combo-dropdown shot is unreliable (the dropdown does not open from the driver's click).
- An OK-only dialog closes on Escape (as in WPF); the driver's trailing Escape relies on that.
- WPF's tab content is not in its UI Automation tree (menus are); the Avalonia head exposes everything.

## 6. Diagnosing a hang

If a head stops responding, dump its managed thread stacks before killing it:

```powershell
dotnet tool install -g dotnet-stack       # once
dotnet-stack report -p (Get-Process Gum.Avalonia).Id
```

The UI thread is the one whose stack ends in `Program.Main`. A nested `Dispatcher.MainLoop` in it
means a synchronous dialog is open; `IsHungAppWindow` (user32) on the main window says whether
Windows considers it hung.

## 7. Linux (WSL or a real machine)

The head and every headless suite run on Linux. On this repository's Windows machine the runs
were done in WSL Ubuntu with WSLg for the display:

```bash
# once: a user-local SDK and a clone on the Linux file system (never build under /mnt/c from Linux)
curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0 --install-dir ~/.dotnet
git clone --depth 1 file:///mnt/c/git/Gum -b <branch> ~/Gum
export PATH=$HOME/.dotnet:$PATH

# each time: copy the Windows working tree's uncommitted changes into the clone
bash /mnt/c/git/Gum/Tools/ParityShots/sync-to-linux-clone.sh ~/Gum /mnt/c/git/Gum

cd ~/Gum
dotnet build Tool/Gum.Avalonia/Gum.Avalonia.csproj -c Release
for p in Gum/ConvertToJsonPlugin/ConvertToJsonPlugin.csproj Gum/EventOutputPlugin/EventOutputPlugin.csproj \
         Gum/GumFormsPlugin/GumFormsPlugin.csproj Gum/ImportFromGumxPlugin/ImportFromGumxPlugin.csproj \
         Gum/PerformanceMeasurementPlugin/PerformanceMeasurementPlugin.csproj Gum/SvgPlugin/SkiaPlugin.csproj; do
  dotnet build $p -c Release
done
cp -r Tests/CodeGen_Skia_ByReference/Content/GumProject /tmp/gumrun
GUM_ECHO_OUTPUT=1 dotnet Tool/Gum.Avalonia/bin/Release/net10.0/Gum.Avalonia.dll /tmp/gumrun/*.gumx --exit-after 30 --screenshot /tmp/linux.png
dotnet test Tests/Gum.Presentation.Tests
dotnet test Tests/Gum.Avalonia.Tests
dotnet test Tests/Gum.ProjectServices.Tests
dotnet test Tests/Gum.Bundle.Tests
```

Linux needs a desktop session, a GL driver, and `libfontconfig1` (SkiaSharp's Linux native uses
it). Findings from these runs are recorded in `Direction/avalonia-migration/parity-checklist.md`
under "Per-OS quirks found" and in `coverage-matrix.md`.
