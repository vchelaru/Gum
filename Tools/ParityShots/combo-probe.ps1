# Tries to drop the filtered Base Type combo open in one head and captures the result.
param([string]$ProcessName = "Gum", [int]$X = 877, [int]$Y = 592, [string]$Keys = "", [string]$Out = (Join-Path $PSScriptRoot "out\combo-probe.png"))
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Out) | Out-Null
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System; using System.Runtime.InteropServices;
public static class N2 {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
}
"@
[void][N2]::SetProcessDPIAware()
$proc = Get-Process -Name $ProcessName | Select-Object -First 1
[void][N2]::SetForegroundWindow($proc.MainWindowHandle); Start-Sleep -Milliseconds 600
[void][N2]::SetCursorPos($X, $Y); Start-Sleep -Milliseconds 100
[N2]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 60; [N2]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero)
Start-Sleep -Milliseconds 400
if ($Keys -ne "") { [System.Windows.Forms.SendKeys]::SendWait($Keys); Start-Sleep -Milliseconds 400 }
Start-Sleep -Milliseconds 900
$bmp = New-Object System.Drawing.Bitmap 520, 500
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen(380, 480, 0, 0, $bmp.Size)
$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
[System.Windows.Forms.SendKeys]::SendWait("{ESC}")
"saved $Out"
