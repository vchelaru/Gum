# Selects an element by name through the tree search in one head and captures a screen region.
param([string]$ProcessName = "Gum", [string]$Element = "TestScreen", [int]$X = 900, [int]$Y = 30, [int]$W = 1000, [int]$H = 800, [string]$Out = (Join-Path $PSScriptRoot "out\selected.png"))
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Out) | Out-Null
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System; using System.Runtime.InteropServices;
public static class N3 {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
}
"@
[void][N3]::SetProcessDPIAware()
$proc = Get-Process -Name $ProcessName | Select-Object -First 1
[void][N3]::ShowWindow($proc.MainWindowHandle, 3)
[void][N3]::SetForegroundWindow($proc.MainWindowHandle); Start-Sleep -Milliseconds 800
[System.Windows.Forms.SendKeys]::SendWait("^f"); Start-Sleep -Milliseconds 300
[System.Windows.Forms.SendKeys]::SendWait("^a"); [System.Windows.Forms.SendKeys]::SendWait($Element); Start-Sleep -Milliseconds 900
[System.Windows.Forms.SendKeys]::SendWait("{ENTER}"); Start-Sleep -Milliseconds 400
[System.Windows.Forms.SendKeys]::SendWait("{ESC}"); Start-Sleep -Milliseconds 2500
$bmp = New-Object System.Drawing.Bitmap $W, $H
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($X, $Y, 0, 0, $bmp.Size)
$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
"saved $Out"
