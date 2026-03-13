# TextureHeight

## Introduction

TextureHeight controls the portion of the SpriteRuntime's Texture which is displayed. This value is measured in pixels. This value is only applied if a SpriteRuntime's TextureAddress is set to `Custom` or `DimensionsBased`.

## Example - Assigning TextureHeight

The following code assigns a SpriteRuntime's TextureHeight:

```csharp
// Initialize
// Assuming a Sprite's TextureAddress is set to Custom
sprite.TextureHeight = 64;
```
