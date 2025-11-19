# RectangleRuntime

The RectangleRuntime object can be used to draw a single-pixel-wide rectangle. It can either have a solid outline or dotted, and can display any color. RectangleRuntimes only draw outlines. For filled rectangles see the [ColoredRectangleRuntime](coloredrectangleruntime.md) type.

### Code Example

The following code creates a RectangleRuntime:

```csharp
var lineRectangle = new RectangleRuntime();
lineRectangle.Width = 120;
lineRectangle.Height = 24;
lineRectangle.Color = Color.Pink;  // This is a Microsoft.Xna.Framework.Color
container.Children.Add(lineRectangle);
```

<figure><img src="../../.gitbook/assets/WideRectOverBlue.png" alt=""><figcaption><p>RectangleRuntime instantiated in code</p></figcaption></figure>
