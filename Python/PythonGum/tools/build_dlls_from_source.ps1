$here  = $PSScriptRoot
$out   = Join-Path $here '..\src\gum_runtime\_clr'
$cfg   = 'Debug'
$tfms  = 'net6.0'

# paths to your projects
$gumCommonProj = Join-Path $here '..\..\..\GumCommon\GumCommon.csproj'
$helperProj    = Join-Path $here '..\..\GumToPythonHelpers\GumToPythonHelpers.csproj'

# function to build and copy
function Build-And-Copy([string]$proj) {
    foreach ($tfm in $tfms) {
        $dir = Join-Path $out $tfm
        New-Item -ItemType Directory -Force -Path $dir | Out-Null

        dotnet build $proj -c $cfg -f $tfm `
            /p:CopyLocalLockFileAssemblies=true `
            /p:OutDir="$dir\" | Out-Null
    }
}

# build both projects
Build-And-Copy $gumCommonProj
Build-And-Copy $helperProj
