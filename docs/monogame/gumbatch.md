# GumBatch

### Introduction

GumBatch is an object which supports _immediate mode_ rendering, similar to MonoGame's SpriteBatch. GumBatch can support rendering text with DrawString as well as any IRenderableIpso.

For information on getting your project set up to use GumBatch, see the [Setup for GumBatch](setup-for-gumbatch.md) page.

### Rendering Strings

GumBatch can be used to render strings directly. This requires a BitmapFont.

The following code renders a string at X = 100, Y=200:

```csharp
gumBatch.Begin();
gumBatch.DrawString(
    font, 
    "I am at X=100, Y=200",
    new Vector2(100, 200), 
    Color.White);
gumBatch.End();
```

<figure><img src="../.gitbook/assets/image (63).png" alt=""><figcaption><p>A single text object rendered using DrawString</p></figcaption></figure>

Multiple strings can be rendered between `Begin` and `End` calls:

```csharp
gumBatch.Begin();
for(int i = 0; i < 10; i++)
{
    gumBatch.DrawString(
        font, 
        $"This is string {i}", 
        new Vector2(0, 20*i), 
        Color.White);

}
gumBatch.End();
```

<figure><img src="../.gitbook/assets/image (64).png" alt=""><figcaption><p>Multiple DrawString calls between Begin and End</p></figcaption></figure>

DrawString can accept newlines and color the text:

```csharp
gumBatch.Begin();
    gumBatch.DrawString(
        font, 
        $"This string contains\nnewlines which result in\nthe text rendering over multiple lines",
        new Vector2(20, 20), 
        Color.Purple);
gumBatch.End();
```

<figure><img src="../.gitbook/assets/image (65).png" alt=""><figcaption><p>Colored text with newlines</p></figcaption></figure>

### Rendering TextRuntimes

If your text rendering requires more advanced positioning, wrapping, rotation, sizing, and so on, you can use TextRuntime instances.

TextRuntimes can be used in both GumBatch as well as they can be added to the SystemManagers if you use SystemManagers.Draw (retained mode).

The following code shows how to create a TextRuntime instance and render it using GumBatch:

```csharp
// In Game1 at class scope:
TextRuntime textRuntime;

// In Initialize:
textRuntime = new TextRuntime();
textRuntime.UseCustomFont = true;
textRuntime.CustomFontFile = "Fonts/Font16Jing_Jing.fnt";
textRuntime.Text = 
    "I am an immediate mode TextRuntime. I am really long text which will wrap within the bounds of the TextRuntime";
textRuntime.X = 0;
textRuntime.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
textRuntime.XOrigin = HorizontalAlignment.Center;

textRuntime.Y = 0;
textRuntime.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
textRuntime.YOrigin = VerticalAlignment.Center;

textRuntime.HorizontalAlignment = HorizontalAlignment.Center;
textRuntime.VerticalAlignment = VerticalAlignment.Center;

textRuntime.Width = 300;
textRuntime.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
textRuntime.Height = 0;
textRuntime.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

textRuntime.Rotation = 90;

// In Draw:
gumBatch.Begin();
gumBatch.Draw(textRuntime);
gumBatch.End();
```

<figure><img src="../.gitbook/assets/image (66).png" alt=""><figcaption><p>Rendering a TextRuntime in immediate mode with GumBatch</p></figcaption></figure>
