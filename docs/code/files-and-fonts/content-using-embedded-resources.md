# Content Using Embedded Resources

## Introduction

While some platforms (such as Windows) provide direct access to the file system, access to files on platforms like iOS and Android is limited.

Content loading can be performed using embedded resources to standardize file access across all platforms. This section discusses how to use embedded resources in SkiaGum.

## Embedded Resource Setup

Before loading a resource, SkiaGum projects must first be set up to load from embedded resource. Gum must know two pieces of information before content can be loaded:

1. Which project contains the embedded resources to be loaded
2. How to modify a resource path (optional, but required if using generated code)

Before performing this initialization, you must decide where you will store your content. For example, consider a project which stores all of its SVG files in a `GumProject/Resources`  folder.

<figure><img src="../../.gitbook/assets/19_06 01 46.png" alt=""><figcaption><p>Resources folder in a GumProject folder</p></figcaption></figure>

{% hint style="info" %}
If you are using a Gum (.gumx) project, adding resource files relative to the Gum project is recommended since it keeps your project portable.
{% endhint %}

When using generated code, Gum generates all file loads relative to the .gumx location, so we need to tell the Gum runtime to use this as its relative path.

To do this, add the following code to where you initialize your code project, such as the CreateMauiApp function:

```csharp
SkiaResourceManager.CustomResourceAssembly = typeof(MauiProgram).Assembly;
SkiaResourceManager.AdjustContentName = (contentName) =>
{
    return "MauiSkiaGum.GumProject." + contentName;
};
```

Notice that resource loading uses the period separator for paths, so the adjusted content name should take form of `YourProjectName.Subfolder.Subfolder2.AdditionalSubFolder`.

## Marking Files as Embedded Resource

To mark your files as embedded resource in Visual Studio:

1. Right-click on a file and select Properties
2. Change the `Build Action` to `Embedded resource` in the Properties tab.

<figure><img src="../../.gitbook/assets/19_06 19 19.png" alt=""><figcaption></figcaption></figure>

For simpler maintenace, this can be changed to a wildcard in the .csproj file:

```xml
<ItemGroup>
  <EmbeddedResource Include="GumProject\Resources\**\*" />
</ItemGroup>
```

## Generated Code Content Loading

If embedded resource content loading is set up correctly, then generated code will correctly load files.

Note that generated code does not assume embedded resource content loading. For example, the following block of generated code shows how an SVG SourceFile is assigned:

```csharp
this.SvgInstance.SourceFile = @"Resources\gum-logo-reverse.svg";
```

Notice that in this case the source file uses a slash separator, but embedded resources internally use a period character for separators. Internally Gum automatically handles this difference so no changes are needed in generated code.

