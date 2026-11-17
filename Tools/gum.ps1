# Builds Gum.slnx (the Avalonia head and its plugins) and runs the tool from the checkout this script lives in.
# Arguments are passed to the tool, e.g. `gum path/to/project.gumx`.
# The Preview variants are skipped because they are slow and rarely change; when testing something the Preview runs,
# build Tool/GumPreview/GumPreview.csproj first.

$repoRoot = Split-Path -Parent $PSScriptRoot

dotnet build (Join-Path $repoRoot 'Gum.slnx') -p:SkipPreviewVariantPublish=true
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$exeName = if ($IsWindows) { 'Gum.exe' } else { 'Gum' }
& (Join-Path $repoRoot "Tool/Gum.Avalonia/bin/Debug/net10.0/$exeName") @args
