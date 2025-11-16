# Loading a Gum Project (.gumx)

## Introduction

Gum projects can be loaded in a game project. Gum projects are made up of multiple files including:

* .gumx - the main Gum project
* .gusx - Gum screen files
* .gucx - Gum component files
* .gutx - Gum standard element files
* .png - image files
* .fnt - font files

{% hint style="info" %}
You are not required to use the Gum tool or .gumx projects - you are free to do everything in code if you prefer. Of course using the Gum tool can make it much easier to iterate quickly and experiment.
{% endhint %}

## Creating a Gum Project

Before creating a Gum project, it is recommended that you already have a functional Game project. Next, you'll need to save your Gum project:

1. Open the Gum tool
2. Select File->New Project
3. Navigate to the desired location for your project. See below for recommended locations:

{% tabs %}
{% tab title="MonoGame/KNI/FNA" %}
Create a folder inside of your game's Content folder, such as `Content/GumProject`, then save the file in the newly-created folder.
{% endtab %}

{% tab title="raylib" %}
Create a folder inside of your game's resources folder, such as `resources/GumProject`, then save the file in the newly-created folder.
{% endtab %}

{% tab title=".NET MAUI" %}
Create a folder inside of your project's folder, such as `GumProject`, then save the file in the newly-created folder.
{% endtab %}
{% endtabs %}

It's best to put your Gum project in a folder that is not shared with any other content that it stays organized from the rest of your content files. Remember, Gum creates lots of files.

## Adding the Gum Project to your .csproj

To add the Gum files to your csproj:

1. Open your .csproj in a text editor
2. Add a line to copy all files in the Gum project folder including the .gumx file itself. For example, your .csproj might look this (see tabs below)

{% tabs %}
{% tab title="MonoGame/KNI/FNA Desktop" %}
```xml
<ItemGroup>
    <None Update="Content\GumProject\**\*.*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

{% hint style="info" %}
If you are using the Contentless project ([https://github.com/Ellpeck/Contentless](https://github.com/Ellpeck/Contentless)) , you need to explicitly exclude Gum and all of its files by adding and modifying `Content/Contentless.json` .
{% endhint %}


{% endtab %}

{% tab title="MonoGame/KNI Android" %}
```xml
<AndroidAsset Include="Content\GumProject\**\*.*" />
```
{% endtab %}

{% tab title="raylib" %}
```xml
<ItemGroup>
    <None Update="resources\GumProject\**\*.*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```
{% endtab %}

{% tab title=".NET MAUI" %}
.NET MAUI projects do not currently reference Gum projects so the file does not need to be added ot the game project. Currently .NET MAUI projects must use full code generation to reference a Gum project.
{% endtab %}
{% endtabs %}

For more information about wildcard support in .csproj files, see this page on how to include wildcards in your .csproj:

[https://learn.microsoft.com/en-us/visualstudio/msbuild/how-to-select-the-files-to-build?view=vs-2022#specify-inputs-with-wildcards](https://learn.microsoft.com/en-us/visualstudio/msbuild/how-to-select-the-files-to-build?view=vs-2022#specify-inputs-with-wildcards)

## Loading a Gum Project

To load a Gum Project:

1. Make sure that your Gum project has at least one Screen
2. Open your file that has your Gum initialization code, such as Game1.cs or Project.cs
3. Modify the Initialize method by passing it a Gum project file path

{% tabs %}
{% tab title="MonoGame/KNI/FNA" %}
```csharp
protected override void Initialize()
{
    GumUI.Initialize(
        this, 
        "GumProject/GumProject.gumx");

    // This assumes that your project has at least 1 screen
    var screen = ObjectFinder.Self.GumProjectSave.Screens
        .FirstOrDefault()
        ?.ToGraphicalUiElement();
        
    if(screen == null)
    {
        throw new Exception(
            "No screen found in the Gum project, " + 
            "did you add a Screen in the Gum tool?");
    }
            
    screenRuntime.AddToRoot();
    
    base.Initialize();
}
```

By default the Gum path is relative to your game's Content folder.&#x20;

If your Gum project is not part of the the folder you can still load it by using the "../" prefix to step out of the Content folder. For example, the following code would load a Gum project located at `<exe location>/GumProject/GumProject.gumx`:

```csharp
GumUI.Initialize(
    this, "../GumProject/GumProject.gumx");
```
{% endtab %}

{% tab title="raylib" %}
```csharp
GumUI.Initialize(
    "resources/GumProject/raylibGumProject.gumx");

var screen = ObjectFinder.Self.GumProjectSave.Screens
    .FirstOrDefault()
    ?.ToGraphicalUiElement();

if(screen == null)
{
    throw new Exception(
        "No screen found in the Gum project, " + 
        "did you add a Screen in the Gum tool?");
}
screen.AddToRoot();
```
{% endtab %}

{% tab title=".NET MAUI" %}
.NET MAUI projects do not currently support loading .gumx projects.
{% endtab %}
{% endtabs %}

The code above loads the Gum project using the desired file path, such as `"GumProject/GumProject.gumx"`.

## ToGraphicalUiElement

Once a Gum project is loaded, all of its screens and components can be accessed through the `ObjectFinder.Self.GumProjectSave` property. Any screen or component can be converted to a GraphicalUiElement, which is the visual object that displays in game.

The code in the previous section creates a `GraphicalUiElement` from the first screen in the project.

For an example of a Game1.cs file which loads a project file, see the MonoGameGumFromFile: [https://github.com/vchelaru/Gum/blob/0e266942560e585359f019ac090a6c1010621c0b/Samples/MonoGameGumFromFile/MonoGameGumFromFile/Game1.cs#L76-L82](https://github.com/vchelaru/Gum/blob/0e266942560e585359f019ac090a6c1010621c0b/Samples/MonoGameGumFromFile/MonoGameGumFromFile/Game1.cs#L76-L82)

Note that calling ToGraphicalUiElement creates a [GraphicalUiElement](../../gum-code-reference/graphicaluielement/) (Gum object) from the first screen. You can access any screen in the the Gum project if your project has multiple Screens.

You can get a reference to elements within the screen by calling `GetGraphicalUiElementByName`, as shown in the following code:

```csharp
// Load the gum project (see code above)
var screenRuntime = ObjectFinder.Self.GumProject.Screens
    .First()
    .ToGraphicalUiElement();
screenRuntime.AddToRoot();

// Items in the screen can be accessed using the GetGraphicalUiElementByName method:
var child = screenRuntime .GetGraphicalUiElementByName("TitleInstance");

// All GraphicalUiElements have common properties, like X:
child.X += 30;

// you can also set properties which may not be common to all GraphicalUiElements,
// like Text:
child.SetProperty("Text", "Hello world");
```
