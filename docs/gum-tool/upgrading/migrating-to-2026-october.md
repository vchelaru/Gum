# Migrating to 2026 October

## Introduction

This page discusses breaking changes and other considerations when migrating from `2026 September` to `2026 October`.

## What Changed at a Glance

This release seals the built-in content loaders, so you can no longer derive a class from one. This is a **hard break**, but it reaches you only if you inherited from a built-in loader, which never let you change how loading works in the first place. Implementing `IContentLoader` yourself, the supported way to customize loading, is unchanged.

## Upgrading the Gum Tool

{% hint style="warning" %}
**Placeholder:** add the release link and the per-platform download steps for this release, plus the Avalonia cutover migration section required by `Direction/decisions/0018-external-wpf-plugins-break-at-avalonia-cutover.md`. See the `gum-monthly-release` skill.
{% endhint %}

## Upgrading the Runtime

This release's runtime ships as NuGet version **`PLACEHOLDER!!!! NuGet version`**. Upgrade your Gum NuGet packages to this version. For more information, see the NuGet packages for your particular platform:

* MonoGame - [https://www.nuget.org/packages/Gum.MonoGame/](https://www.nuget.org/packages/Gum.MonoGame/)
* KNI - [https://www.nuget.org/packages/Gum.KNI/](https://www.nuget.org/packages/Gum.KNI/)
* FNA - [https://www.nuget.org/packages/Gum.FNA/](https://www.nuget.org/packages/Gum.FNA/)
* raylib - [https://www.nuget.org/packages/Gum.raylib](https://www.nuget.org/packages/Gum.raylib)
* .NET MAUI - [https://www.nuget.org/packages/Gum.SkiaSharp.Maui](https://www.nuget.org/packages/Gum.SkiaSharp.Maui)
* SkiaSharp - [https://www.nuget.org/packages/Gum.SkiaSharp/](https://www.nuget.org/packages/Gum.SkiaSharp/)

If using GumCommon directly, you can update the GumCommon NuGet:

* GumCommon - [https://www.nuget.org/packages/FlatRedBall.GumCommon](https://www.nuget.org/packages/FlatRedBall.GumCommon)

## Breaking Changes and Migrations

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
