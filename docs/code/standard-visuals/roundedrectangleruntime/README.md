# RoundedRectangleRuntime

## Introduction

RoundedRectangleRuntime can be used to draw a solid (filled) or outlined rectangle of any color. As the name suggests, it supports rounded corners.

{% hint style="warning" %}
RoundedRectangleRuntime requires using SkiaGum. For more information on platform support, see the [SkiaGum Platform Support](broken-reference) page.
{% endhint %}

## Code Example

The following code creates a RoundedRectangleRuntime:

```csharp
var roundedRectangle = new RoundedRectangleRuntime();
MainStack.Children.Add(roundedRectangle);
roundedRectangle.Width = 100;
roundedRectangle.Height = 100;
roundedRectangle.CornerRadius = 20;
roundedRectangle.Color = SKColors.Blue;
```

<figure><img src="../../../.gitbook/assets/blueRect.png" alt=""><figcaption><p>Blue RoundedRectangleRuntime</p></figcaption></figure>
