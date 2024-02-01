# TextRuntime

### Introduction

The TextRuntime object is used to draw strings to the screen. It supports a variety of options for rendering text including alignment, fonts, coloring, and line wrapping.

### Example - Creating a TextRuntime

To create a TextRuntime, instantiate it and add it to the managers as shown in the following code:

```csharp
var textInstance = new TextRuntime();
textInstance.Text = "Hello world";
textInstance.AddToManagers(SystemManagers.Default, null);
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
customText.AddToManagers(SystemManagers.Default, null);
```

This code assumes a font file named WhitePeaberryOutline.fnt is located in the `Content/WhitePeaberryOutline` folder. By default all Gum content loading is performed relative to the Content folder.

Note that .fnt files reference one or more image files, so the image file must also be added to the correct folder. In this case, the WhitePeaberryOutline.fnt file references a WhitePeaberryOutline.png file, so both files are in the same folder.

<figure><img src="../../.gitbook/assets/image (1) (1).png" alt=""><figcaption><p>WhitePeaberryOutline font in the Solution Explorer</p></figcaption></figure>

Also, note that files are loaded from-file rather than using the content pipeline. This means that extensions (such as .fnt) are included in the file path, and that both the .fnt and .png files must have their **Copy to Output Directory** value set to **Copy if newer**.

<figure><img src="../../.gitbook/assets/image (1) (1) (1).png" alt=""><figcaption><p>Copy if newer property set</p></figcaption></figure>

### Font Component Values

A TextRuntime's font can be controlled by its individual font component values. **Setting these values in code will not produce a .fnt file for you - the .fnt file must already be in your project**. The following values are used to determine the font (.fnt) to load:

* FontSize
* Font
* OutlineThickness
* UseFontSmoothing
* IsItalic
* IsBold

By default, all fonts will be of the format `Font{Font}{FontSize}.fnt`. Consider the following code:

```csharp
text.UseCustomFont = false;
text.Font = "Arial";
text.FontSize = 24;
```

This results in the TextRuntime object searching for a font named `FontArial24.fnt`.

As mentioned before, the default location for fonts is the Content folder, so the code would search for `Content/FontArial24.fnt`.

The following additional suffixes (in order listed below) are added to the font name.

* OutlineThicknes - if greater than 0, then the suffix `_o` followed by the outline thickness is added. For example, if OutlineThickness is 3, a font might be named `FontArial24_3.fnt`
* UseFontSmoothing - if false, then \_noSmooth is appended. For example `Font24_noSmooth.fnt`
* IsItalic - if true, then \_Italic is appended. For example `Font24_Italic.fnt`
* IsBold - if true, then \_Bold is appended. For example `Font24_Bold.fnt`

The BmfcSave.GetFontCacheFileNameFor method can be called with any combination to obtain the desired font value. For example, the following code coudl be used to determine the desired .fnt file:

```csharp
var desiredFntName = BmfcSave.GetFontCacheFileNameFor(
    18, // font size
    "Consolas", // font name
    2, // outline thickness
    true, // use font smoothing
    false, // is italic
    true // is bold
    );
```

Note that this method does not take into consideration the content folder.

### Creating Fonts

To create a .fnt file, you have a few options:

1. Open Gum, create a temporary Text instance with the desired properties, then look at the font cache folder
2. Use Angelcode Bitmap Font Generator. For more information see the [Use Custom Font page](../../gum-elements/text/use-custom-font.md).
3. Manually create a .fnt file in text and a corresponding .png. This is easiest if you create a .fnt file using one of the options above, then modify it.
