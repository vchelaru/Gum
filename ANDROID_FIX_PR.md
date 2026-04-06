# Fix: Android/iOS File Loading in GumFileSerializer.ReadAndDetectFormat

## Problem

Gum fails to load `.gumx` project files and referenced files (`.gusx`, `.gucx`, `.gutx`, `.behx`) on Android and iOS platforms with errors like:

```
Error loading DemoScreenGum:
Could not find a part of the path '/Content/GumProject/Screens/DemoScreenGum.gusx'.
```

## Root Cause

The method `GumFileSerializer.ReadAndDetectFormat()` in `GumDataTypes/Serialization/GumFileSerializer.cs` uses `File.ReadAllText(fileName)` directly, which attempts to access the file system using absolute paths. On Android and iOS, content files are bundled inside the application package (APK/IPA) and cannot be accessed via standard file system APIs.

## Solution

Changed line 145 in `GumFileSerializer.cs` from:

```csharp
string content = File.ReadAllText(fileName);
```

To:

```csharp
stringcontent = FileManager.FromFileText(fileName);
```

`FileManager.FromFileText()` is the cross-platform file reading method that:
- On Windows/macOS/Linux: Uses standard file system access
- On Android/iOS: Uses `TitleContainer.OpenStream()` to read files from the application bundle
- Supports custom stream loaders via `FileManager.CustomGetStreamFromFile` delegate

## Files Changed

- `GumDataTypes/Serialization/GumFileSerializer.cs` (1 line)

## Testing

- Verified that Gum projects load correctly on Android
- Existing desktop functionality remains unchanged
- The fix is consistent with how other file loading is handled in Gum (e.g., `FileManager.XmlDeserialize`)

## Related Code

This fix aligns with the existing cross-platform file loading pattern used elsewhere in Gum:

```csharp
// FileManager.cs line 747-779
public static Stream GetStreamForFile(string fileName)
{
    try
    {
#if ANDROID || IOS
        fileName = TryRemoveLeadingDotSlash(fileName);
        return Microsoft.Xna.Framework.TitleContainer.OpenStream(fileName);
#else
        if (CustomGetStreamFromFile != null)
        {
            return CustomGetStreamFromFile(fileName);
        }
        else
        {
            // ...standard file access...
        }
#endif
    }
    // ...
}
```

## Impact

This is a minimal, non-breaking change that enables Gum to work correctly on mobile platforms without affecting existing desktop functionality.