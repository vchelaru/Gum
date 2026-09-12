param([string]$ProcessName = "Gum", [int]$Depth = 4, [string]$Class = "", [string]$Id = "", [switch]$Raw)
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$proc = Get-Process -Name $ProcessName -ErrorAction Stop | Select-Object -First 1
"Process $($proc.ProcessName) pid $($proc.Id) hwnd $($proc.MainWindowHandle) title '$($proc.MainWindowTitle)'"
$root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)
function Dump($el, $indent, $depth) {
    if ($depth -lt 0) { return }
    $walker = if ($script:Raw) { [System.Windows.Automation.TreeWalker]::RawViewWalker } else { [System.Windows.Automation.TreeWalker]::ControlViewWalker }
    $child = $walker.GetFirstChild($el)
    while ($child -ne $null) {
        $c = $child.Current
        $name = $c.Name
        if ($name.Length -gt 40) { $name = $name.Substring(0, 40) }
        $r = $c.BoundingRectangle
        $rect = if ([double]::IsInfinity($r.X) -or [double]::IsInfinity($r.Width)) { "[offscreen]" } else { "[$([int]$r.X),$([int]$r.Y) $([int]$r.Width)x$([int]$r.Height)]" }
        "$indent$($c.ControlType.ProgrammaticName.Replace('ControlType.','')) '$name' id='$($c.AutomationId)' cls='$($c.ClassName)' $rect"
        Dump $child ($indent + "  ") ($depth - 1)
        $child = $walker.GetNextSibling($child)
    }
}
$script:Raw = $Raw
if ($Id -ne "") {
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, $Id)
    $root = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
    "Start at id $Id -> $($root -ne $null)"
}
if ($Class -ne "") {
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ClassNameProperty, $Class)
    $root = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
    "Start at class $Class -> $($root -ne $null)"
}
Dump $root "" $Depth
