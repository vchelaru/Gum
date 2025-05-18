# Purpose: Build all solutions in the specified directory.
# Usage: .\build-all.ps1 -Path <path-to-solutions>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Path
)

$ErrorActionPreference = 'Continue'
$failures = [System.Collections.Generic.List[string]]::new()
$successes = [System.Collections.Generic.List[string]]::new()

$slns = Get-ChildItem -Path $Path -Recurse -File -Filter '*.sln' |
        Select-Object -ExpandProperty FullName

if (-not $slns) {
    Write-Warning "No .sln files found under '$Path'."
    exit 1
} 
else {
    Write-Host "Found $($slns.Count) solution(s):"
    $slns | ForEach-Object { Write-Host "  - $_" }
}

foreach ($sln in $slns) {
    Write-Host "`n→ Restoring $sln"
    dotnet restore $sln -v q
    if ($LASTEXITCODE -ne 0) {
        $failures.Add("RESTORE failed: $(Split-Path $sln -Leaf)")
        continue
    }

    Write-Host "Building $(Split-Path $sln -Leaf)"
    dotnet build $sln `
      --configuration Release `
      --no-restore `
      --verbosity minimal `
      --property WarningLevel=0 `
      -clp:ErrorsOnly

    if ($LASTEXITCODE -ne 0) {
        $failures.Add("$(Split-Path $sln -Leaf)")
    } 
    else {
        $successes.Add("$(Split-Path $sln -Leaf)")
    }

    
}

if ($failures.Count -gt 0) {
    Write-Host "`n❌ Failures:`n$($failures -join "`n")" -ForegroundColor Red
    if ($successes.Count -gt 0) {
    Write-Host "`n✅ Successes:`n$($successes -join "`n")"
    }
    exit 1
}
else {
    Write-Host "`n✅ All solutions built successfully:`n$($successes -join "`n")" -ForegroundColor Green
}