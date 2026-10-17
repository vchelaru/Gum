<#
.SYNOPSIS
    Creates a MonoGame project from every bundled Forms theme with gumcli, generates its code, and compiles it.

.DESCRIPTION
    Each theme goes through `gumcli new --theme <name> --platform monogame --source-linked`, is switched to
    FullyInCode (the mode that emits per-variable assignments), is generated with `gumcli codegen`, and is
    built against this checkout's runtime projects. Exits 1 when any theme fails at any step.

.PARAMETER Cli
    Path to a built gumcli executable.

.PARAMETER OutputRoot
    Folder the projects are created in. Defaults to a new temp folder, deleted afterwards.

.PARAMETER Configuration
    Build configuration. CI passes the one the runtime projects were already built in, so they are not compiled twice.

.PARAMETER Theme
    Names of the themes to run. Defaults to every theme under Tools/Gum.ProjectServices/Templates/FormsThemes.
#>
param(
    [Parameter(Mandatory = $true)][string]$Cli,
    [string]$OutputRoot = "",
    [string[]]$Theme = @(),
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$deleteOutput = $false

if ($Theme.Count -eq 0) {
    $Theme = Get-ChildItem (Join-Path $repoRoot "Tools/Gum.ProjectServices/Templates/FormsThemes") -Directory |
        Select-Object -ExpandProperty Name
}

if ($OutputRoot -eq "") {
    $OutputRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("gum-themes-" + [guid]::NewGuid().ToString("N"))
    $deleteOutput = $true
}

$failures = @()

# Phase 1: create and generate every theme project. gumcli runs are independent processes, so they overlap.
$created = $Theme | ForEach-Object -ThrottleLimit 4 -Parallel {
    $name = $_
    $cli = $using:Cli
    # Named apart from the theme: a project called Bubblegum would put the theme's own Bubblegum namespace inside itself.
    $projectName = "${name}Game"
    $project = Join-Path $using:OutputRoot $projectName
    $steps = "new"
    $log = ""

    try {
        # 2>&1: gumcli's stderr banner would otherwise reach the parent's error stream, where "Stop" makes it fatal.
        $log += (& $cli new $project --theme $name --platform monogame --source-linked --no-restore 2>&1 | Out-String)
        if ($LASTEXITCODE -ne 0) { throw "gumcli new exited $LASTEXITCODE" }

        $gumFolder = Join-Path $project "Content/GumProject"
        $settingsPath = Join-Path $gumFolder "ProjectCodeSettings.codsj"
        $settings = Get-Content $settingsPath -Raw
        if ($settings -notmatch '"ObjectInstantiationType":\s*1') { throw "scaffolded settings no longer default to FindByName" }
        $settings -replace '"ObjectInstantiationType":\s*1', '"ObjectInstantiationType": 0' | Set-Content $settingsPath

        $steps = "codegen"
        $log += (& $cli codegen (Join-Path $gumFolder "GumProject.gumj") 2>&1 | Out-String)
        if ($LASTEXITCODE -ne 0) { throw "gumcli codegen exited $LASTEXITCODE" }

        [pscustomobject]@{ Name = $name; Csproj = (Join-Path $project "$projectName.csproj"); Error = $null; Log = $log }
    }
    catch {
        [pscustomobject]@{ Name = $name; Csproj = $null; Error = "($steps): $_"; Log = $log }
    }
}

foreach ($result in $created | Where-Object { $_.Error }) {
    Write-Host $result.Log
    Write-Host "FAIL $($result.Name) $($result.Error)"
    $failures += $result.Name
}

# Phase 2: one restore and one parallel build over every theme project, so the shared runtime projects
# they all reference build once.
$built = @($created | Where-Object { -not $_.Error })
if ($built.Count -gt 0) {
    $solution = Join-Path $OutputRoot "Themes.slnx"
    $entries = ($built | ForEach-Object { "  <Project Path=`"$($_.Csproj)`" />" }) -join "`n"
    Set-Content $solution "<Solution>`n$entries`n</Solution>"

    dotnet restore $solution --verbosity quiet
    $ok = $LASTEXITCODE -eq 0
    if ($ok) {
        dotnet build $solution --configuration $Configuration --no-restore -m --verbosity quiet -p:WarningLevel=0
        $ok = $LASTEXITCODE -eq 0
    }

    if ($ok) {
        foreach ($result in $built) { Write-Host "ok   $($result.Name)" }
    }
    else {
        # Name the failing themes: rebuild each one alone (the shared projects are already built, so this is quick).
        foreach ($result in $built) {
            dotnet build $result.Csproj --configuration $Configuration --no-restore --verbosity quiet -p:WarningLevel=0
            if ($LASTEXITCODE -eq 0) { Write-Host "ok   $($result.Name)" }
            else { Write-Host "FAIL $($result.Name) (build)"; $failures += $result.Name }
        }
        if ($failures.Count -eq 0) { $failures += "(combined restore/build)" }
    }
}

if ($deleteOutput -and (Test-Path $OutputRoot)) {
    Remove-Item -Recurse -Force $OutputRoot
}

if ($failures.Count -gt 0) {
    Write-Host "::error::Theme projects failed: $($failures -join ', ')"
    exit 1
}
