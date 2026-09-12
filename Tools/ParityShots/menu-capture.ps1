# Opens a menu path (top-level item, then sub-items by name) through UI Automation in one head and
# captures the dialog window that appears (or the main window region when none does).
param([string]$ProcessName = "Gum", [string[]]$Path = @("Content", "Import", "HTML…"), [string]$Out = (Join-Path $PSScriptRoot "out\menu-dialog.png"), [int]$WaitMs = 1500)
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Out) | Out-Null
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System; using System.Runtime.InteropServices; using System.Collections.Generic;
public static class N5 {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out RECT rect, int size);
    public delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc proc, IntPtr lParam);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    public static List<IntPtr> VisibleWindows(uint pid) { var l = new List<IntPtr>(); EnumWindows((h, p) => { uint q; GetWindowThreadProcessId(h, out q); if (q == pid && IsWindowVisible(h)) l.Add(h); return true; }, IntPtr.Zero); return l; }
    public static RECT FrameRect(IntPtr h) { RECT r; if (DwmGetWindowAttribute(h, 9, out r, Marshal.SizeOf(typeof(RECT))) != 0) GetWindowRect(h, out r); return r; }
}
"@
[void][N5]::SetProcessDPIAware()
$proc = Get-Process -Name $ProcessName | Select-Object -First 1
$main = $proc.MainWindowHandle
[void][N5]::SetForegroundWindow($main); Start-Sleep -Milliseconds 600
function Click($x, $y) { [void][N5]::SetCursorPos([int]$x, [int]$y); Start-Sleep -Milliseconds 80; [N5]::mouse_event(2,0,0,0,[UIntPtr]::Zero); Start-Sleep -Milliseconds 50; [N5]::mouse_event(4,0,0,0,[UIntPtr]::Zero); Start-Sleep -Milliseconds 500 }
function FindItem($name) {
    $cond = New-Object System.Windows.Automation.AndCondition(
        (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $name)),
        (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::MenuItem)))
    $root = [System.Windows.Automation.AutomationElement]::FromHandle($main)
    $found = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
    if ($found -ne $null) { return $found }
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
$others = [N5]::VisibleWindows([uint32]$proc.Id) | Where-Object { $_ -ne $main }
$target = $others | Sort-Object { $r = [N5]::FrameRect($_); ($r.Right - $r.Left) * ($r.Bottom - $r.Top) } -Descending | Select-Object -First 1
if ($target -eq $null) { "no dialog window; capturing the main window"; $target = $main }
$r = [N5]::FrameRect($target)
$bmp = New-Object System.Drawing.Bitmap ($r.Right - $r.Left), ($r.Bottom - $r.Top)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($r.Left, $r.Top, 0, 0, $bmp.Size)
$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
"saved $Out ($($bmp.Width)x$($bmp.Height))"
[System.Windows.Forms.SendKeys]::SendWait("{ESC}")
