# TextRuntime

### Introduction

The TextRuntime object is used to draw strings to the screen. It supports a variety of options for rendering text including alignment, fonts, coloring, and line wrapping.

### Example

To create a TextRuntime, instantiate it and add it to the managers as shown in the following code:

```csharp
var textInstance = new TextRuntime();
textInstance.Text = "Hello world";
text.AddToManagers(SystemManagers.Default, null);
```

### Fonts

By default all TextRuntime instances us an Arial 18 pt font. This can be changed by specifying ta custom font.

Fonts on TextRuntime objects can be modified in one of two ways:

1. By setting the UseCustomFont property to true, then changing the CustomFontFile property to a desired .fnt file.
2. By setting UseCustomFont property to false, then changing the individual Font values. This approach requires following a specific .fnt naming convention.

For most projects, the first approach is recommended since it doesn't require specific naming conventions. The second approach is convenient if your project is using the Gum tool, and if the Gum tool has already generated fonts for the specific combinations of values you are assigning.

The following code shows how to load a custom font:

```csharp
var customText = new TextRuntime();
customText.UseCustomFont = true;
customText.CustomFontFile = "WhitePeaberryOutline/WhitePeaberryOutline.fnt";
customText.Text = "Hello, I am using a custom font";
container.Children.Add(customText);
```

This code assumes a font file named WhitePeaberryOutline.fnt is located in the `Content/WhitePeaberryOutline` folder. By default all Gum content loading is performed relative to the Content folder.

Note that .fnt files reference one or more image files, so the image file must also be added to the correct folder. In this case, the WhitePeaberryOutline.fnt file references a WhitePeaberryOutline.png file, so both files are in the same folder.

<figure><img src="../../.gitbook/assets/image.png" alt=""><figcaption><p>WhitePeaberryOutline font in the Solution Explorer</p></figcaption></figure>

Also, note that files are loaded from-file rather than using the content pipeline. This means that extensions (such as .fnt) are included in the file path, and that both the .fnt and .png files must have their **Copy to Output Directory** value set to **Copy if newer**.

<figure><img src="../../.gitbook/assets/image (1).png" alt=""><figcaption><p>Copy if newer property set</p></figcaption></figure>

