# Shapes Platform Support

## Introduction

Shapes (Skia) support varies per platform. See below to see information about your platform.

{% tabs %}
{% tab title="MonoGame/KNI" %}
MonoGame and KNI projects can use the following shapes:

* Arc
* Circle
* Rectangle

The outline (stroke) of Circle and Rectangle renders out of the box. The fill and the richer effects — gradients, drop shadows, and dashed strokes — are provided by the shape support package (`Gum.Shapes.MonoGame` or `Gum.Shapes.KNI`). Without the package those properties are saved but do not draw.

Other types, such as SVG or Lottie, are not currently supported.

For information on adding the shape support package to your project, see the [Shapes](../../../code/standard-visuals/shapes-apos.shapes.md) page.

{% hint style="info" %}
The older ColoredCircle, RoundedRectangle, and SolidRectangle shapes are being phased out in favor of Circle and Rectangle. They remain available so existing projects keep working, but will be removed in a future release.
{% endhint %}
{% endtab %}

{% tab title="FNA" %}
FNA renders the outline (stroke) of Circle and Rectangle only. The fill and the richer effects (gradients, drop shadows, dashed strokes) are not available on FNA because there is no shape support package for it. We are looking for contributors to help expand FNA shape support.
{% endtab %}

{% tab title="Raylib" %}
Raylib supports Circle and Rectangle — including fill, gradients, and drop shadows — natively, with no extra package required.

{% hint style="info" %}
Gradient-on-outline (a gradient applied to the stroke rather than the fill) is not yet implemented on raylib.
{% endhint %}
{% endtab %}

{% tab title="Skia platforms (Maui, WPF, Silk.NET)" %}
All shape types are fully supported.
{% endtab %}
{% endtabs %}
