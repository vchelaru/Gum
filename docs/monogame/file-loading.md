# File Loading

### Introduction

A typical Gum project references many file types. Aside from the XML files created by the Gum tool (such as a .gumx file), a typical Gum project also references .png files and .fnt files.

Files which are referenced by your Gum project (as created in the Gum UI tool) automatically load their necessary dependencies assuming the files are part of the built file system. Typically this means that your project should copy all Gum XML, PNG, and FNT files to the output folder. Gum does not use the MonoGame content pipeline.

### Files in Gum Projects

When a file is added to a Gum project, the Gum UI tool checks the location of the file. If the file is not relative to the Gum project file (.gumx), the Gum UI tool warns you about the file being located outside of the project's folder. The tool recommends that the file should be copied so that your project remains portable.

If all of your project files are located relative to the .gumx root project file, then your project should be portable, and all referenced files will be automatically resolved for you when instantiating Screens and Components from your Gum project.

The Gum runtime library performs all of its loading from-file, so all of your files must be present in the destination directory. As explained in the [Loading .gumx](loading-.gumx-gum-project.md) page, all of your files should be set to **Copy if newer** in Visual Studio.

<figure><img src="../.gitbook/assets/image (4) (1) (1) (1) (1).png" alt=""><figcaption><p>bear.png file set to Copy if newer</p></figcaption></figure>

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

Whenever a file is assigned on a runtime object, Gum looks for the file in the `ToolsUtilities.FileManager.RelativeDirectory` directory. This directory defaults to your game's Content folder.

If your Gum project (.gumx) is located in the Content folder, then you should probably not change this value.

<figure><img src="../.gitbook/assets/image (44).png" alt=""><figcaption><p>GumProject.gumx located in the Content folder</p></figcaption></figure>

If your project is located in a subfolder of Content, then you must change the relative directory to be the location of your Gum project before you assign files. For example,  consider a situation where the Gum project is located in a folder called gum.

<figure><img src="../.gitbook/assets/image (45).png" alt=""><figcaption><p>Gum project in a subfolder</p></figcaption></figure>

To support this situation, you would want to set your relative directory as shown in the following code:

```csharp
ToolsUtilities.FileManager.RelativeDirectory = "content\\gum\\";

gumProject.Screens.First()
    .ToGraphicalUiElement(SystemManagers.Default, addToManagers:true);
```

Note that the following operations can result in files being loaded, so if your project is not located in the Content folder then you must set the RelativeDirectory before doing any of the following:

* Calling ToGraphicalUiElement
* Assigning SourceFileName
* Setting custom or cached fonts on a Text object
* Setting states (which may assign variables)

It's recommended practice to set the RelativeDirectory to your Gum project's location and to leave it there so you never have to consider subfolders in any code that accesses files directly or indirectly.

### File Caching

By default Gum caches loaded textures. In other words, the following code only results in a single file IO operation:

```csharp
Sprite1.SourceFile = "MyFile.png";
Sprite2.SourceFile = "MyFile.png";
```

File caching can be disabled by setting the LoaderManager's CacheTextures property to false as shown in the following code:

```csharp
LoaderManager.Self.CacheTextures = false;
```

Of course, doing so means that Gum will go to disk for every file which can increase load times and result in significantly more video memory usage.

Note that setting CacheTextures to false flushes the cache and disposes all cached content, so you can force the reload of all files by calling setting CacheTextures to false, then back to true as shown in the following code:

```csharp
Sprite1.SourceFile = "MyFile.png"; // This loads the file from disk
Sprite2.SourceFile = "MyFile.png"; // This uses the cached Texture2D

LoaderManager.Self.CacheTextures = false; // This clears the cache:
LoaderManager.Self.CacheTextures = true;
Sprite1.SourceFile = "MyFile.png"; // This once-again goes to disk to load the file
Sprite2.SourceFile = "MyFile.png"; // This uses the cached Texture2D 

LoaderManager.Self.CacheTextures = false; // This clears the cache
Sprite1.SourceFile = "MyFile.png"; // This once-again goes to disk to load the file
Sprite2.SourceFile = "MyFile.png"; // This also goes to disk to load the file
```

Be careful setting CacheTextures to false since all existing textures will be disposed. This means that if you have loaded textures which are still being referenced by runtime objects, you will get an exception if those are still being drawn after setting CacheTextures to false.
