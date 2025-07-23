$proj = "C:\git\Gum\GumCommon\GumCommon.csproj"
$base = "C:\git\Gum\Python\PythonGum\src\gum_runtime\_clr"

foreach ($tfm in 'net6.0','netstandard2.0') {
    dotnet build $proj -c Debug -f $tfm `
        /p:CopyLocalLockFileAssemblies=true `
        /p:OutDir="$base\$tfm\"
}
