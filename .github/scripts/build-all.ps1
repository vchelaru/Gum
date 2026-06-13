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

$slns = Get-ChildItem -Path $Path -Recurse -File |
        Where-Object { $_.Extension -in '.sln', '.slnx' } |
        Select-Object -ExpandProperty FullName

if (-not $slns) {
    Write-Warning "No .sln / .slnx files found under '$Path'."
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
    # PublishAot=false: a few samples set <PublishAot>true</PublishAot>. On `dotnet build`
    # (this script never publishes) AOT does not actually compile to native — it only forces
    # resolution of the host-RID (win-x64) runtime pack, which the RID-less `dotnet restore`
    # above never downloads, so the build fails with "runtime pack ... was not downloaded".
    # Disabling AOT for the build removes that pointless requirement without losing coverage,
    # since AOT is only exercised on publish.
    dotnet build $sln `
      --configuration Release `
      --no-restore `
      --verbosity minimal `
      --property WarningLevel=0 `
      --property PublishAot=false `
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