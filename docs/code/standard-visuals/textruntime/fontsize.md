# FontSize

## Introduction

FontSize is used to adjust the size of the font used by the TextRuntime. Your particular platform may require fonts to be created as .fnt/.png pairs to be loaded, while other platforms can create fonts dynamically.

MonoGame, KNI, and FNA projects can dynamically create fonts at runtime by installing a KernSmith NuGet package. Without KernSmith, these platforms require fonts to be pre-created as .fnt/.png pairs, usually in FontCache.

Skia platforms can dynamically create fonts out of the box with no additional setup.

For more information on font setup, see the [Fonts](fonts.md) page.

## Example - Setting FontSize

The following code shows how to assign FontSize. Note that the effective font produced is a combination of the current values of other font values.

```csharp
// Initialize
MyTextRuntime.FontSize = 18;
```
