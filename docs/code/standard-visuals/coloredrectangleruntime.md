# ColoredRectangleRuntime

{% hint style="info" %}
ColoredRectangleRuntime is the legacy solid-color rectangle. New Gum projects (version 3 and later) seed the **Rectangle** standard instead, which is backed by [RectangleRuntime](rectangleruntime.md). RectangleRuntime renders fill, outline (stroke), gradients, and drop shadows, so prefer it for new code. ColoredRectangleRuntime remains fully supported for existing projects.
{% endhint %}

### Introduction

The ColoredRectangleRuntime is a visual object which displays a solid-color rectangle. It can be used to display solid colors, but can also display gradients.

ColoredRectangleRuntime is often used as a colored background or a health bar.

### Code Example

The following code creates a ColoredRectangleRuntime:

```csharp
var coloredRectangleRuntime = new ColoredRectangleRuntime();
coloredRectangleRuntime.Color = Color.White;
coloredRectangleRuntime.Width = 100;
coloredRectangleRuntime.Height = 100;
coloredRectangleRuntime.AddToManagers();
```

The output is a white rectangle:

### Gradient

ColoredRectangleRuntime can display gradients. For information on gradients see the [ColoredRectangle gradient page](../../gum-tool/gum-elements/coloredrectangle/gradient.md).

The output is a gradient rectangle:
