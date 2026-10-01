# Migrating to 2026 October

## Introduction

This page discusses breaking changes and other considerations when migrating from `2026 September` to `2026 October`.

## What Changed at a Glance

`2026 October` is the first release of the rebuilt Gum tool, which now runs on Windows, macOS, and Linux. It opens the same project files as the old tool. Plugins written for the old Windows-only tool no longer load, so a plugin you installed yourself needs an updated build from its author.

On the runtime side, this release seals the built-in content loaders, so you can no longer derive a class from one. This is a **hard break**, but it reaches you only if you inherited from a built-in loader, which never let you change how loading works in the first place. Implementing `IContentLoader` yourself, the supported way to customize loading, is unchanged.

`TextBox` and `PasswordBox` now raise the `KeyDown` event every other Forms control uses, so the handler you pass must be a `KeyEventHandler`. Lambdas and methods keep compiling without changes.

Two smaller behavior changes can also affect runtime code. raylib now throws when a file is missing, like MonoGame, KNI, and FNA already did. `GetFrameworkElement` now returns `null` when no control matches instead of throwing.

## Upgrading the Gum Tool

The download is now a separate file for each OS, and `Gum.zip` is gone.

{% tabs %}
{% tab title="Windows" %}
To upgrade the Gum tool:

1. Download `Gum-win-x64.zip` from the [October 1, 2026 release on GitHub](https://github.com/vchelaru/Gum/releases/tag/Release_October_01_2026)
2. Delete the old tool from your machine
3. Unzip the new tool to the same location so your file associations keep working
{% endtab %}

{% tab title="macOS" %}
Download `Gum-osx-arm64.tar.xz` (Apple Silicon) or `Gum-osx-x64.tar.xz` (Intel) from the [October 1, 2026 release on GitHub](https://github.com/vchelaru/Gum/releases/tag/Release_October_01_2026), then follow the macOS steps in [Setup](../setup/README.md).
{% endtab %}

{% tab title="Linux" %}
Download `Gum-linux-x64.tar.xz` from the [October 1, 2026 release on GitHub](https://github.com/vchelaru/Gum/releases/tag/Release_October_01_2026), then follow the Linux steps in [Setup](../setup/README.md).
{% endtab %}
{% endtabs %}

The [September 2, 2026 release](https://github.com/vchelaru/Gum/releases/tag/Release_September_02_2026) is the last release of the old Windows-only tool, and it stays available for download.

## Upgrading the Runtime

The `2026 October` runtime ships as NuGet version **`2026.10.1.1`**. Upgrade your Gum NuGet packages to this version. For more information, see the NuGet packages for your particular platform:

* MonoGame - [https://www.nuget.org/packages/Gum.MonoGame/](https://www.nuget.org/packages/Gum.MonoGame/)
* KNI - [https://www.nuget.org/packages/Gum.KNI/](https://www.nuget.org/packages/Gum.KNI/)
* FNA - [https://www.nuget.org/packages/Gum.FNA/](https://www.nuget.org/packages/Gum.FNA/)
* raylib - [https://www.nuget.org/packages/Gum.raylib](https://www.nuget.org/packages/Gum.raylib)
* .NET MAUI - [https://www.nuget.org/packages/Gum.SkiaSharp.Maui](https://www.nuget.org/packages/Gum.SkiaSharp.Maui)
* SkiaSharp - [https://www.nuget.org/packages/Gum.SkiaSharp/](https://www.nuget.org/packages/Gum.SkiaSharp/)

If using GumCommon directly, you can update the GumCommon NuGet:

* GumCommon - [https://www.nuget.org/packages/FlatRedBall.GumCommon](https://www.nuget.org/packages/FlatRedBall.GumCommon)

## Breaking Changes and Migrations

### Plugins Built for the WPF Tool No Longer Load

The Gum tool no longer contains WPF or Windows Forms, so it skips any plugin assembly that references them. The **Plugins** dialog lists a skipped plugin as not supported, and the **Output** tab names the WPF assembly it referenced.

This affects you only if you installed or wrote a plugin outside the Gum repository. Every plugin that ships with Gum has already moved over.

To migrate a plugin you maintain, replace its WPF menu items, tabs, and dialogs with the framework-neutral equivalents and retarget it to `net10.0`. [Migrating a WPF plugin](../plugins/README.md#migrating-a-wpf-plugin) walks through each change.

### Built-in Content Loaders Are Sealed

Each backend's built-in content loader is now `sealed`:

* `RenderingLibrary.Content.ContentLoader` on MonoGame, KNI, and FNA
* `RenderingLibrary.Content.ContentLoader` on raylib
* `SkiaGum.Content.EmbeddedResourceContentLoader` on SkiaSharp

This affects you only if you wrote a class that derives from one of them. Customizing loading by implementing `IContentLoader` yourself is the supported approach and is unchanged, as is setting `LoaderManager.Self.ContentLoader`.

None of these loaders ever had a `virtual` method, so a derived class could not actually change how loading works. Hiding `LoadContent` with the `new` keyword compiled, but Gum calls the loader through the `IContentLoader` interface, so the hidden method never ran. Reimplementing the interface explicitly did run, and is the same thing as wrapping, with an unused base class attached.

To migrate, implement `IContentLoader` and hold the built-in loader in a field instead of inheriting from it.

❌ Old:

```csharp
// Class scope
public class MyContentLoader : RenderingLibrary.Content.ContentLoader
{
    public new T LoadContent<T>(string contentName)
    {
        // This never ran, because Gum calls through IContentLoader.
        return base.LoadContent<T>(contentName);
    }
}
```

✅ New:

```csharp
// Class scope
public class MyContentLoader : RenderingLibrary.Content.IContentLoader
{
    RenderingLibrary.Content.IContentLoader _defaultLoader;

    public MyContentLoader(RenderingLibrary.Content.IContentLoader defaultLoader)
    {
        _defaultLoader = defaultLoader;
    }

    public T LoadContent<T>(string contentName)
    {
        // Your own loading goes here.

        return _defaultLoader.LoadContent<T>(contentName);
    }

    public T TryLoadContent<T>(string contentName)
    {
        return _defaultLoader.TryLoadContent<T>(contentName);
    }
}
```

Pass the built-in loader in when you install yours, so the content names you do not handle keep loading and caching as before:

```csharp
// Initialize
var loaderManager = RenderingLibrary.Content.LoaderManager.Self;
loaderManager.ContentLoader = new MyContentLoader(loaderManager.ContentLoader);
```

For the full explanation, including what your loader receives and how caching works, see [File Loading](../../code/files-and-fonts/file-loading.md).

### TextBox and PasswordBox KeyDown Uses KeyEventHandler

`TextBox` and `PasswordBox` used to declare their own `KeyDown` event, typed `Action<object, KeyEventArgs>`, which hid `FrameworkElement.KeyDown`. A handler added through a `FrameworkElement` reference never ran for a text box. The text box now raises `FrameworkElement.KeyDown`, typed `KeyEventHandler`, like `Button` and `Slider` do.

A lambda or a method passed to `+=` compiles unchanged, since both event types take the same `(object, KeyEventArgs)` parameters. Only a handler stored in a variable typed `Action<object, KeyEventArgs>` stops compiling. Change the variable's type to `KeyEventHandler`.

❌ Old:

```csharp
// Initialize
System.Action<object, KeyEventArgs> handler = (sender, args) => { /* ... */ };
textBox.KeyDown += handler;
```

✅ New:

```csharp
// Initialize
KeyEventHandler handler = (sender, args) => { /* ... */ };
textBox.KeyDown += handler;
```

### raylib Throws When a File Is Missing

raylib now throws an exception when Gum cannot find a file, such as a texture or font, which MonoGame, KNI, and FNA already did. Before, raylib skipped the missing file silently.

This affects you only on raylib, and only if your project references a file that does not exist. Fixing the path or adding the file is the usual fix.

To keep the old behavior, set `MissingFileBehavior` after Gum initializes:

```csharp
// Initialize
Gum.Wireframe.GraphicalUiElement.MissingFileBehavior =
    Gum.Wireframe.MissingFileBehavior.ConsumeSilently;
```

### GetFrameworkElement Returns Null When Nothing Matches

`GetFrameworkElement` and `GetFrameworkElement<T>` now return `null` when no control has the given name. They used to throw an `ArgumentException`. A control with the right name but the wrong type is now skipped, and the search continues to the next match instead of throwing.

This affects you only if your code catches that exception to detect a missing control. Check for `null` instead:

❌ Old:

```csharp
// Initialize
try
{
    var button = menu.GetFrameworkElement<Button>("StartButton");
    button.Click += (_, _) => StartGame();
}
catch (System.ArgumentException)
{
    // StartButton is not in this menu
}
```

✅ New:

```csharp
// Initialize
var button = menu.GetFrameworkElement<Button>("StartButton");
if (button != null)
{
    button.Click += (_, _) => StartGame();
}
```
