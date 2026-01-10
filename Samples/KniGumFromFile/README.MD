Kni For Android requires:
1. Android studio
2. Android SDK intalled
3. Emulator downloaded and installed
4. DOTNET Workflowas for android
5. Visual Studio 2022 (currently to target dotnet8 and not 9 or 10)
6. global.json file in root of solution where sln file is with contents
```json
{
  "sdk": {
    "version": "8.0.416",
    "rollForward": "latestPatch"
  }
}
```