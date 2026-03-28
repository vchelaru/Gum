# VerticalAlignment

## Introduction

VerticalAlignment controls the placement of the letters inside the Text's bounds. To align a TextRuntime in its parent, its YOrigin and YUnits may need to be modified as well to adjust its bounds.

## Example - Vertical Alignment Values

The following code shows how to use each VerticalAlignment value:

```csharp
// Initialize
var topAligned = new TextRuntime();
topAligned.AddToRoot();
topAligned.Text = "Hi, I am some text that is top aligned, even if I line wrap";
topAligned.VerticalAlignment = RenderingLibrary.Graphics.VerticalAlignment.Top;
topAligned.Width = 150;
topAligned.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
topAligned.Height = 150;
topAligned.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
topAligned.X = 50;
topAligned.Y = 100;

var centerAligned = new TextRuntime();
centerAligned.AddToRoot();
centerAligned.Text = "Hi, I am some text that is center aligned, even if I line wrap";
centerAligned.VerticalAlignment = RenderingLibrary.Graphics.VerticalAlignment.Center;
centerAligned.Width = 150;
centerAligned.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
centerAligned.Height = 150;
centerAligned.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
centerAligned.X = 250;
centerAligned.Y = 100;

var bottomAligned = new TextRuntime();
bottomAligned.AddToRoot();
bottomAligned.Text = "Hi, I am some text that is bottom aligned, even if I line wrap";
bottomAligned.VerticalAlignment = RenderingLibrary.Graphics.VerticalAlignment.Bottom;
bottomAligned.Width = 150;
bottomAligned.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
bottomAligned.Height = 150;
bottomAligned.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
bottomAligned.X = 450;
bottomAligned.Y = 100;
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAACp2Rz0vDMBTH7_srHjkplDJFL4qH6aAb6GXUX1gP2fpsH7RJSdLNKf7vJi0i6cpWPeZ9fyQvn88RAJvrqC7ZBRhVY-AGtSaRaTt5YXdSyIiXaB1hVOMUFa2tyAJgCxSpPYrslpaKq20YKV7ltNJOdP4pNzzeVqjZa1NLggzxgj7QVrM1V2BkNSkoE5jCFQjcQIzvZlELQyUeHV8m4tcQTtI0lgspTVdwGRtP2IwCmAMvQcsSwbixybkB0u4i4K0_AFyjAHqz3oIEwsY-O2F-5wMqQyteNOcShbtgZxbGsvJjj5Sa3FpPzsc9wr3dX1t1arcTmqRwA_dB4WSpZVEb9EMzpCw3fXWt8te-J-vtNj27-rEbJsIRWdm9UO2H4nk6XHxtCJo2cZiO3zwM0E2T2Qn7mHq0QT_r5zqw-sR_tDpkpz2FXWxLaYws92PzPB1svjYEW5s4jM1vHobtusnshH1sPdqgD_ZzHWx94j9aHbaznsIfbGz0NfoGd0h-OXsFAAA)

{% hint style="warning" %}
TODO: Replace with screenshot showing all three VerticalAlignment values.
{% endhint %}

## VerticalAlignment, Anchor, and Dock

The Anchor and Dock methods automatically set VerticalAlignment on Text elements to match the anchor or dock direction. For example, `Anchor(Anchor.Bottom)` sets VerticalAlignment to `Bottom`, and `Dock(Dock.Left)` sets it to `Center`. This means calling Anchor or Dock **overwrites** any previously set VerticalAlignment value.

If you need a different alignment than the default, set VerticalAlignment **after** calling Anchor or Dock to override it. The following example docks a TextRuntime to fill its parent's height (which defaults VerticalAlignment to `Center`), then overrides it to `Top`:

```csharp
// Initialize
var text = new TextRuntime();
text.AddToRoot();
text.Text = "Docked to left, but top-aligned";
// Dock sets VerticalAlignment to Center by default
text.Dock(Gum.Wireframe.Dock.Left);
// Override to top-align the characters within the full-height area
text.VerticalAlignment = RenderingLibrary.Graphics.VerticalAlignment.Top;
```

The same applies to Anchor. The following example anchors a TextRuntime to the bottom of its parent (which defaults VerticalAlignment to `Bottom`), then overrides it to `Top`:

```csharp
// Initialize
var text = new TextRuntime();
text.AddToRoot();
text.Text = "Bottom-anchored, but top-aligned";
text.Height = 150;
text.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
// Anchor sets VerticalAlignment to Bottom by default
text.Anchor(Gum.Wireframe.Anchor.Bottom);
// Override to top-align the characters inside the element
text.VerticalAlignment = RenderingLibrary.Graphics.VerticalAlignment.Top;
```
