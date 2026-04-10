# Selective NuGet Publishing Plan

## Problem

The current `dotnet-nuget.yaml` workflow publishes **all** packages when triggered. There's no way to publish only specific packages (e.g. just `FlatRedBall.GumCommon`).

## Solution

Add individual boolean checkbox inputs per package to the `workflow_dispatch` section. The publish step filters `.nupkg` files to only push the selected ones.

## File to modify

`.github/workflows/dotnet-nuget.yaml`

## Packages (15 total)

These are the PackageIds from the repo's `.csproj` files:

| PackageId | Project |
|-----------|---------|
| `FlatRedBall.GumCommon` | `GumCommon/GumCommon.csproj` |
| `FlatRedBall.GumDataTypes` | `GumDataTypes/GumDataTypesNet6.csproj` |
| `FlatRedBall.ToolsUtilities.NetStandard` | `ToolsUtilities/ToolsUtilitiesStandard.csproj` |
| `Gum.MonoGame` | `MonoGameGum/MonoGameGum.csproj` |
| `Gum.KNI` | `MonoGameGum/KniGum/KniGum.csproj` |
| `Gum.FNA` | `MonoGameGum/FnaGum/FnaGum.csproj` |
| `Gum.SkiaSharp` | `Runtimes/SkiaGum/SkiaGum.csproj` |
| `Gum.SkiaSharp.Maui` | `Runtimes/SkiaGum.Maui/SkiaGum.Maui.csproj` |
| `Gum.raylib` | `Runtimes/RaylibGum/RaylibGum.csproj` |
| `Gum.Shapes.MonoGame` | `Runtimes/GumShapes/MonoGameGumShapes.csproj` |
| `Gum.Shapes.KNI` | `Runtimes/GumShapes/KniGumShapes.csproj` |
| `Gum.Expressions` | `Runtimes/GumExpressions/GumExpressions.csproj` |
| `Gum.Themes.Editor.MonoGame` | `Themes/Gum.Themes.Editor.MonoGame/...csproj` |
| `Gum.Themes.Editor.Kni` | `Themes/Gum.Themes.Editor.Kni/...csproj` |
| `GumCli` | `Tools/Gum.Cli/Gum.Cli.csproj` (packed separately) |

## Changes

### 1. Add checkbox inputs

Add one boolean input per package under `workflow_dispatch.inputs`. All default to `true` so the existing "publish everything" behavior is preserved when you just click Run without changing anything.

```yaml
on:
  workflow_dispatch:
    inputs:
      publish_to_nuget:
        description: 'Publish to NuGet.org'
        required: false
        default: false
        type: boolean
      publish_to_github:
        description: 'Publish to GitHub Packages'
        required: false
        default: false
        type: boolean
      # --- Package selection ---
      pkg_GumCommon:
        description: 'FlatRedBall.GumCommon'
        type: boolean
        default: true
      pkg_GumDataTypes:
        description: 'FlatRedBall.GumDataTypes'
        type: boolean
        default: true
      pkg_ToolsUtilities:
        description: 'FlatRedBall.ToolsUtilities.NetStandard'
        type: boolean
        default: true
      pkg_MonoGame:
        description: 'Gum.MonoGame'
        type: boolean
        default: true
      pkg_KNI:
        description: 'Gum.KNI'
        type: boolean
        default: true
      pkg_FNA:
        description: 'Gum.FNA'
        type: boolean
        default: true
      pkg_SkiaSharp:
        description: 'Gum.SkiaSharp'
        type: boolean
        default: true
      pkg_SkiaSharpMaui:
        description: 'Gum.SkiaSharp.Maui'
        type: boolean
        default: true
      pkg_Raylib:
        description: 'Gum.raylib'
        type: boolean
        default: true
      pkg_ShapesMonoGame:
        description: 'Gum.Shapes.MonoGame'
        type: boolean
        default: true
      pkg_ShapesKNI:
        description: 'Gum.Shapes.KNI'
        type: boolean
        default: true
      pkg_Expressions:
        description: 'Gum.Expressions'
        type: boolean
        default: true
      pkg_ThemesMonoGame:
        description: 'Gum.Themes.Editor.MonoGame'
        type: boolean
        default: true
      pkg_ThemesKni:
        description: 'Gum.Themes.Editor.Kni'
        type: boolean
        default: true
      pkg_GumCli:
        description: 'GumCli'
        type: boolean
        default: true
```

### 2. Replace the publish steps

Replace the current "push all .nupkg files" logic with a filtered push. Build a list of selected PackageIds from the checkbox inputs, then only push `.nupkg` files whose names match.

The publish step for NuGet.org becomes (PowerShell):

```powershell
# Build list of selected package IDs
$selected = @()
if ('${{ github.event.inputs.pkg_GumCommon }}' -eq 'true') { $selected += 'FlatRedBall.GumCommon' }
if ('${{ github.event.inputs.pkg_GumDataTypes }}' -eq 'true') { $selected += 'FlatRedBall.GumDataTypes' }
if ('${{ github.event.inputs.pkg_ToolsUtilities }}' -eq 'true') { $selected += 'FlatRedBall.ToolsUtilities.NetStandard' }
if ('${{ github.event.inputs.pkg_MonoGame }}' -eq 'true') { $selected += 'Gum.MonoGame' }
if ('${{ github.event.inputs.pkg_KNI }}' -eq 'true') { $selected += 'Gum.KNI' }
if ('${{ github.event.inputs.pkg_FNA }}' -eq 'true') { $selected += 'Gum.FNA' }
if ('${{ github.event.inputs.pkg_SkiaSharp }}' -eq 'true') { $selected += 'Gum.SkiaSharp' }
if ('${{ github.event.inputs.pkg_SkiaSharpMaui }}' -eq 'true') { $selected += 'Gum.SkiaSharp.Maui' }
if ('${{ github.event.inputs.pkg_Raylib }}' -eq 'true') { $selected += 'Gum.raylib' }
if ('${{ github.event.inputs.pkg_ShapesMonoGame }}' -eq 'true') { $selected += 'Gum.Shapes.MonoGame' }
if ('${{ github.event.inputs.pkg_ShapesKNI }}' -eq 'true') { $selected += 'Gum.Shapes.KNI' }
if ('${{ github.event.inputs.pkg_Expressions }}' -eq 'true') { $selected += 'Gum.Expressions' }
if ('${{ github.event.inputs.pkg_ThemesMonoGame }}' -eq 'true') { $selected += 'Gum.Themes.Editor.MonoGame' }
if ('${{ github.event.inputs.pkg_ThemesKni }}' -eq 'true') { $selected += 'Gum.Themes.Editor.Kni' }
if ('${{ github.event.inputs.pkg_GumCli }}' -eq 'true') { $selected += 'GumCli' }

Write-Host "Selected packages: $($selected -join ', ')"

Get-ChildItem "./nupkgs/*.nupkg" | ForEach-Object {
  $name = $_.BaseName -replace '\.\d+\.\d+\.\d+.*$', ''
  if ($selected -contains $name) {
    Write-Host "Publishing $($_.Name)..."
    dotnet nuget push $_.FullName --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate
  } else {
    Write-Host "Skipping $($_.Name) (not selected)"
  }
}
```

Apply the same pattern to the GitHub Packages publish step.

### 3. Build and pack steps — no changes

All packages are still built and packed every run. Only the **push** is filtered. This keeps things simple and ensures you always get the full set of artifacts to inspect.

## Notes

- All checkboxes default to `true`, so clicking Run without unchecking anything = current behavior (publish all)
- To publish just one package: uncheck all, check only the one you want
- The `.nupkg` filename regex strips the version suffix to match against PackageId
- The `.snupkg` symbol packages will follow the same filtering pattern
