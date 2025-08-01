# Migrating to 2025 May 28

## Introduction

This page discusses breaking changes when migrating from `2025 April 27` to `2025 May 28`

## Upgrading Gum Tool

To upgrade the Gum tool:

1. Download Gum.zip from the release on Github: [https://github.com/vchelaru/Gum/releases/tag/Release\_May\_28\_2025](https://github.com/vchelaru/Gum/releases/tag/Release_May_28_2025)
2. Delete the old tool from your machine
3. Unzip the gum tool to the same location as to not break any file associations

## Upgrading Runtime

Upgrade your Gum NuGet packages to version 2025.5.28.1. For more information, see the NuGet packages for your particular platform:

* MonoGame - [https://www.nuget.org/packages/Gum.MonoGame/](https://www.nuget.org/packages/Gum.MonoGame/)
* KNI - [https://www.nuget.org/packages/Gum.KNI/](https://www.nuget.org/packages/Gum.KNI/)
* FNA - [https://www.nuget.org/packages/Gum.FNA/](https://www.nuget.org/packages/Gum.FNA/)
* SkiaSharp - [https://www.nuget.org/packages/Gum.SkiaSharp/](https://www.nuget.org/packages/Gum.SkiaSharp/)

For other platforms you need to build Gum from source

See below for breaking changes and updates.

## TextRuntime Default WidthUnits and HeightUnits

This version changes the following:

<table data-full-width="false"><thead><tr><th width="240">Variable</th><th width="181">Old Value</th><th>New Value</th></tr></thead><tbody><tr><td>TextRuntime.Height</td><td>50</td><td>0</td></tr><tr><td>TextRuntime.HeightUnits</td><td>DimensionUnitType.Absolute</td><td>DimensionUnitType.RelativeToChildren</td></tr><tr><td>TextRuntime.Width</td><td>100</td><td>0</td></tr><tr><td>TextRuntime.WidthUnits</td><td>DimensionUnitType.Absolute</td><td>DimensionUnitType.RelativeToChildren</td></tr></tbody></table>

This change only changes how code works, and does not affect the tool, nor does it affect projcts which are loading .gumx files.

This change was made to address the confusion of TextRuntime instances wrapping at what seemed like arbitrary points.

The following code can be used to see the difference:

```csharp
var text = new TextRuntime();
text.Text = "I am displaying a long string without wrapping";
mainPanel.AddChild(text);
```

<figure><img src="../../.gitbook/assets/15_06 42 09.png" alt=""><figcaption><p>New default TextRuntime behavior</p></figcaption></figure>

<figure><img src="../../.gitbook/assets/image (1).png" alt=""><figcaption><p>Old default TextRuntime behavior</p></figcaption></figure>

Note that Height and HeightUnits have also changed so that TextRuntimes now automatically adjust their heights. This allows TextRuntimes to properly stack by default, as shown in the following code:

```csharp
for(int i = 0; i < 5; i++)
{
    var text = new TextRuntime();
    text.Text = "Text " + i;
    mainPanel.AddChild(text);
}
```

<figure><img src="../../.gitbook/assets/15_06 45 04.png" alt=""><figcaption><p>New default TextRuntime behavior in a stack</p></figcaption></figure>

<figure><img src="../../.gitbook/assets/15_06 46 52.png" alt=""><figcaption><p>Old default TextRuntime behavior in a stack</p></figcaption></figure>

This change only modifies the default variables, it does not change the behavior of TextRuntime if Height, HeightUnits, Width, and WidthUnits are explicitly set. In other words, the old behavior can be obtained by setting the values as shown in the following code:

```csharp
var textWithOldBehavior = new TextRuntime();
textWithOldBehavior.Text = "Text with old behavior";
textWithOldBehavior.Width = 100;
textWithOldBehavior.WidthUnits = DimensionUnitType.RelativeToChildren;
textWithOldBehavior.Height = 50;
textWithOldBehavior.HeightUnits = DimensionUnitType.Absolute;
```

## Only First Stacked Item Applies X Units and Y Units

This version changes how `X Units` are applied to children in a container with its `Children Layout` set to `Left to Right Stack` . It also changes how `Y Units` are applied to children in a container with its `Children Layout` set to `Left to Right Stack`. This changes how Gum behaves both in the tool and all runtimes.

This change only applies children after the first item in a stack. All subsequent items in a stack ignore the their units values.

This change was made to address the confusion of copying/pasting a child which is not positioned at the top left resulting in overlapping children.

For example, consider a container with two Text instances. The container uses a `Children Layout` of `Top to Bottom Stack`. In this example, both Text instances have their `Y Unit` value set to `Center`. Notice that the first Text instance is positioned relative to the vertical center of its parent, and the second Text instance is positioned under.

<figure><img src="../../.gitbook/assets/15_07 05 12.png" alt=""><figcaption><p>New stacking behavior</p></figcaption></figure>

Previously, the 2nd Text would ignore the stacking behavior since it had a `Y Units` set to `Center`, forcing it to be positioned relative to its parent's vertical center.

<figure><img src="../../.gitbook/assets/15_07 07 46.png" alt=""><figcaption><p>Old stacking behavior</p></figcaption></figure>

To continue using the old behavior, set the parent's `Children Layout` to `Regular`.

<figure><img src="../../.gitbook/assets/15_07 11 09.gif" alt=""><figcaption><p>Old behavior can be obtained by setting Children Layout to Regular</p></figcaption></figure>

