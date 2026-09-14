param([string]$ProcessName = "Gum.Avalonia", [string[]]$Path = @("Edit", "Add", "Screen"), [int]$WaitMs = 1500, [string]$Button = "Cancel", [switch]$PreClickCaption)
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
public static class N6 {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder sb, int max);
    [DllImport("user32.dll")] public static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder sb, int max);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    public delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc proc, IntPtr lParam);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    public static List<IntPtr> VisibleWindows(uint pid) {
        var list = new List<IntPtr>();
        EnumWindows((h, l) => { uint p; GetWindowThreadProcessId(h, out p); if (p == pid && IsWindowVisible(h)) list.Add(h); return true; }, IntPtr.Zero);
        return list;
    }
    public static string TitleOf(IntPtr h) { var sb = new System.Text.StringBuilder(512); GetWindowText(h, sb, 512); return sb.ToString(); }
    public static string ClassOf(IntPtr h) { var sb = new System.Text.StringBuilder(256); GetClassName(h, sb, 256); return sb.ToString(); }
}
"@
[void][N6]::SetProcessDPIAware()
function Click($x, $y) { [void][N6]::SetCursorPos([int]$x, [int]$y); Start-Sleep -Milliseconds 80; [N6]::mouse_event(2,0,0,0,[UIntPtr]::Zero); Start-Sleep -Milliseconds 50; [N6]::mouse_event(4,0,0,0,[UIntPtr]::Zero); Start-Sleep -Milliseconds 600 }

$proc = Get-Process -Name $ProcessName | Select-Object -First 1
$main = $proc.MainWindowHandle
[void][N6]::ShowWindow($main, 3); [void][N6]::SetForegroundWindow($main); Start-Sleep -Milliseconds 800
"main: $main '$([N6]::TitleOf($main))' foreground=$([N6]::GetForegroundWindow())"

function FindItem($name) {
    $cond = New-Object System.Windows.Automation.AndCondition(
        (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $name)),
        (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::MenuItem)))
    $byPid = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, [int]$proc.Id)
    foreach ($top in [System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children, $byPid)) {
        $found = $top.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
        if ($found -ne $null) { return $found }
    }
    return $null
}
foreach ($name in $Path) {
    $item = FindItem $name
    if ($item -eq $null) { "menu item '$name' not found"; exit 1 }
    $r = $item.Current.BoundingRectangle
    Click ($r.X + $r.Width / 2) ($r.Y + $r.Height / 2)
}
Start-Sleep -Milliseconds $WaitMs

$others = [N6]::VisibleWindows([uint32]$proc.Id) | Where-Object { $_ -ne $main }
"other visible windows: $($others.Count)"
$dialog = $others | Select-Object -First 1
if ($dialog -eq $null) { "no dialog window appeared"; exit 1 }
$fg = [N6]::GetForegroundWindow()
"dialog: $dialog '$([N6]::TitleOf($dialog))' class=$([N6]::ClassOf($dialog)) exstyle=0x$('{0:X}' -f [N6]::GetWindowLong($dialog, -20))"
"foreground after open: $fg  (isDialog=$($fg -eq $dialog) isMain=$($fg -eq $main))"

# Focused element per UI Automation
$focused = [System.Windows.Automation.AutomationElement]::FocusedElement
"UIA focused element: '$($focused.Current.Name)' type=$($focused.Current.ControlType.ProgrammaticName) pid=$($focused.Current.ProcessId)"

# First click on Cancel: does the dialog close?
$root = [System.Windows.Automation.AutomationElement]::FromHandle($dialog)
if ($PreClickCaption) { $wr = New-Object N6+RECT; [void][N6]::GetWindowRect($dialog, [ref]$wr); Click (($wr.Left + $wr.Right) / 2) ($wr.Top + 12); "pre-clicked caption; foreground=$([N6]::GetForegroundWindow()) isDialog=$([N6]::GetForegroundWindow() -eq $dialog)" }
$cancelCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $Button)
$cancel = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cancelCond)
if ($cancel -eq $null) { "no $Button button found in dialog"; [System.Windows.Forms.SendKeys]::SendWait("{ESC}"); exit 1 }
$r = $cancel.Current.BoundingRectangle
Click ($r.X + $r.Width / 2) ($r.Y + $r.Height / 2)
$stillOpen = [N6]::IsWindowVisible($dialog)
"after first click on Cancel: dialog still open = $stillOpen ; foreground=$([N6]::GetForegroundWindow()) (isDialog=$([N6]::GetForegroundWindow() -eq $dialog))"
if ($stillOpen) {
    Click ($r.X + $r.Width / 2) ($r.Y + $r.Height / 2)
    "after second click on Cancel: dialog still open = $([N6]::IsWindowVisible($dialog))"
}
