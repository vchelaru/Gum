dotnet clean .\AllLibraries.sln
dotnet clean .\AllLibraries.sln -c Release
dotnet clean .\AllLibraries.sln -c Debug

dotnet clean .\Gum.sln
dotnet clean .\Gum.sln -c Release
dotnet clean .\Gum.sln -c Debug

dotnet clean .\SkiaGum.sln
dotnet clean .\SkiaGum.sln -c Release
dotnet clean .\SkiaGum.sln -c Debug

# Recursively find and delete all bin and obj folders
Get-ChildItem -Path . -Include bin,obj,objNetFramework -Recurse -Directory -Force | ForEach-Object {
    Remove-Item -Path $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
}