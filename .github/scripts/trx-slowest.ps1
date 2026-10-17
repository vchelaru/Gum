# Writes the slowest tests and test classes from the .trx files in a folder to the job summary,
# so a slow suite can be explained from a CI run without downloading anything. (#6092)
param(
    [string]$ResultsDirectory = "TestResults",
    [int]$Top = 25
)

$ErrorActionPreference = "Stop"

$rows = @()
foreach ($file in Get-ChildItem -Path $ResultsDirectory -Filter *.trx -ErrorAction SilentlyContinue) {
    [xml]$trx = Get-Content -Path $file.FullName
    $ns = New-Object System.Xml.XmlNamespaceManager($trx.NameTable)
    $ns.AddNamespace("t", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")
    foreach ($result in $trx.SelectNodes("//t:UnitTestResult", $ns)) {
        $name = $result.testName
        $dot = $name.LastIndexOf(".")
        $paren = $name.IndexOf("(")
        if ($paren -gt 0) { $dot = $name.LastIndexOf(".", $paren) }
        $rows += [pscustomobject]@{
            Suite    = $file.BaseName
            Class    = if ($dot -gt 0) { $name.Substring(0, $dot) } else { $name }
            Test     = $name
            Seconds  = [TimeSpan]::Parse($result.duration).TotalSeconds
        }
    }
}

if ($rows.Count -eq 0) {
    Write-Host "No .trx results found in $ResultsDirectory"
    exit 0
}

$lines = @()
$lines += "### Slowest test classes (total seconds)"
$lines += "| Class | Tests | Seconds |"
$lines += "|---|---|---|"
$rows | Group-Object Class |
    ForEach-Object { [pscustomobject]@{ Class = $_.Name; Count = $_.Count; Seconds = ($_.Group | Measure-Object Seconds -Sum).Sum } } |
    Sort-Object Seconds -Descending | Select-Object -First $Top |
    ForEach-Object { $lines += "| $($_.Class) | $($_.Count) | $([math]::Round($_.Seconds, 1)) |" }

$lines += ""
$lines += "### Slowest tests (seconds)"
$lines += "| Test | Seconds |"
$lines += "|---|---|"
$rows | Sort-Object Seconds -Descending | Select-Object -First $Top |
    ForEach-Object { $lines += "| $($_.Test) | $([math]::Round($_.Seconds, 2)) |" }

$lines += ""
$total = ($rows | Measure-Object Seconds -Sum).Sum
$lines += "$($rows.Count) tests, $([math]::Round($total, 1)) s summed test time."

$text = $lines -join "`n"
Write-Host $text
if ($env:GITHUB_STEP_SUMMARY) {
    Add-Content -Path $env:GITHUB_STEP_SUMMARY -Value $text
}
