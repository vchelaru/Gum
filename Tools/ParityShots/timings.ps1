# Launches each head on its scratch project and measures, for the same scripted actions, how long
# startup takes and what the element selections cost. Writes out\timings.md beside this script (or
# -OutRoot) and prints it. Nothing else may be in the foreground on the primary monitor while it
# runs: the heads are brought to the front and driven with the keyboard.
#
# Measures per head:
#   window     launch -> main window handle exists
#   loaded     launch -> the window title carries the project name (project loaded, title plugin ran)
#   idle       working set and private bytes after -IdleMs of nothing
#   selection  wall clock, CPU time and working-set growth for selecting -Elements one after the
#              other through the tree search (Ctrl+F, name, Enter, Escape), -SettleMs apart
#   shutdown   CloseMainWindow -> process exit
#
# Both apps must be built in the configuration named by -Configuration (Release by default).
param(
    [string]$WpfGumx = (Join-Path $env:TEMP "GumParity\wpf\GameUiSamplesGumProject.gumx"),
    [string]$AvaloniaGumx = (Join-Path $env:TEMP "GumParity\avalonia\GameUiSamplesGumProject.gumx"),
    [string[]]$Heads = @("avalonia", "wpf"),
    [string]$Configuration = "Release",
    [string]$OutRoot = (Join-Path $PSScriptRoot "out"),
    [string[]]$Elements = @("ButtonStandard", "CheckBox", "ComboBox", "DialogBox", "ListBox", "Slider", "TextBox", "TreeView", "Keyboard", "Menu"),
    [int]$SettleMs = 1500,
    [int]$IdleMs = 4000,
    [int]$LoadTimeoutSec = 90
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName Microsoft.VisualBasic
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class NativeTimings {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out RECT rect, int size);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    public static RECT FrameRect(IntPtr h) {
        RECT r; if (DwmGetWindowAttribute(h, 9, out r, Marshal.SizeOf(typeof(RECT))) != 0) GetWindowRect(h, out r); return r;
    }
}
"@
[void][NativeTimings]::SetProcessDPIAware()
New-Item -ItemType Directory -Force $OutRoot | Out-Null

$repo = Resolve-Path (Join-Path $PSScriptRoot "..\..")
function ExeFor($head) {
    if ($head -eq "wpf") { Join-Path $repo "Gum\bin\$Configuration\Gum.exe" }
    else { Join-Path $repo "Tool\Gum.Avalonia\bin\$Configuration\net10.0\Gum.Avalonia.exe" }
}
function Keys($text) { [System.Windows.Forms.SendKeys]::SendWait($text); Start-Sleep -Milliseconds 150 }
function Foreground($proc) {
    [void][NativeTimings]::ShowWindow($proc.MainWindowHandle, 3)
    [void][NativeTimings]::SetForegroundWindow($proc.MainWindowHandle); Start-Sleep -Milliseconds 800
    if ([NativeTimings]::GetForegroundWindow() -ne $proc.MainWindowHandle) { [Microsoft.VisualBasic.Interaction]::AppActivate($proc.Id); Start-Sleep -Milliseconds 800 }
    if ([NativeTimings]::GetForegroundWindow() -ne $proc.MainWindowHandle) { throw "Could not bring $($proc.ProcessName) to the front" }
}
function CaptureWindow($hwnd, $path) {
    $r = [NativeTimings]::FrameRect($hwnd)
    $bmp = New-Object System.Drawing.Bitmap ($r.Right - $r.Left), ($r.Bottom - $r.Top)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($r.Left, $r.Top, 0, 0, $bmp.Size)
    $g.Dispose(); $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
}
function Mb($bytes) { [math]::Round($bytes / 1MB, 0) }

$rows = New-Object System.Collections.Generic.List[object]
foreach ($head in $Heads) {
    $exe = ExeFor $head
    $gumx = if ($head -eq "wpf") { $WpfGumx } else { $AvaloniaGumx }
    if (-not (Test-Path $exe)) { throw "$exe is missing; build $Configuration first" }
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($gumx)
    Write-Host "== $head ($exe)"

    $clock = [System.Diagnostics.Stopwatch]::StartNew()
    $proc = Start-Process -FilePath $exe -ArgumentList @("`"$gumx`"") -PassThru
    while ($proc.MainWindowHandle -eq 0 -and $clock.Elapsed.TotalSeconds -lt $LoadTimeoutSec) { Start-Sleep -Milliseconds 50; $proc.Refresh() }
    $windowMs = [int]$clock.ElapsedMilliseconds
    while ($proc.MainWindowTitle -notlike "*$projectName*" -and $clock.Elapsed.TotalSeconds -lt $LoadTimeoutSec) { Start-Sleep -Milliseconds 50; $proc.Refresh() }
    $loadedMs = [int]$clock.ElapsedMilliseconds
    $loaded = $proc.MainWindowTitle -like "*$projectName*"
    Write-Host "  window after $windowMs ms, title '$($proc.MainWindowTitle)' after $loadedMs ms (loaded: $loaded)"

    Start-Sleep -Milliseconds $IdleMs
    $proc.Refresh()
    $idleWorkingSet = $proc.WorkingSet64; $idlePrivate = $proc.PrivateMemorySize64

    Foreground $proc
    Start-Sleep -Milliseconds 1000
    $proc.Refresh()
    $cpuBefore = $proc.TotalProcessorTime
    $selection = [System.Diagnostics.Stopwatch]::StartNew()
    foreach ($element in $Elements) {
        Keys "^f"; Start-Sleep -Milliseconds 200; Keys "^a"; Keys $element; Start-Sleep -Milliseconds 700
        Keys "{ENTER}"; Start-Sleep -Milliseconds 300; Keys "{ESC}"
        Start-Sleep -Milliseconds $SettleMs
    }
    $selection.Stop()
    $proc.Refresh()
    $cpuSelection = $proc.TotalProcessorTime - $cpuBefore
    $afterWorkingSet = $proc.WorkingSet64
    CaptureWindow $proc.MainWindowHandle (Join-Path $OutRoot "$head-timings-final.png")
    $pauses = $Elements.Count * ($SettleMs + 1200 + 5 * 150)
    Write-Host "  $($Elements.Count) selections: $([int]$selection.ElapsedMilliseconds) ms wall (of which $pauses ms scripted pauses), CPU $([int]$cpuSelection.TotalMilliseconds) ms, working set $(Mb $idleWorkingSet) -> $(Mb $afterWorkingSet) MB"

    $shutdown = [System.Diagnostics.Stopwatch]::StartNew()
    [void]$proc.CloseMainWindow()
    if (-not $proc.WaitForExit(20000)) { Write-Host "  did not exit; killed"; $proc.Kill() }
    $shutdownMs = [int]$shutdown.ElapsedMilliseconds

    $rows.Add([pscustomobject]@{
        Head = $head; WindowMs = $windowMs; LoadedMs = $loadedMs; Loaded = $loaded
        IdleWorkingSetMb = Mb $idleWorkingSet; IdlePrivateMb = Mb $idlePrivate
        SelectionWallMs = [int]$selection.ElapsedMilliseconds; SelectionCpuMs = [int]$cpuSelection.TotalMilliseconds
        AfterWorkingSetMb = Mb $afterWorkingSet; ShutdownMs = $shutdownMs
    })
    Start-Sleep -Milliseconds 1500
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# Timings ($Configuration, $(Get-Date -Format 'yyyy-MM-dd HH:mm'))")
$lines.Add("")
$lines.Add("Selections: $($Elements -join ', ') ($SettleMs ms settle each).")
$lines.Add("")
$lines.Add("| Head | Window (ms) | Project loaded (ms) | Idle working set (MB) | Idle private (MB) | $($Elements.Count) selections wall (ms) | Selections CPU (ms) | Working set after (MB) | Shutdown (ms) |")
$lines.Add("|---|---|---|---|---|---|---|---|---|")
foreach ($r in $rows) {
    $loadedText = if ($r.Loaded) { "$($r.LoadedMs)" } else { "not seen in $LoadTimeoutSec s" }
    $lines.Add("| $($r.Head) | $($r.WindowMs) | $loadedText | $($r.IdleWorkingSetMb) | $($r.IdlePrivateMb) | $($r.SelectionWallMs) | $($r.SelectionCpuMs) | $($r.AfterWorkingSetMb) | $($r.ShutdownMs) |")
}
$lines | Set-Content (Join-Path $OutRoot "timings.md")
$lines | ForEach-Object { Write-Host $_ }
