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

.PARAMETER Theme
    Names of the themes to run. Defaults to every theme under Tools/Gum.ProjectServices/Templates/FormsThemes.
#>
param(
    [Parameter(Mandatory = $true)][string]$Cli,
    [string]$OutputRoot = "",
    [string[]]$Theme = @()
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

foreach ($name in $Theme) {
    # Named apart from the theme: a project called Bubblegum would put the theme's own Bubblegum namespace inside itself.
    $projectName = "${name}Game"
    $project = Join-Path $OutputRoot $projectName
    $steps = "new"

    try {
        & $Cli new $project --theme $name --platform monogame --source-linked --no-restore
        if ($LASTEXITCODE -ne 0) { throw "gumcli new exited $LASTEXITCODE" }

        $gumFolder = Join-Path $project "Content/GumProject"
        $settingsPath = Join-Path $gumFolder "ProjectCodeSettings.codsj"
        $settings = Get-Content $settingsPath -Raw
        if ($settings -notmatch '"ObjectInstantiationType":\s*1') { throw "scaffolded settings no longer default to FindByName" }
        $settings -replace '"ObjectInstantiationType":\s*1', '"ObjectInstantiationType": 0' | Set-Content $settingsPath

        $steps = "codegen"
        & $Cli codegen (Join-Path $gumFolder "GumProject.gumj")
        if ($LASTEXITCODE -ne 0) { throw "gumcli codegen exited $LASTEXITCODE" }

        $steps = "build"
        $csproj = Join-Path $project "$projectName.csproj"
        dotnet restore $csproj --verbosity quiet
        if ($LASTEXITCODE -ne 0) { throw "dotnet restore exited $LASTEXITCODE" }
        dotnet build $csproj --no-restore --verbosity quiet -p:WarningLevel=0
        if ($LASTEXITCODE -ne 0) { throw "dotnet build exited $LASTEXITCODE" }

        Write-Host "ok   $name"
    }
    catch {
        Write-Host "FAIL $name ($steps): $_"
        $failures += $name
    }
}

if ($deleteOutput -and (Test-Path $OutputRoot)) {
    Remove-Item -Recurse -Force $OutputRoot
}

if ($failures.Count -gt 0) {
    Write-Host "::error::Theme projects failed: $($failures -join ', ')"
    exit 1
}
