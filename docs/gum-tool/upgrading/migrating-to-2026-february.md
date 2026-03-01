# Migrating to 2026 February

## Introduction

This page discusses breaking changes and other considerations when migrating from `2026 January` to `2026 February` . Note that due to last-minute bug fixes, the version was released on March 1.

## Upgrading Gum Tool

{% tabs %}
{% tab title="Windows" %}
To upgrade the Gum tool:

1. Download Gum.zip from the release on Github: [https://github.com/vchelaru/Gum/releases/tag/Release\_March\_01\_2026](https://github.com/vchelaru/Gum/releases/tag/Release_March_01_2026)
2. Delete the old tool from your machine
3. Unzip the gum tool to the same location as to not break any file associations
{% endtab %}

{% tab title="Linux" %}
To upgrade the Gum tool, it depends on your current version of gum.

1. Your Gum version < 2026
   1. Download the latest `setup_gum_linux.sh` script to your home directory [https://github.com/vchelaru/Gum/blob/master/setup\_gum\_linux.sh](../../../setup_gum_linux.sh)
   2. Make it executable `chmod +x ./setup_gum_linux.sh`
   3. Re-run the `./setup_gum_linux.sh` script:
      1. It will create a new folder/wine-prefix  `~/.wine_gum_dotnet8`&#x20;
      2. It will create a new  `~/bin/gum`  script with an  `upgrade`  option for future upgrades
   4. Errors and Resolutions:
      1. If you get an error about `gum wine prefix directory already exists` then you can either rename your old directory, or install gum to a different wine prefix with `./setup_gum_linux.sh ~/.my_wine_prefix`
2. Your Gum version >= 2026
   1. Run the upgrade
      1. `gum upgrade` or `~/bin/gum upgrade`
{% endtab %}
{% endtabs %}

## Upgrading Runtime

Upgrade your Gum NuGet packages to version **2026.3.1.1**. For more information, see the NuGet packages for your particular platform:

* MonoGame - [https://www.nuget.org/packages/Gum.MonoGame/](https://www.nuget.org/packages/Gum.MonoGame/)
* KNI - [https://www.nuget.org/packages/Gum.KNI/](https://www.nuget.org/packages/Gum.KNI/)
* FNA - [https://www.nuget.org/packages/Gum.FNA/](https://www.nuget.org/packages/Gum.FNA/)
* raylib - [https://www.nuget.org/packages/Gum.raylib](https://www.nuget.org/packages/Gum.raylib)
* .NET MAUI - [https://www.nuget.org/packages/Gum.SkiaSharp.Maui](https://www.nuget.org/packages/Gum.SkiaSharp.Maui)
* SkiaSharp - [https://www.nuget.org/packages/Gum.SkiaSharp/](https://www.nuget.org/packages/Gum.SkiaSharp/)

If using GumCommon directly, you can update the GumCommon NuGet:

* GumCommon - [https://www.nuget.org/packages/FlatRedBall.GumCommon](https://www.nuget.org/packages/FlatRedBall.GumCommon)

If using the Apos.Shapes library, update the library for your target platform:

* Gum.Shapes.MonoGame - [https://www.nuget.org/packages/Gum.Shapes.MonoGame](https://www.nuget.org/packages/Gum.Shapes.MonoGame)
* Gum.Shapes.KNI - [https://www.nuget.org/packages/Gum.Shapes.KNI](https://www.nuget.org/packages/Gum.Shapes.KNI)

For other platforms you need to build Gum from source

See below for breaking changes and updates.

## \[Breaking] Cursor Visual Interaction Uses Only HasEvents

Previous versions of the Gum runtime would interact with a visual if its `HasEvents` property was set to true and also if it has any events such as `Click` assigned. This behavior was confusing and did not respect the `HasEvents` property. Now, a visual will react to (and consume) events if its `HasEvents` property is set to true.

Most projects will not be affected by this; however, projects which explicitly set `HasEvents` to true on a visual will now have events consumed by that visual.

This is most likely a problem if a Standard Element in the Gum tool (such as NineSlice) has its Has Events variable set to true on the Standard Element itself, which makes this value true for all instances of Standard Element.

FlatRedBall continues to use the old behavior, so this change does not break FlatRedBall projects.

Furthermore, the old behavior can still be enabled by explicitly calling this code **after initializing Gum**.

```csharp
ICursor.VisualOverBehavior = VisualOverBehavior.OnlyIfEventsAreNotNullAndHasEventsIsTrue;
```

Note that this old behavior can cause confusion when working with visual elements so keeping the old behavior is not recommended.

Furthermore, the following runtimes now default to HasEvents set to false. Previously these were set to true by default:

* NineSliceRuntime
* PolygonRuntime
* TextRuntime

### \[Breaking] Gum UI Default Forms Controls

If your project is using the default Forms controls, or if you have created your own custom forms controls, you may need to make the following changes or clicks will not be registered:

#### PasswordBox

Change PasswordBox.ClipContainer Has Events to false.

<figure><img src="../../.gitbook/assets/11_08 07 20.png" alt=""><figcaption></figcaption></figure>

#### TextBox

Change TextBox.ClipContainer Has Events to false.

<figure><img src="../../.gitbook/assets/11_08 08 37.png" alt=""><figcaption></figcaption></figure>

