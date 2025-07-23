$here  = $PSScriptRoot
$proj  = Join-Path $here '..\..\..\GumCommon\GumCommon.csproj'
$out   = Join-Path $here '..\src\gum_runtime\_clr'
$cfg   = 'Debug'
$tfms  = 'net6.0'

foreach ($tfm in $tfms) {
    $dir = Join-Path $out $tfm
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    dotnet build $proj -c $cfg -f $tfm `
        /p:CopyLocalLockFileAssemblies=true `
        /p:OutDir="$dir\"
}