# SpriteRuntime

### Introduction

The SpriteRuntime object can be used to draw a Texture2D to screen. It supports drawing the entire texture or a portion. It also provides the ability to scale according to the source texture size and aspect ratio.

### Code Example

The following code can be used to instantiate a Sprite which uses a Texture (png file) named BearTexture.png.

{% hint style="info" %}
**NOTE:** The BeatTexture.png file must have the `Copy to Output Directory` set to either `Copy always` or `Copy if newer`. Otherwise you will get an error: System.IO.IOException 'Could not get the stream for the file C:\path\to\file\BearTexture.png'
{% endhint %}

```csharp
var sprite = new SpriteRuntime();
sprite.SourceFileName = "BearTexture.png";
container.Children.Add(sprite);
```

<figure><img src="../../../.gitbook/assets/BearTextureOverBlueBackground.png" alt=""><figcaption><p>BearTexture.png displayed by a SpriteRuntime</p></figcaption></figure>

### SpriteRuntime Texture

A SpriteRuntime's texture can be assigned using the name of the file or a direct Texture2D reference.

#### Assigning SourceFileName

The SourceFileName property is used to assign the SpriteRuntime's texture using a file name. The name of the file should (by default) be relative to the Content folder. For example, consider the following line:

```csharp
sprite.SourceFileName = "BearTexture.png";
```

This code assumes that the file is relative to the project's Content folder, as shown in the following screenshot:

<figure><img src="../../../.gitbook/assets/BearTexturePngInSolution.png" alt=""><figcaption><p>BearTexture.png in the Content folder</p></figcaption></figure>

For more information on working with files at runtime, see the [File Loading](../../files-and-fonts/file-loading.md) page.

Assigning Texture

If your game manages its own textures, you can assign a Texture on the SpriteRuntime through its Texture property as shown in the following code.

```csharp
// This code assumes that MyTexture is a valid Texture2D
sprite.Texture = MyTexture;
```

Note that assigning the SourceFileName property results in the Texture property referencing a texture if a valid texture is found.
