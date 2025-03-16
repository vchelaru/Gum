# ColoredRectangleRuntime

The ColoredRectangleRuntime object can be used to draw a solid (filled) rectangle of any color. To draw a rectangle outline (not filled in), see the [RectangleRuntime](rectangleruntime.md) type.

### Code Example

The following code creates a ColoredRectangleRuntime:

```csharp
var coloredRectangleInstance = new ColoredRectangleRuntime();
coloredRectangleInstance.Width = 120;
coloredRectangleInstance.Height = 24;
coloredRectangleInstance.Color = Color.White; // This is a Microsoft.Xna.Framework.Color
container.Children.Add(coloredRectangleInstance);

```

<figure><img src="../../.gitbook/assets/image (46).png" alt=""><figcaption><p>ColoredRectangleRuntime instance created in code</p></figcaption></figure>
