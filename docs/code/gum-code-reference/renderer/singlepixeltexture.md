# SinglePixelTexture

### Introduction

The SinglePixelTexture is a reference to a Texture2D which is used to draw solid-colored Gum objects such as ColoredRectangle (renderable type SolidRectangle).

By default the SinglePixelTexture is constructed at runtime in-memory, but this can introduce a large number of rendering state changes when rendering multiple objects.

### Assigning SinglePixelTexture

For performance reasons, the single pixel texture should be a PNG which also has other art in your Gum project. Ideally your project should have just one large PNG which includes a single pixel texture, all fonts, and all other sprites used in Sprite and NineSlice instances.

To assign the SinglePixelTexture, you must access the Renderer. This can typically be accessed through the default SystemManagers as shown in the following code:

```csharp
// assuming MainTexture is a texture that contains 
// the single pixel
var renderer = SystemManagers.Default.Renderer;
renderer.SinglePixelTexture = MainTexture;
// The MainTexture should contain more than just the single pixel, so
// you probably need to assign the rectangle. This is in pixel coordiantes
// This assumes that x and y are valid x and y coordinates on your texture
renderer.SinglePixelSourceRectangle = new System.Drawing.Rectangle(
    x, 
    y,
    1,
    1
);
```
