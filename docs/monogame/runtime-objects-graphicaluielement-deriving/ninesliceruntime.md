# NineSliceRuntime

### Introduction

The NineSliceRuntime object is used to draw a visual object which references a Texture2D, but which does not stretch the corner pieces of a texture, and which only stretches the edges between the corners along their axis. In other words, the NineSlice (sometimes also referred to as a sprite frame) is used to draw frames in UI which can stretch without introducing visual artifacts.

For more information about the NineSlice type, see the [NineSlice page](../../gum-elements/nineslice/).

### Code Example

The following code can be used to instantiate a NineSliceRuntime which uses a Texture (png file) named Frame.png.

```csharp
var nineSlice = new NineSliceRuntime();
nineSlice.SourceFileName = "Frame.png";
nineSlice.Width = 256;
nineSlice.Height = 48;
container.Children.Add(nineSlice);
```

<figure><img src="../../.gitbook/assets/image (3) (1) (1) (1) (1).png" alt=""><figcaption><p>NineSlice using Frame.png</p></figcaption></figure>

### TextureAddressMode and Texture Coordinates

By default a NineSlice uses it entire texture. This can be customized using texture coordinate and TextureAddress properies as shown in the following code:

```csharp
nineSlice.TextureLeft = 0;
nineSlice.TextureTop = 16;
nineSlice.TextureWidth = 32;
nineSlice.TextureHeight = 32;
nineSlice.TextureAddress = Gum.Managers.TextureAddress.Custom;
```

Note that if TextureAddress isn't set to Custom, then the four coordinate values are ignored and the entire texture is used.
