# TextureAddress

## Introduction

TextureAddress controls whether a Sprite should display its entire SourceFile or only a portion. By default this value is `Gum.Managers.TextureAddress.EntireTexture`.



## Example - Assigning Texture Address

The following code can be used to display a 32x32 portion of the texture beginning beginning at the top-left of the source file.

```csharp
sprite.TextureAddress = 
    Gum.Managers.TextureAddress.Custom;
sprite.TextureLeft = 0;
sprite.TextureTop = 0;
sprite.TextureWidth = 32;
sprite.TextureHeight = 32;
```
