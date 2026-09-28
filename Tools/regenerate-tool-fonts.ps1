<#
.SYNOPSIS
Regenerates the tool's own bitmap fonts from Liberation Sans (SIL OFL 1.1) with
gumcli's KernSmith generator, so the release ships no renderings of a proprietary font (#5430).

.DESCRIPTION
Liberation Sans is metric-compatible with Arial, which the previous files were rendered from, so
line heights and advances match. Outputs:
  Gum/Content/TestFont.fnt + _0.png                    editor default bitmap font (size 20)
  Gum/Content/Fonts/Font18LiberationSans_o1.fnt + _0.png  ruler/dimension font (size 18, outline 1)
  XnaAndWinforms/Content/Font18LiberationSans.fnt + _0.png   Texture Coordinates tab font (size 18, embedded)

.EXAMPLE
pwsh Tools/regenerate-tool-fonts.ps1
#>
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot

dotnet build (Join-Path $repo 'Tools/Gum.Cli/Gum.Cli.csproj') -v q -nologo | Out-Null
if ($LASTEXITCODE -ne 0) { throw "gumcli build failed ($LASTEXITCODE)" }
$gumcli = Join-Path $repo 'Tools/Gum.Cli/bin/Debug/net10.0/gumcli.dll'

$work = Join-Path ([System.IO.Path]::GetTempPath()) ("gum-tool-fonts-" + [guid]::NewGuid())
$gumx = Join-Path $work 'ToolFonts.gumx'
dotnet $gumcli new $gumx --template empty | Out-Null
if ($LASTEXITCODE -ne 0) { throw "gumcli new failed ($LASTEXITCODE)" }

# The empty template's Text default is Fonts/LiberationSans-Regular.ttf.
@'
<?xml version="1.0" encoding="utf-8"?>
<ScreenSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Name>ToolFonts</Name>
  <State>
    <Name>Default</Name>
    <Variable Type="int" Name="RulerText.FontSize" SetsValue="true"><Value xsi:type="xsd:int">18</Value></Variable>
    <Variable Type="int" Name="RulerText.OutlineThickness" SetsValue="true"><Value xsi:type="xsd:int">1</Value></Variable>
    <Variable Type="int" Name="DefaultText.FontSize" SetsValue="true"><Value xsi:type="xsd:int">20</Value></Variable>
  </State>
  <Instance><Name>RulerText</Name><BaseType>Text</BaseType></Instance>
  <Instance><Name>DefaultText</Name><BaseType>Text</BaseType></Instance>
</ScreenSave>
'@ | Set-Content (Join-Path $work 'Screens/ToolFonts.gusx')
(Get-Content $gumx -Raw) -replace '  <SinglePixelTextureTop', "  <ScreenReference Name=`"ToolFonts`" />`n  <SinglePixelTextureTop" |
    Set-Content $gumx

dotnet $gumcli fonts $gumx
if ($LASTEXITCODE -ne 0) { throw "gumcli fonts failed ($LASTEXITCODE)" }

function Copy-Font([string]$sourceName, [string]$destination) {
    $cache = Join-Path $work 'FontCache'
    $destinationName = [System.IO.Path]::GetFileNameWithoutExtension($destination)
    $fnt = (Get-Content (Join-Path $cache "$sourceName.fnt") -Raw) -replace "file=`"$sourceName`_0.png`"", "file=`"$destinationName`_0.png`""
    Set-Content -Path $destination -Value $fnt -NoNewline
    Copy-Item (Join-Path $cache "$sourceName`_0.png") (Join-Path (Split-Path $destination) "$destinationName`_0.png") -Force
}

Copy-Font 'Font20LiberationSans-Regular_ttf' (Join-Path $repo 'Gum/Content/TestFont.fnt')
Copy-Font 'Font18LiberationSans-Regular_ttf_o1' (Join-Path $repo 'Gum/Content/Fonts/Font18LiberationSans_o1.fnt')
Copy-Font 'Font18LiberationSans-Regular_ttf' (Join-Path $repo 'XnaAndWinforms/Content/Font18LiberationSans.fnt')

Remove-Item $work -Recurse -Force
