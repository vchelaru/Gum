param([string]$ProcessName = "Gum.Avalonia", [string]$Menu = "Edit", [string]$Out = (Join-Path $PSScriptRoot "out\menu-open.png"))
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class N7 {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool IsWindowEnabled(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
"@
[void][N7]::SetProcessDPIAware()
function Click($x, $y) { [void][N7]::SetCursorPos([int]$x, [int]$y); Start-Sleep -Milliseconds 80; [N7]::mouse_event(2,0,0,0,[UIntPtr]::Zero); Start-Sleep -Milliseconds 50; [N7]::mouse_event(4,0,0,0,[UIntPtr]::Zero) }
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Out) | Out-Null
$proc = Get-Process -Name $ProcessName | Select-Object -First 1
$main = $proc.MainWindowHandle
"main enabled=$([N7]::IsWindowEnabled($main)) foreground=$([N7]::GetForegroundWindow() -eq $main)"
[void][N7]::SetForegroundWindow($main); Start-Sleep -Milliseconds 500
$root = [System.Windows.Automation.AutomationElement]::FromHandle($main)
$cond = New-Object System.Windows.Automation.AndCondition(
    (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $Menu)),
    (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::MenuItem)))
$item = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
if ($item -eq $null) { "top-level '$Menu' not found"; exit 1 }
$r = $item.Current.BoundingRectangle
Click ($r.X + $r.Width / 2) ($r.Y + $r.Height / 2)
Start-Sleep -Milliseconds 900
$wr = New-Object N7+RECT; [void][N7]::GetWindowRect($main, [ref]$wr)
$bmp = New-Object System.Drawing.Bitmap 520, 320
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($wr.Left, $wr.Top, 0, 0, $bmp.Size)
$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
"saved $Out; foreground is main=$([N7]::GetForegroundWindow() -eq $main)"
[System.Windows.Forms.SendKeys]::SendWait("{ESC}")
