# Custom Image Formats

### Introduction

Gum loads the image formats listed on [Source File](../../gum-tool/gum-elements/sprite/source-file.md) on its own. To load another format, such as WebP, add an image decoder. A decoder turns the file's bytes into pixels and Gum turns those pixels into a texture, so one decoder works on every runtime.

{% hint style="info" %}
Available in November 2026, or now if building Gum from source.
{% endhint %}

### Writing a Decoder

A decoder implements `IImageDecoder`. Gum calls `TryDecode` with the file name and a stream positioned at the start of the file:

* Return `false` when the file is not your format. Gum then asks the next decoder, and then uses its own loading.
* Return `true` with a `DecodedImage` when you decoded the file.
* Throw when the file is your format but is broken. The load fails with your exception, and no other decoder runs.

`DecodedImage` holds RGBA bytes (4 bytes per pixel, row by row), the width, the height, and whether the colors are already multiplied by alpha. Most decoders return straight (not premultiplied) alpha, which is the default.

The following decoder loads WebP files through SkiaSharp. It checks the file's header, so it declines any file that is not WebP even when the extension says otherwise:

```csharp
public class WebPDecoder : RenderingLibrary.Content.IImageDecoder
{
    public System.Collections.Generic.IEnumerable<string> SupportedExtensions => new[] { "webp" };

    public bool TryDecode(string fileName, System.IO.Stream stream,
        out RenderingLibrary.Content.DecodedImage image)
    {
        image = null;

        // A WebP file starts with "RIFF", four size bytes, then "WEBP".
        byte[] header = new byte[12];
        if (stream.Read(header, 0, 12) < 12
            || System.Text.Encoding.ASCII.GetString(header, 0, 4) != "RIFF"
            || System.Text.Encoding.ASCII.GetString(header, 8, 4) != "WEBP")
        {
            return false;
        }
        stream.Position = 0;

        using SkiaSharp.SKCodec codec = SkiaSharp.SKCodec.Create(stream)
            ?? throw new System.IO.InvalidDataException($"Could not decode {fileName}");
        var info = new SkiaSharp.SKImageInfo(codec.Info.Width, codec.Info.Height,
            SkiaSharp.SKColorType.Rgba8888, SkiaSharp.SKAlphaType.Unpremul);
        SkiaSharp.SKCodecResult result = codec.GetPixels(info, out byte[] rgba);
        if (result != SkiaSharp.SKCodecResult.Success)
        {
            throw new System.IO.InvalidDataException($"Could not decode {fileName}: {result}");
        }

        image = new RenderingLibrary.Content.DecodedImage(rgba, info.Width, info.Height);
        return true;
    }
}
```

`SupportedExtensions` is optional. The Gum tool uses it to accept `.webp` as a Sprite's Source File. Decoders without it still run on every file.

This decoder loads the first frame of an animated WebP file.

### Registering a Decoder

Add the decoder to `LoaderManager.Self.ImageDecoders` before Gum loads your project. Textures that load before the decoder is registered go through Gum's own loading and fail for formats it does not know.

```csharp
// Initialize
RenderingLibrary.Content.LoaderManager.Self.ImageDecoders.Add(new WebPDecoder());
GumUI.Initialize(this, "GumProject/GumProject.gumx");
```

Gum asks decoders in the order they were added, and `Insert(0, decoder)` puts a decoder first.

A decoded texture goes into Gum's texture cache under its file name, so each file is decoded once. Files loaded through a MonoGame `ContentManager` (XNB files) never reach decoders.

If no decoder handles a file whose format Gum does not load, the load fails with a message naming the extension and `LoaderManager.Self.ImageDecoders`.

### Premultiplied Alpha

Set `isPremultiplied` to `true` when the decoder returns colors already multiplied by alpha:

```csharp
// Initialize
image = new RenderingLibrary.Content.DecodedImage(rgba, width, height, isPremultiplied: true);
```

Gum then skips its own premultiply step. When the renderer draws straight alpha (`Renderer.UsePremultipliedAlpha` is `false`), Gum converts the pixels back to straight alpha and logs a warning, because the conversion loses precision for nearly transparent pixels. Return straight alpha when the library offers it.
