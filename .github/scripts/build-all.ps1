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

# Solutions that can't be built by this generic windows-runner sweep and need dedicated CI.
# MauiSkiaGum is a MAUI multi-platform app: its net9.0-windows (WinUI) head is self-contained
# and needs the win-x64 runtime pack (NETSDK1112) the RID-less restore can't supply, and its
# net9.0-ios head can't build on a Windows runner at all (needs a Mac). Build it in a dedicated
# MAUI workflow instead.
#
# The SokolGum samples need Sokol.NET, which is no longer a submodule (it dragged in 11 nested
# submodules and ~143 MB of native binaries, and broke `git pull` for anyone who initialized it).
# Building them requires cloning Sokol.NET by hand — see Runtimes/SokolGum/README.md.
$excludeLeafNames = @('MauiSkiaGum.sln', 'SokolGumSample.slnx', 'SokolGumFromFile.slnx')

$slns = Get-ChildItem -Path $Path -Recurse -File |
        Where-Object { $_.Extension -in '.sln', '.slnx' } |
        Where-Object { $excludeLeafNames -notcontains $_.Name } |
        Select-Object -ExpandProperty FullName

if (-not $slns) {
    Write-Warning "No .sln / .slnx files found under '$Path'."
    exit 1
}
else {
    Write-Host "Found $($slns.Count) solution(s):"
    $slns | ForEach-Object { Write-Host "  - $_" }
    if ($excludeLeafNames.Count -gt 0) {
        Write-Host "Skipping (excluded, need dedicated CI): $($excludeLeafNames -join ', ')"
    }
}

$maxAttempts = 3

foreach ($sln in $slns) {
    $leaf = Split-Path $sln -Leaf
    $succeeded = $false
    $lastFailureStage = $null

    for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
        if ($attempt -gt 1) {
            # A build-time tool (e.g. Stride.Core's AssemblyProcessor) can extract its own helper
            # exe/dll to a shared, content-hash-keyed temp path and still hold it open (or have it
            # locked by Windows Defender scanning the fresh file) for a moment after the process
            # that ran it exits. Retrying after a short pause clears this without masking a real
            # compile error, which fails identically on every attempt (#4877 CI flake).
            Write-Host "Retrying $leaf (attempt $attempt of $maxAttempts) after a transient failure..."
            Start-Sleep -Seconds 10
        }

        Write-Host "`n→ Restoring $sln"
        dotnet restore $sln -v q
        if ($LASTEXITCODE -ne 0) {
            $lastFailureStage = 'RESTORE'
            continue
        }

        Write-Host "Building $leaf"
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

        if ($LASTEXITCODE -eq 0) {
            $succeeded = $true
            break
        }
        $lastFailureStage = 'BUILD'
    }

    if ($succeeded) {
        $successes.Add($leaf)
    }
    elseif ($lastFailureStage -eq 'RESTORE') {
        $failures.Add("RESTORE failed: $leaf")
    }
    else {
        $failures.Add($leaf)
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