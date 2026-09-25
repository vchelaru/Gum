# Launches each head by itself on its scratch project and measures the same scripted actions in
# both: startup, element selections, the tree search, menus, the tree context menu, the Code tab
# (code generation), the delete dialog with its confirm and undo, and shutdown. Writes
# out\timings.md beside this script (or -OutRoot) and prints it. Nothing else may be in the
# foreground on the primary monitor while it runs: the heads are brought to the front and driven
# with the keyboard.
#
# No fixed pauses: after each action the script waits until the process's CPU rate falls back to
# the idle rate it measured after loading (both heads render their canvas continuously, so idle
# is not zero), sampled every 100 ms and held for 300 ms; "settle" is the time from the action to
# the start of that quiet window. Menus and dialogs are also timed to the moment they appear
# through UI Automation. Reported per action: settle (ms), CPU (ms), and for menus and dialogs
# the appear time (ms). A settle past -MaxSettleMs is reported as a timeout.
#
# Both apps must be built in the configuration named by -Configuration (Release by default).
param(
    [string]$WpfGumx = (Join-Path $env:TEMP "GumParity\wpf\GameUiSamplesGumProject.gumx"),
    [string]$AvaloniaGumx = (Join-Path $env:TEMP "GumParity\avalonia\GameUiSamplesGumProject.gumx"),
    [string[]]$Heads = @("avalonia", "wpf"),
    [string]$Configuration = "Release",
    [string]$OutRoot = (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "out"),
    [string[]]$Elements = @("MainMenu", "StardewInventoryScreen", "HyTaleInventoryScreen", "GameTitleScreen", "TestScreen",
        "DialogBox", "ComboBox", "Slider", "TreeView", "ListBox", "Keyboard", "SettingsView"),
    [string]$DeleteInstance = "NineSliceInstance",
    [string]$DeleteInstanceOwner = "DialogBox",
    [int]$MaxSettleMs = 10000,
    [int]$IdleMs = 3000,
    [int]$LoadTimeoutSec = 90
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName Microsoft.VisualBasic
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
public static class NativeTimings {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out RECT rect, int size);
    public delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc proc, IntPtr lParam);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    public static RECT FrameRect(IntPtr h) {
        RECT r; if (DwmGetWindowAttribute(h, 9, out r, Marshal.SizeOf(typeof(RECT))) != 0) GetWindowRect(h, out r); return r;
    }
    public static List<IntPtr> VisibleWindows(uint pid) {
        var list = new List<IntPtr>();
        EnumWindows((h, l) => { uint p; GetWindowThreadProcessId(h, out p); if (p == pid && IsWindowVisible(h)) list.Add(h); return true; }, IntPtr.Zero);
        return list;
    }
}
"@
[void][NativeTimings]::SetProcessDPIAware()
New-Item -ItemType Directory -Force $OutRoot | Out-Null

$repo = Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "..\..")
function ExeFor($head) {
    if ($head -eq "wpf") { Join-Path $repo "Gum\bin\$Configuration\Gum.exe" }
    else { Join-Path $repo "Tool\Gum.Avalonia\bin\$Configuration\net10.0\Gum.exe" }
}
function Keys($text) { [System.Windows.Forms.SendKeys]::SendWait($text) }
function Click($x, $y, $right = $false) {
    [void][NativeTimings]::SetCursorPos([int]$x, [int]$y); Start-Sleep -Milliseconds 40
    if ($right) { [NativeTimings]::mouse_event(8, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 30; [NativeTimings]::mouse_event(16, 0, 0, 0, [UIntPtr]::Zero) }
    else { [NativeTimings]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 30; [NativeTimings]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero) }
}
function Foreground($proc) {
    [void][NativeTimings]::ShowWindow($proc.MainWindowHandle, 3)
    [void][NativeTimings]::SetForegroundWindow($proc.MainWindowHandle); Start-Sleep -Milliseconds 800
    if ([NativeTimings]::GetForegroundWindow() -ne $proc.MainWindowHandle) { [Microsoft.VisualBasic.Interaction]::AppActivate($proc.Id); Start-Sleep -Milliseconds 800 }
    if ([NativeTimings]::GetForegroundWindow() -ne $proc.MainWindowHandle) { throw "Could not bring $($proc.ProcessName) to the front" }
}
function Mb($bytes) { [math]::Round($bytes / 1MB, 0) }
function CpuMs($proc) { $proc.Refresh(); $proc.TotalProcessorTime.TotalMilliseconds }

# UI Automation lookups: the main window first, then the process's other top-level windows (menus
# and dialogs are separate windows).
function FindByName($proc, $name, $types) {
    $nameCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $name)
    foreach ($type in $types) {
        $cond = if ($type -eq $null) { $nameCond } else { New-Object System.Windows.Automation.AndCondition(@($nameCond, (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, $type)))) }
        try {
            $root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)
            $found = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
            if ($found -ne $null) { return $found }
            $byPid = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, [int]$proc.Id)
            foreach ($top in [System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children, $byPid)) {
                $found = $top.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
                if ($found -ne $null) { return $found }
            }
        } catch { }
    }
    return $null
}
function Center($el) { $r = $el.Current.BoundingRectangle; @([int]($r.X + $r.Width / 2), [int]($r.Y + $r.Height / 2)) }
function OtherWindows($proc) { [NativeTimings]::VisibleWindows($proc.Id) | Where-Object { $_ -ne $proc.MainWindowHandle -and ([NativeTimings]::FrameRect($_).Right - [NativeTimings]::FrameRect($_).Left) -gt 50 } }

# Polls a condition every 20 ms; returns the ms until it held, or -1 on timeout.
function WaitFor([scriptblock]$condition, $timeoutMs) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    while ($sw.ElapsedMilliseconds -lt $timeoutMs) {
        if (& $condition) { return [int]$sw.ElapsedMilliseconds }
        Start-Sleep -Milliseconds 20
    }
    return -1
}

# The idle CPU rate (cores) over $ms of nothing.
function MeasureIdleRate($proc, $ms) {
    $c0 = CpuMs $proc; $sw = [System.Diagnostics.Stopwatch]::StartNew()
    Start-Sleep -Milliseconds $ms
    $c1 = CpuMs $proc
    return ($c1 - $c0) / $sw.ElapsedMilliseconds
}

# Waits until the CPU rate has been at the idle rate (plus a margin of 0.15 core) for three
# consecutive 100 ms samples. Returns the settle time (start of the quiet window), the CPU spent
# above the idle rate over the wait, and whether it timed out.
function WaitSettled($proc, $idleRate) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $start = CpuMs $proc; $lastCpu = $start; $lastT = 0; $quiet = 0
    while ($sw.ElapsedMilliseconds -lt $MaxSettleMs) {
        Start-Sleep -Milliseconds 100
        $cpu = CpuMs $proc; $t = [int]$sw.ElapsedMilliseconds
        $rate = ($cpu - $lastCpu) / [math]::Max(1, $t - $lastT)
        $lastCpu = $cpu; $lastT = $t
        if ($rate -le $idleRate + 0.15) { $quiet++ } else { $quiet = 0 }
        if ($quiet -ge 3) { return @{ Ms = [math]::Max(0, $t - 300); Cpu = [math]::Max(0, [int](($cpu - $start) - $idleRate * $t)); TimedOut = $false } }
    }
    return @{ Ms = [int]$sw.ElapsedMilliseconds; Cpu = [math]::Max(0, [int](($lastCpu - $start) - $idleRate * $lastT)); TimedOut = $true }
}

$script:Results = New-Object System.Collections.Generic.List[object]
function Record($head, $action, $settle, $appearMs, $note) {
    $settleText = if ($settle -eq $null) { "" } elseif ($settle.TimedOut) { "timeout ($($settle.Ms))" } else { "$($settle.Ms)" }
    $cpuText = if ($settle -eq $null) { "" } else { "$($settle.Cpu)" }
    $appearText = if ($appearMs -eq $null) { "" } elseif ($appearMs -lt 0) { "not seen" } else { "$appearMs" }
    $script:Results.Add([pscustomobject]@{ Head = $head; Action = $action; AppearMs = $appearText; SettleMs = $settleText; CpuMs = $cpuText; Note = $note })
    Write-Host ("  {0,-42} appear {1,-8} settle {2,-14} cpu {3,-6} {4}" -f $action, $appearText, $settleText, $cpuText, $note)
}
function Act($head, $proc, $idleRate, $action, [scriptblock]$do) {
    & $do
    $settle = WaitSettled $proc $idleRate
    Record $head $action $settle $null ""
    return $settle
}
function SelectViaSearch($name) {
    # The first result is pre-selected. Escape afterwards moves keyboard focus into the tree.
    Keys "^f"; Start-Sleep -Milliseconds 150; Keys "^a"; Keys $name; Start-Sleep -Milliseconds 400; Keys "{ENTER}"; Start-Sleep -Milliseconds 150; Keys "{ESC}"
}
function ClickTab($proc, $name) {
    $tab = FindByName $proc $name @([System.Windows.Automation.ControlType]::TabItem, [System.Windows.Automation.ControlType]::Text)
    if ($tab -eq $null) { return $false }
    $c = Center $tab; Click $c[0] $c[1]
    return $true
}

$summary = New-Object System.Collections.Generic.List[object]
foreach ($head in $Heads) {
    $exe = ExeFor $head
    $gumx = if ($head -eq "wpf") { $WpfGumx } else { $AvaloniaGumx }
    if (-not (Test-Path $exe)) { throw "$exe is missing; build $Configuration first" }
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($gumx)
    Write-Host "== $head ($exe)"

    # Startup.
    $clock = [System.Diagnostics.Stopwatch]::StartNew()
    # The Avalonia head keeps this run's settings apart from the user's (the WPF head has no such flag).
    $args = if ($head -eq "wpf") { @("`"$gumx`"") } else { @("`"$gumx`"", "--user-data", "`"$(Join-Path (Split-Path $gumx) 'UserData')`"") }
    $proc = Start-Process -FilePath $exe -ArgumentList $args -PassThru
    while ($proc.MainWindowHandle -eq 0 -and $clock.Elapsed.TotalSeconds -lt $LoadTimeoutSec) { Start-Sleep -Milliseconds 20; $proc.Refresh() }
    $windowMs = [int]$clock.ElapsedMilliseconds
    while ($proc.MainWindowTitle -notlike "*$projectName*" -and $clock.Elapsed.TotalSeconds -lt $LoadTimeoutSec) { Start-Sleep -Milliseconds 20; $proc.Refresh() }
    $loadedMs = [int]$clock.ElapsedMilliseconds
    $startupCpu = [int](CpuMs $proc)
    Record $head "startup: window" $null $windowMs ""
    Record $head "startup: project loaded (title)" $null $loadedMs "CPU so far $startupCpu ms"
    # The idle rate is not known yet, so this settle uses one core as the bar (both heads render
    # their canvas continuously).
    $loadSettle = WaitSettled $proc 1.0
    Record $head "startup: settled after load" $loadSettle $null "against a 1-core bar"

    Start-Sleep -Milliseconds $IdleMs
    $idleRate = MeasureIdleRate $proc 2000
    $proc.Refresh()
    $idleWorkingSet = $proc.WorkingSet64
    Record $head "idle" $null $null ("CPU rate {0:P0} of a core, working set {1} MB, private {2} MB" -f $idleRate, (Mb $idleWorkingSet), (Mb $proc.PrivateMemorySize64))

    Foreground $proc
    Start-Sleep -Milliseconds 800
    $cpuAtStart = CpuMs $proc
    $actions = [System.Diagnostics.Stopwatch]::StartNew()

    # Element selections through the tree search.
    $selectionSettles = @()
    foreach ($element in $Elements) {
        $settle = Act $head $proc $idleRate "select $element" { SelectViaSearch $element }
        if (-not $settle.TimedOut) { $selectionSettles += $settle.Ms }
    }

    # The search box with a term that matches many rows, then cleared.
    [void](Act $head $proc $idleRate "search: type 'Button'" { Keys "^f"; Start-Sleep -Milliseconds 150; Keys "^a"; Keys "Button" })
    [void](Act $head $proc $idleRate "search: clear (Escape)" { Keys "{ESC}" })

    # Menus: time to appear, then settle.
    foreach ($menu in @(@("File", "Save All"), @("Edit", "Undo"))) {
        $item = FindByName $proc $menu[0] @([System.Windows.Automation.ControlType]::MenuItem)
        if ($item -eq $null) { Record $head "$($menu[0]) menu" $null -1 "menu header not found"; continue }
        $c = Center $item; Click $c[0] $c[1]
        $appear = WaitFor { (FindByName $proc $menu[1] @([System.Windows.Automation.ControlType]::MenuItem)) -ne $null } 5000
        $settle = WaitSettled $proc $idleRate
        Record $head "$($menu[0]) menu: open" $settle $appear ""
        Keys "{ESC}"; Keys "{ESC}"; Start-Sleep -Milliseconds 300
    }

    # Tree context menu on the selected element.
    SelectViaSearch $DeleteInstanceOwner
    [void](WaitSettled $proc $idleRate)
    $node = FindByName $proc $DeleteInstanceOwner @([System.Windows.Automation.ControlType]::Text)
    if ($node -ne $null -and $head -eq "avalonia") { $c = Center $node; Click $c[0] $c[1] $true } else { Keys "+{F10}" }
    $appear = WaitFor { (FindByName $proc "Copy Full Path" @([System.Windows.Automation.ControlType]::MenuItem)) -ne $null } 5000
    $settle = WaitSettled $proc $idleRate
    Record $head "tree context menu: open" $settle $appear ""
    Keys "{ESC}"; Keys "{ESC}"; Start-Sleep -Milliseconds 300

    # The Code tab: code generation for the selected element; then back to Performance.
    if (ClickTab $proc "Code") {
        $settle = WaitSettled $proc $idleRate
        Record $head "Code tab: select (code generation)" $settle $null ""
        SelectViaSearch "ComboBox"
        $settle = WaitSettled $proc $idleRate
        Record $head "select ComboBox with Code tab in front" $settle $null ""
        if (ClickTab $proc "Performance") { [void](WaitSettled $proc $idleRate) }
    } else { Record $head "Code tab" $null -1 "tab header not found through UI Automation" }

    # Delete dialog on an instance: appear, confirm (delete + save), undo.
    SelectViaSearch $DeleteInstance
    [void](WaitSettled $proc $idleRate)
    Keys "{DELETE}"
    $appear = WaitFor { @(OtherWindows $proc).Count -gt 0 } 5000
    $settle = WaitSettled $proc $idleRate
    Record $head "delete dialog: open" $settle $appear ""
    if ($appear -ge 0) {
        Keys "{ENTER}"
        $closed = WaitFor { @(OtherWindows $proc).Count -eq 0 } 5000
        $settle = WaitSettled $proc $idleRate
        Record $head "delete dialog: confirm (delete + save)" $settle $closed "closed after (ms)"
        Start-Sleep -Milliseconds 300
        [void](Act $head $proc $idleRate "undo the delete (Ctrl+Z)" { Keys "^z" })
        [void](Act $head $proc $idleRate "redo the delete (Ctrl+Y)" { Keys "^y" })
        [void](Act $head $proc $idleRate "undo again (Ctrl+Z)" { Keys "^z" })
    } else { Keys "{ESC}" }

    $actions.Stop()
    $proc.Refresh()
    $cpuAtEnd = CpuMs $proc
    $totalCpu = [int]($cpuAtEnd - $cpuAtStart)
    $afterWorkingSet = $proc.WorkingSet64
    Record $head "all actions" $null $null ("{0} ms wall, {1} ms CPU, working set {2} -> {3} MB" -f [int]$actions.ElapsedMilliseconds, $totalCpu, (Mb $idleWorkingSet), (Mb $afterWorkingSet))

    $shutdown = [System.Diagnostics.Stopwatch]::StartNew()
    [void]$proc.CloseMainWindow()
    if (-not $proc.WaitForExit(20000)) { Write-Host "  did not exit; killed"; $proc.Kill() }
    Record $head "shutdown" $null ([int]$shutdown.ElapsedMilliseconds) ""

    $selectionMedian = if ($selectionSettles.Count -gt 0) { ($selectionSettles | Sort-Object)[[int]($selectionSettles.Count / 2)] } else { -1 }
    $summary.Add([pscustomobject]@{
        Head = $head; WindowMs = $windowMs; LoadedMs = $loadedMs; StartupCpuMs = $startupCpu
        IdleRate = ("{0:P0}" -f $idleRate); IdleWorkingSetMb = Mb $idleWorkingSet
        SelectionMedianMs = $selectionMedian; ActionsWallMs = [int]$actions.ElapsedMilliseconds; ActionsCpuMs = $totalCpu
        AfterWorkingSetMb = Mb $afterWorkingSet; ShutdownMs = [int]$shutdown.ElapsedMilliseconds
    })
    Start-Sleep -Milliseconds 1500
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# Timings ($Configuration, $(Get-Date -Format 'yyyy-MM-dd HH:mm'))")
$lines.Add("")
$lines.Add("Settle = ms from the action until the process's CPU rate returned to its idle rate for 300 ms (100 ms resolution). CPU = ms of processor time above the idle rate during that wait. Appear = ms until a menu or dialog was found through UI Automation.")
$lines.Add("")
$lines.Add("## Summary")
$lines.Add("")
$lines.Add("| Head | Window (ms) | Project loaded (ms) | Startup CPU (ms) | Idle CPU rate | Idle working set (MB) | Median selection settle (ms) | All actions wall (ms) | All actions CPU (ms) | Working set after (MB) | Shutdown (ms) |")
$lines.Add("|---|---|---|---|---|---|---|---|---|---|---|")
foreach ($s in $summary) {
    $lines.Add("| $($s.Head) | $($s.WindowMs) | $($s.LoadedMs) | $($s.StartupCpuMs) | $($s.IdleRate) | $($s.IdleWorkingSetMb) | $($s.SelectionMedianMs) | $($s.ActionsWallMs) | $($s.ActionsCpuMs) | $($s.AfterWorkingSetMb) | $($s.ShutdownMs) |")
}
$lines.Add("")
$lines.Add("## Per action")
$lines.Add("")
$lines.Add("| Head | Action | Appear (ms) | Settle (ms) | CPU (ms) | Note |")
$lines.Add("|---|---|---|---|---|---|")
foreach ($r in $script:Results) { $lines.Add("| $($r.Head) | $($r.Action) | $($r.AppearMs) | $($r.SettleMs) | $($r.CpuMs) | $($r.Note) |") }
$lines | Set-Content (Join-Path $OutRoot "timings.md")
$lines | ForEach-Object { Write-Host $_ }
