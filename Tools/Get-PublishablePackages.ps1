<#
.SYNOPSIS
    Lists the .nupkg files a publish step may push, leaving out the ones held back.

.DESCRIPTION
    Gum.KniSkia and the Gum.Themes.*.KniSkia packages still build, pack and test every day. Their
    stable versions are not pushed until they ship; a prerelease version (a '-' label such as
    2026.4.23.1-preview.1) is pushed so the packages can be tested. Both publish steps in
    dotnet-nuget.yaml take their list from here.
    Remove in December 2026 when Gum.KniSkia ships (#6380).
#>
param([string]$PackageDirectory = './nupkgs')

# A stable version is digits and dots only, so a '-' label after the id makes it a prerelease and unheld.
$held = '^Gum\.KniSkia\.[\d.]+\.nupkg$', '^Gum\.Themes\.[^.]+\.KniSkia\.[\d.]+\.nupkg$'
Get-ChildItem -Path $PackageDirectory -Filter '*.nupkg' | Where-Object {
    $name = $_.Name
    -not ($held | Where-Object { $name -match $_ })
}
