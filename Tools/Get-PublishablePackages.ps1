<#
.SYNOPSIS
    Lists the .nupkg files a publish step may push, leaving out the ones held back.

.DESCRIPTION
    Gum.KniSkia and the Gum.Themes.*.KniSkia packages still build, pack and test every day but are not
    pushed until they ship. Both publish steps in dotnet-nuget.yaml take their list from here.
    Remove in December 2026 when Gum.KniSkia ships (#6380).
#>
param([string]$PackageDirectory = './nupkgs')

$held = '^Gum\.KniSkia\.\d', '^Gum\.Themes\.[^.]+\.KniSkia\.\d'
Get-ChildItem -Path $PackageDirectory -Filter '*.nupkg' | Where-Object {
    $name = $_.Name
    -not ($held | Where-Object { $name -match $_ })
}
