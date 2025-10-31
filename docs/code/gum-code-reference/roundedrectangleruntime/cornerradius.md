# CornerRadius

## Introduction

CornerRadius controls a RoundedRectangleRuntime's corner radius. A value of 0 results in 90-degree angles at the corners. A larger value makes the corners more rounded.

## Assigning CornerRadius

CornerRadius can be assigned to set the radius of a RoundedRectangleRuntime. The following code assigns the CornerRadius to 20 pixels:

```csharp
var roundedRectangle = new RoundedRectangleRuntime();
MainStack.Children.Add(roundedRectangle);
roundedRectangle.Width = 100;
roundedRectangle.Height = 100;
roundedRectangle.Color = SKColors.Blue;
roundedRectangle.CornerRadius = 20;
```

<figure><img src="../../../.gitbook/assets/blueRect (1).png" alt=""><figcaption><p>CornerRadius of 20 pixels</p></figcaption></figure>

For information about the relationship between CornerRadius and the size of the RoundedRectangleRuntime, see the Gum Tool RoundedRectangle [Corner Radius](../../../gum-tool/gum-elements/skia-standard-elements/roundedrectangle/corner-radius.md) page.

## Custom Corner Radius

Each corner on a RoundedRectangleRuntime supports a custom radius.

For example, the following creates a RoundedRectangleRuntime with 0 radii for the bottom corners, and a larger corner for top right.

```csharp
var roundedRectangle = new RoundedRectangleRuntime();
MainStack.Children.Add(roundedRectangle);
roundedRectangle.Width = 100;
roundedRectangle.Height = 100;
roundedRectangle.Color = SKColors.Blue;

// This is the default radius:
roundedRectangle.CornerRadius = 20;

// But we can overwite each one by setting a value:
roundedRectangle.CustomRadiusTopRight = 40;
roundedRectangle.CustomRadiusBottomLeft = 0;
roundedRectangle.CustomRadiusBottomRight = 0;

// undo assignments by setting the value back to null:
roundedRectangle.CustomRadiusTopLeft = 50;
roundedRectangle.CustomRadiusTopLeft = null;
```

<figure><img src="../../../.gitbook/assets/29_13 41 32.png" alt=""><figcaption><p>RoundedRectangleRuntime with different corner radii</p></figcaption></figure>
