# File Loading

### Introduction

A typical Gum project references many file types. Aside from the XML files created by the Gum tool (such as a .gumx file), a typical Gum project also references .png files and .fnt files.

Files which are referenced by your Gum project (as created in the Gum UI tool) automatically load their necessary dependencies assuming the files are part of the built file system.

Files can also be loaded and referenced at runtime for additional flexibility.

### Files in Gum Projects

When a file is added to a Gum project, the Gum UI tool checks the location of the file. If the file is not relative to the Gum project file (.gumx), the Gum UI tool warns you about the file being located outside of the project's folder. The tool recommends that the file should be copied so that your project remains portable.

If all of your project files are located relative to the .gumx root project file, then your project should be portable, and all referenced files will be automatically resolved for you when instantiating Screens and Components from your Gum project.

The Gum runtime library performs all of its loading from-file, so all of your files must be present in the destination directory. As explained in the [Loading .gumx](loading-.gumx-gum-project.md) page, all of your files should be set to **Copy if newer** in Visual Studio.

<figure><img src="../.gitbook/assets/image (4).png" alt=""><figcaption><p>bear.png file set to Copy if newer</p></figcaption></figure>

### Loading Files Through Runtime Objects

Gum runtime objects can reference files. For example both SpriteRuntime and NineSliceRuntime can reference a Texture2D. Similarly, the TextRuntime type can reference BitmapFonts which are loaded from .fnt and .png files.

All runtime types support the assignment of these files by direct assignment of their appropriate type, or by string name.&#x20;

For example, a Sprite's texture could be assigned either through the SourceFileName property or the Texture property:

```csharp
SpriteRuntimeInstance.SourceFile = "bear.png";
// or
SpriteRuntimeInstance.Texture = MyTexture;
```

In the case of the SourceFile assignment, the SpriteRuntime loads the Texture2D from disk. By default the file is loaded relative to the Content folder.

### Setting FileManager.RelativeDirectory

Whenever a file is assigned on a runtime object, Gum looks for the file in the `ToolsUtilities.FileManager.RelativeDirectory` directory. This directory defaults to your game's Content folder, and in most cases it should not be changed. The most common case for changing the RelativeDirectory property is when loading a file which in turn has its own relative files. In this case you should keep track of the relative directory before you change it. Once you are finished loading, you should revert it.

For example, your code might look like the following snippet:

```csharp
var oldRelativeDirectory = ToolsUtilities.FileManager.RelativeDirectory;
currentDirectory = "c:/Folder/YourDesiredFolder/";
// perform loading...
ToolsUtilities.FileManager.RelativeDirectory = oldRelativeDirectory;
```
