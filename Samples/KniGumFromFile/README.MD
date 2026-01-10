Kni For Android requires:
1. Android studio
2. Android SDK installed
3. Emulator downloaded and installed
4. DOTNET Workflows for android
5. global.json file in root of solution where sln file is with contents
```json
{
  "sdk": {
    "version": "8.0.416",
    "rollForward": "latestPatch"
  }
}
```
6. Directory.Build.props file in root of solution that tells Visual Studio 2026 to use older build logic/tools, allowing to compile in VS2026.
```xml
<Project>
  <PropertyGroup>
    <!-- "17.0" corresponds to VS 2022 -->
    <MinimumVisualStudioVersion>17.0</MinimumVisualStudioVersion>
    <VisualStudioVersion>17.0</VisualStudioVersion>
  </PropertyGroup>
</Project>
```

7. If you're still having issues you can open the solution file in Visual Studio 2022 to help force it to dotnet 8