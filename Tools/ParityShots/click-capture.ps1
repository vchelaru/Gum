# Brings a head to the front, clicks a point, waits, and captures a screen region.
param([string]$ProcessName = "Gum", [int]$ClickX = 1050, [int]$ClickY = 858, [int]$X = 895, [int]$Y = 848, [int]$W = 1660, [int]$H = 540, [string]$Out = (Join-Path $PSScriptRoot "out\clicked.png"))
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Out) | Out-Null
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System; using System.Runtime.InteropServices;
public static class N4 {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
}
"@
[void][N4]::SetProcessDPIAware()
$proc = Get-Process -Name $ProcessName | Select-Object -First 1
[void][N4]::SetForegroundWindow($proc.MainWindowHandle); Start-Sleep -Milliseconds 600
[void][N4]::SetCursorPos($ClickX, $ClickY); Start-Sleep -Milliseconds 100
[N4]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 60; [N4]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero)
Start-Sleep -Milliseconds 900
$bmp = New-Object System.Drawing.Bitmap $W, $H
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($X, $Y, 0, 0, $bmp.Size)
$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
"saved $Out"
