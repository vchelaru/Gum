
# Drives the running WPF tool and Avalonia head through the same UI states and captures matching
# screenshots into out\<head>\<name>.png beside this script (or -OutRoot). Both apps must be running, maximized on the
# primary monitor, and left alone while this runs.
param(
    [string[]]$Heads = @("avalonia", "wpf"),
    [string]$OutRoot = (Join-Path $PSScriptRoot "out"),
    [string]$Element = "DialogBox",
    [string]$Instance = "NineSliceInstance",
    [string]$FilterText = "Width Units",
    [string]$ComboFilter = "Base Type"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName Microsoft.VisualBasic
Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
public static class Native {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll")] public static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder sb, int max);
    [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder sb, int max);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out RECT rect, int size);
    public delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc proc, IntPtr lParam);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    public static List<IntPtr> VisibleWindows(uint pid) {
        var list = new List<IntPtr>();
        EnumWindows((h, l) => { uint p; GetWindowThreadProcessId(h, out p); if (p == pid && IsWindowVisible(h)) list.Add(h); return true; }, IntPtr.Zero);
        return list;
    }
    public static RECT FrameRect(IntPtr h) {
        RECT r; if (DwmGetWindowAttribute(h, 9, out r, Marshal.SizeOf(typeof(RECT))) != 0) GetWindowRect(h, out r); return r;
    }
    public static string ClassOf(IntPtr h) { var sb = new System.Text.StringBuilder(256); GetClassName(h, sb, 256); return sb.ToString(); }
    public static string TitleOf(IntPtr h) { var sb = new System.Text.StringBuilder(512); GetWindowText(h, sb, 512); return sb.ToString(); }
}
"@
[void][Native]::SetProcessDPIAware()

$script:Log = New-Object System.Collections.Generic.List[string]
function Note($text) { $script:Log.Add($text); Write-Host $text }
function Pause($ms) { Start-Sleep -Milliseconds $ms }
function Keys($text) { [System.Windows.Forms.SendKeys]::SendWait($text); Pause 150 }
function Click($x, $y, $right = $false) {
    [void][Native]::SetCursorPos([int]$x, [int]$y); Pause 60
    if ($right) { [Native]::mouse_event(8, 0, 0, 0, [UIntPtr]::Zero); Pause 40; [Native]::mouse_event(16, 0, 0, 0, [UIntPtr]::Zero) }
    else { [Native]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero); Pause 40; [Native]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero) }
    Pause 250
}
function Hover($x, $y) { [void][Native]::SetCursorPos([int]$x, [int]$y); Pause 700 }
function Capture($x, $y, $w, $h, $path) {
    $bmp = New-Object System.Drawing.Bitmap ([int]$w), ([int]$h)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen([int]$x, [int]$y, 0, 0, $bmp.Size)
    $g.Dispose()
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Note "  saved $path ($w x $h)"
}
function CaptureWindow($hwnd, $path) {
    $r = [Native]::FrameRect($hwnd)
    Capture $r.Left $r.Top ($r.Right - $r.Left) ($r.Bottom - $r.Top) $path
}
function Foreground($hwnd) {
    [void][Native]::ShowWindow($hwnd, 3)   # SW_MAXIMIZE keeps both heads on the same layout
    [void][Native]::SetForegroundWindow($hwnd); Pause 600
    if ([Native]::GetForegroundWindow() -ne $hwnd) { [Microsoft.VisualBasic.Interaction]::AppActivate($script:CurrentPid); Pause 600 }
    if ([Native]::GetForegroundWindow() -ne $hwnd) { throw "Could not bring the window to the front" }
}
function Uia($hwnd) { [System.Windows.Automation.AutomationElement]::FromHandle($hwnd) }
function FindByName($root, $name, $type) {
    $conds = @((New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $name)))
    if ($type) { $conds += New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, $type) }
    $cond = if ($conds.Count -eq 1) { $conds[0] } else { New-Object System.Windows.Automation.AndCondition($conds) }
    $found = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
    if ($found -ne $null) { return $found }
    # Popups (menus, context menus) are separate top-level windows of the same process.
    $byPid = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, [int]$script:CurrentPid)
    foreach ($top in [System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children, $byPid)) {
        $found = $top.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
        if ($found -ne $null) { return $found }
    }
    return $null
}
function Center($el) { $r = $el.Current.BoundingRectangle; @([int]($r.X + $r.Width / 2), [int]($r.Y + $r.Height / 2)) }
function ExpandMenu($root, $name) {
    $item = FindByName $root $name ([System.Windows.Automation.ControlType]::MenuItem)
    if ($item -eq $null) { throw "Menu '$name' not found" }
    $c = Center $item; Click $c[0] $c[1]; Pause 500
    return $item
}
function OtherWindows($processId, $main) { [Native]::VisibleWindows($processId) | Where-Object { $_ -ne $main -and [Native]::FrameRect($_).Right - [Native]::FrameRect($_).Left -gt 50 } }
function CaptureDialog($processId, $main, $path, $what) {
    $others = @(OtherWindows $processId $main)
    if ($others.Count -eq 0) { Note "  MISSING: no $what window appeared"; return $false }
    $dialog = $others | Sort-Object { $r = [Native]::FrameRect($_); ($r.Right - $r.Left) * ($r.Bottom - $r.Top) } -Descending | Select-Object -First 1
    Note "  $what window: '$([Native]::TitleOf($dialog))' class $([Native]::ClassOf($dialog))"
    CaptureWindow $dialog $path
    return $true
}
function SelectViaSearch($name) {
    # The first result is pre-selected. Escape afterwards moves keyboard focus into the tree.
    Keys "^f"; Pause 300; Keys "^a"; Keys $name; Pause 900; Keys "{ENTER}"; Pause 400; Keys "{ESC}"; Pause 1000
}
function CloseMenus() { Keys "{ESC}"; Keys "{ESC}"; Pause 300 }

$script:ComboPoint = $null
foreach ($head in $Heads) {
    $processName = if ($head -eq "wpf") { "Gum" } else { "Gum.Avalonia" }
    $proc = Get-Process -Name $processName -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($proc -eq $null) { Note "$head : $processName is not running, skipped"; continue }
    $out = Join-Path $OutRoot $head
    New-Item -ItemType Directory -Force $out | Out-Null
    $main = $proc.MainWindowHandle
    $script:CurrentPid = $proc.Id
    Note "== $head (pid $($proc.Id)) -> $out"
    foreach ($leftover in @(OtherWindows $proc.Id $main)) { Note "  closing leftover window '$([Native]::TitleOf($leftover))'"; [void][Native]::SetForegroundWindow($leftover); Pause 300; Keys "{ESC}"; Pause 400 }
    Foreground $main
    $win = [Native]::FrameRect($main)
    $wx = $win.Left; $wy = $win.Top; $ww = $win.Right - $win.Left; $wh = $win.Bottom - $win.Top
    Note "  window at $wx,$wy ${ww}x$wh"
    $root = Uia $main
    CaptureWindow $main (Join-Path $out "main.png")

    # Menus: File and Edit dropped open.
    foreach ($menu in @("File", "Edit")) {
        Note "- $menu menu"
        [void](ExpandMenu $root $menu)
        Capture $wx $wy 700 800 (Join-Path $out ("{0}-menu.png" -f $menu.ToLower()))
        CloseMenus
    }

    # Select the element from the tree search, then the Variables panel.
    Note "- select $Element"
    SelectViaSearch $Element
    Click ($wx + 415) ($wy + 492); Pause 400   # the Variables tab header, in case another tab is in front
    Capture ($wx + 380) ($wy + 480) 520 910 (Join-Path $out "variables.png")

    # Right-click menu on the selected component; then its Add object submenu.
    Note "- tree context menu"
    $node = FindByName $root $Element ([System.Windows.Automation.ControlType]::Text)
    if ($node -ne $null -and $head -eq "avalonia") { $c = Center $node; Click $c[0] $c[1] $true }
    else { Keys "^f"; Pause 200; Keys "{ESC}"; Pause 300; Keys "+{F10}" }   # Escape from the search box focuses the tree
    Pause 600
    Capture $wx ($wy + 30) 760 1000 (Join-Path $out "tree-context.png")
    Keys "{DOWN}"; Keys "{DOWN}"; Keys "{DOWN}"; Keys "{DOWN}"; Keys "{RIGHT}"; Pause 700
    Capture $wx ($wy + 30) 1000 1000 (Join-Path $out "add-instance.png")
    CloseMenus

    # Width Units row through the variable filter, then its combo dropped open.
    Note "- variable filter '$FilterText'"
    Keys "^e"; Pause 300; Keys "^a"; Keys $FilterText; Pause 900
    Capture ($wx + 380) ($wy + 480) 520 300 (Join-Path $out "width-units.png")
    Keys "^e"; Pause 200; Keys "^a"; Keys $ComboFilter; Pause 900
    Click ($wx + 680) ($wy + 592); Pause 300; Keys "%{DOWN}"; Pause 800   # focus the filtered row's combo, Alt+Down drops it open
    Capture ($wx + 380) ($wy + 480) 520 500 (Join-Path $out "combo-open.png")
    Keys "{ESC}"; Pause 300
    Keys "^e"; Pause 200; Keys "^a"; Keys "{BACKSPACE}"; Keys "{ESC}"; Pause 400
    Click ($wx + 415) ($wy + 492); Pause 400

    # Delete dialog on an instance.
    Note "- delete dialog on $Instance"
    SelectViaSearch $Instance
    Keys "{DELETE}"; Pause 1800
    if (-not (CaptureDialog $proc.Id $main (Join-Path $out "delete-dialog.png") "delete dialog")) { Capture $wx ($wy + 30) 900 700 (Join-Path $out "delete-dialog-missing.png") }
    CloseMenus; Pause 400

    # Project Properties from Edit > Properties (opens as a tab).
    Note "- project properties"
    [void](ExpandMenu $root "Edit")
    $props = FindByName $root "Properties" ([System.Windows.Automation.ControlType]::MenuItem)
    if ($props -eq $null) { Note "  MISSING: Edit > Properties not found"; CloseMenus }
    else { $c = Center $props; Click $c[0] $c[1]; Pause 1200; Capture ($wx + 380) ($wy + 480) 520 910 (Join-Path $out "project-properties.png") }
    # Add Component dialog: collapse the tree so the Components folder is the second row, then its
    # context menu's first item.
    Note "- add component dialog"
    Click ($wx + 17) ($wy + 71); Pause 500
    Click ($wx + 100) ($wy + 130); Pause 300
    Keys "{HOME}"; Keys "{DOWN}"; Pause 300
    Capture $wx ($wy + 30) 400 200 (Join-Path $out "tree-collapsed.png")
    $folder = FindByName $root "Components" ([System.Windows.Automation.ControlType]::Text)
    if ($folder -ne $null -and $head -eq "avalonia") { $c = Center $folder; Click $c[0] $c[1] $true } else { Keys "+{F10}" }
    Pause 600
    Keys "{DOWN}"; Keys "{ENTER}"; Pause 1800
    if (-not (CaptureDialog $proc.Id $main (Join-Path $out "add-component.png") "Add Component dialog")) { Capture $wx ($wy + 30) 900 700 (Join-Path $out "add-component-missing.png") }
    CloseMenus; Pause 400
    CaptureWindow $main (Join-Path $out "main-after.png")
}
Note "done"
$script:Log | Set-Content (Join-Path $OutRoot "last-run.log")
