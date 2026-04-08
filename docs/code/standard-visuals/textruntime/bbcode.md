# BBCode

## Introduction

TextRuntime supports using BBCode-like syntax for formatting individual words and letters. Customizations include changing size, font, and even using custom functions.

Using BBCode for TextRuntimes supports all of the functionality outlined in the Text documentation which can be found here: [https://docs.flatredball.com/gum/gum-tool/gum-elements/text/text#using-bbcode-for-inline-styling](https://docs.flatredball.com/gum/gum-tool/gum-elements/text/text#using-bbcode-for-inline-styling)

{% hint style="info" %}
The following BBCode variables require font generation:

* IsBold
* IsItalic
* Font
* FontSize
* OutlineThickness

The Gum tool generates fonts automatically into the FontCache folder. For code-only projects, the recommended approach is to use [dynamic font generation via KernSmith](fonts.md#dynamic-font-generation-recommended), which handles font creation at runtime with no extra setup. Without the Gum tool or KernSmith, you must provide pre-built `.fnt` files for each font combination your BBCode text uses.

The following variables work without any font setup:

* Color
* Red/Green/Blue
* FontScale
* Custom (see below)
{% endhint %}

## Custom Functions

TextRuntime instances can use the `Custom` tag to define a custom function to be used when rendering a Text. This tag can be used to customize letters in a TextRuntime using custom code, allowing for effects such as wavy, rainbow, or shaking text.

The following variables can be modified on each letter. This list also includes their default values:

* XOffset = 0
* YOffset = 0
* Color = null
* ScaleX = 1
* ScaleY = 1
* RotationDegrees = 0
* ReplacementCharacter = null

To use the Custom tag, the following must be performed:

1. A function must be declared which is of one of the following types:
   * `Func<int, string, LetterCustomization>` - the int value represents the index in the custom block, starting with 0. The string represents the entire string in the block. The returned LetterCustomization instance includes the desired modifications to the letter. Register with `Text.Customizations`.
   * `Func<int, string, LetterCustomization, LetterCustomization>` - same as above, but the third parameter is a LetterCustomization containing the letter's current state (color, offset, scale) as set by prior BBCode tags. This allows relative modifications such as darkening the current color. Register with `Text.ContextCustomizations`.
2. This function must be registered with the `RenderingLibrary.Graphics.Text` type using the appropriate dictionary.
3. A Custom tag must be included in the text string.

### Code Example: Shaking Letters With Custom Tag

The following code block shows how to create a TextRuntime with letters shaking horizontally:

```csharp
// Initialize
LetterCustomization ShakeHorizontally(int index, string textInBlock)
{
    var customization = new LetterCustomization();
    var seconds = DateTime.Now.TimeOfDay.TotalSeconds;
    customization.XOffset = MathF.Sin((float)(seconds * 55 + index * 4));
    return customization;
}
// The function must be registered before it is used
RenderingLibrary.Graphics.Text.Customizations["ShakeHorizontally"] = ShakeHorizontally;

var shakingText = new TextRuntime();
shakingText.AddToRoot();
shakingText.Anchor(Anchor.Center);
shakingText.Text = "I am feeling so [Custom=ShakeHorizontally]cold[/Custom] today!";
```

<figure><img src="../../../.gitbook/assets/01_05 56 54.gif" alt=""><figcaption><p>TextRuntime displaying shaking the word "cold"</p></figcaption></figure>

### Code Example: On-Command Custom Values

Custom tags can reference functions inside of classes which can provide additional customization, such as one-time changes.

The following code shows how to make text grow and shrink one time whenever gold is added. First, we create a class that includes the logic for growing/shrinking. This class also stores a StartTime variable which is used to determine the text size. Note that this is using a DateTime for simplicity, but you could also game time or other timing methods.

```csharp
class GrowShrinkCustomFunction
{
    public DateTime StartTime { get; set; } 
    public LetterCustomization Execute(int index, string textInBlock)
    {
        var timeSinceStart = (float)(DateTime.Now - StartTime).TotalSeconds;

        const float growTime = 0.15f;

        float scale = 1;
        float maxScale = 0.5f;
        
        // We only want to change the scale if the time is within the grow/shrink
        // period
        if(timeSinceStart < growTime)
        {
            var ratioGrown = timeSinceStart / growTime;
            scale = 1 + maxScale * ratioGrown;
        }
        else if(timeSinceStart < 2 * growTime)
        {
            var ratioShrunk = (timeSinceStart - growTime) / growTime;
            scale = 1 + maxScale - maxScale * ratioShrunk;
        }

        var customization = new LetterCustomization();
        customization.ScaleX = scale;
        customization.ScaleY = scale;
        return customization;
    }
}
```

We can register this function and use it on a TextRuntime. The following code creates a button which increases the gold and resets the start time:

```csharp
// Class scope
int _gold = 0;
GrowShrinkCustomFunction _growShrink;

protected override void Initialize()
{
    GumUI.Initialize(this);

    _growShrink = new GrowShrinkCustomFunction();
    // The function must be registered before it is used
    RenderingLibrary.Graphics.Text.Customizations["GrowAndShrink"] = 
        _growShrink.Execute;

    var goldDisplayText = new TextRuntime();
    goldDisplayText.AddToRoot();
    goldDisplayText.Anchor(Anchor.Center);
    goldDisplayText.Text = "Gold: [Custom=GrowAndShrink]0[/Custom]";

    var button = new Button();
    button.Text = "Get Gold!";
    button.AddToRoot();
    button.Anchor(Anchor.Center);
    button.Y += 50;
    button.Click += (_, _) =>
    {
        _gold += 5;
        goldDisplayText.Text = "Gold: [Custom=GrowAndShrink]" + _gold + "[/Custom]";
        _growShrink.StartTime = DateTime.Now;
    };
}
```

<figure><img src="../../../.gitbook/assets/01_07 39 37 (1) (1).gif" alt=""><figcaption><p>Button adding gold and resetting the grow shirnk time.</p></figcaption></figure>

### Code Example: Context-Aware Custom Function

Custom functions can optionally receive the letter's current state as a `LetterCustomization` parameter. This is useful for making relative modifications — for example, darkening whatever color was set by prior BBCode tags.

```csharp
LetterCustomization Darken(int index, string textInBlock, LetterCustomization context)
{
    var color = context.Color ?? System.Drawing.Color.White;
    return new LetterCustomization
    {
        Color = System.Drawing.Color.FromArgb(color.A, color.R / 2, color.G / 2, color.B / 2)
    };
}
RenderingLibrary.Graphics.Text.ContextCustomizations["Darken"] = Darken;

var text = new TextRuntime();
text.AddToRoot();
text.Text = "This is [Color=Red]red and [Custom=Darken]dark red[/Custom][/Color] text";
```

The context parameter includes the current Color, XOffset, YOffset, ScaleX, and ScaleY as resolved by any prior BBCode tags on the same letter. Functions using the simple two-parameter signature continue to work unchanged.
