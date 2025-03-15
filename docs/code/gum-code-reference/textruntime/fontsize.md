# FontSize

## Introduction

FontSize is used to adjust the size of the font used by the TextRuntime. Your particular platform may require fonts to be created as .fnt/.png pairs to be loaded, while other platforms can create fonts dynamically.

The following platforms require fonts to be created before, usually in FontCache:

* FlatRedBall
* MonoGame / Kni / FNA

The following platforms can dynamically create fonts:

* Skia
* Silk.NET
* raylib

## Example - Setting FontSize

The following code shows how to assign FontSize. Note that the effective font produced is a combination of the current values of other font values.

```csharp
MyTextRuntime.FontSize = 18;
```
