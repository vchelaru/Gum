# Skia Standard Elements

### Introduction

Skia standard elements are a collection of elements which use SkiaSharp for rendering. Skia standard elements provide additional types of visuals supported by Gum, but not all runtimes support Skia standard elements.

Skia adds advanced vector graphics support, including the **Arc** shape and vector file formats such as SVG and Lottie. The core **Circle** and **Rectangle** standard elements — which support fill, outline, gradients, drop shadows, dashed strokes, and (on Rectangle) rounded corners — also render at their richest on Skia.

{% hint style="info" %}
The older **ColoredCircle**, **RoundedRectangle**, and **SolidRectangle** Skia shapes are being phased out in favor of the core Circle and Rectangle standard elements. They remain available so existing projects keep working, but will be removed in a future release.
{% endhint %}

{% hint style="warning" %}
Using Skia Standard Elements may limit which platforms can run your Gum project. For more information, see the [Shapes Platform Support](shapes-platform-support.md) page.
{% endhint %}

### Enabling Skia Standard Elements

Skia standard elements must be explicitly added to gum projects. To add Skia standard elements Select **Plugins** -> **Add Skia Standard Elements.**

<figure><img src="../../../.gitbook/assets/image (2) (1) (2) (1).png" alt=""><figcaption><p>Add Skia Standard Elements</p></figcaption></figure>

After clicking this option, Gum adds new standard elements.

<figure><img src="../../../.gitbook/assets/image (1) (1) (1) (1) (2) (1) (1) (1) (1).png" alt=""><figcaption><p>Skia standard elements in Gum</p></figcaption></figure>

Once these Skia standard elements are added, they can be added to Screens and Components just like any other standard element.

<figure><img src="../../../.gitbook/assets/26_15 46 00.gif" alt=""><figcaption><p>Skia standard elements can be added just like any other standard element</p></figcaption></figure>
