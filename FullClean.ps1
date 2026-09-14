dotnet clean .\AllLibraries.sln
dotnet clean .\AllLibraries.sln -c Release
dotnet clean .\AllLibraries.sln -c Debug

dotnet clean .\Gum.slnx
dotnet clean .\Gum.Wpf.sln
dotnet clean .\Gum.slnx -c Release
dotnet clean .\Gum.Wpf.sln -c Release
dotnet clean .\Gum.slnx -c Debug
dotnet clean .\Gum.Wpf.sln -c Debug

dotnet clean .\SkiaGum.sln
dotnet clean .\SkiaGum.sln -c Release
dotnet clean .\SkiaGum.sln -c Debug

# Recursively find and delete all bin and obj folders
Get-ChildItem -Path . -Include bin,obj,objNetFramework -Recurse -Directory -Force | ForEach-Object {
    Remove-Item -Path $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
}