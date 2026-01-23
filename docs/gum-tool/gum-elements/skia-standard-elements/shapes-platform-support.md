# Shapes Platform Support

## Introduction

Shapes (Skia) support varies per platform. See below to see information about your platform.

{% tabs %}
{% tab title="MonoGame/KNI" %}
MonoGame and KNI projects can use the following shapes:

* Arc
* ColoredCircle
* RoundedRectangle

Other types, such as SVG or Lottie, are not currently supported.

These shapes are rendered using the Apos.Shapes library.

For information on adding Apos.Shapes to your library, see the [Shapes (Apos.Shapes)](../../../code/standard-visuals/shapes-apos.shapes.md) page.
{% endtab %}

{% tab title="FNA" %}
Apos.Shapes may work on FNA but has not been thoroughly tested. We are looking for contributors to help test this.
{% endtab %}

{% tab title="Raylib" %}
Shapes are not yet implemented in raylib. Please create an issue on GitHub or contact us on Discord if you would like to see this implemented.
{% endtab %}

{% tab title="Skia platforms (Maui, WPF, Silk.NET)" %}
All shape types are fully supported.
{% endtab %}
{% endtabs %}
