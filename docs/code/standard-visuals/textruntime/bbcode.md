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
* HasDropshadow

The Gum tool generates fonts automatically into the FontCache folder. For code-only projects, the recommended approach is to use [dynamic font generation via KernSmith](fonts.md#dynamic-font-generation-recommended), which handles font creation at runtime with no extra setup. Without the Gum tool or KernSmith, you must provide pre-built `.fnt` files for each font combination your BBCode text uses.

The following variables work without any font setup:

* Color
* Red/Green/Blue
* FontScale
* Custom (see below)
{% endhint %}

## Code Example: Basic BBCode Styling

The following example shows how to use BBCode tags for inline color and scale changes:

```csharp
// Initialize
var text = new TextRuntime();
text.Text = "This is [Color=Red]red[/Color], [Color=Blue]blue[/Color], and [FontScale=2]big[/FontScale] text";
text.AddToRoot();
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAACkWOTQvCMAyG7_sVoSeF4cCjsoMfODx4mbt1PXQ2zEDXQtdOUfzvdoMphECe5CHvOwFg574IHduAdwHTEZAhT1LTCyNlg3Tg8ekhB4MPuFhjC9lhdFZFwCM6Gsi0qyqelMF46nCx3NZmVCYYvZpVd-ohFj9YbV1eohIOFc-mUaQz3-uAoontv5FGAT9Z4683qTFfi4Zanv2AmLLVbP64U6qypbU-hmDJ5wu5H0Ud4QAAAA)

BBCode tags can be nested and combined. Tags are written as `[TagName=Value]text[/TagName]`. The supported tags include `Color`, `Red`, `Green`, `Blue`, `FontScale`, `Font`, `FontSize`, `IsBold`, `IsItalic`, `OutlineThickness`, `HasDropshadow`, `Custom`, `Glyph` (see [Inline Glyphs](#inline-glyphs)), and `Link` (see [Links](#links)).

Tag names are not case sensitive, and a closing tag matches its opening tag whatever the case, so `[color=Red]red[/COLOR]` works the same as `[Color=Red]red[/Color]`.

An opening tag that has no closing tag is not a tag. It stays in the text and draws as literal characters:

```csharp
// Initialize
var text = new TextRuntime();
// Draws "This is [Color=Red]not closed" with the bracketed text visible.
text.Text = "This is [Color=Red]not closed";
text.AddToRoot();
```

`HasDropshadow` toggles the shadow for just the wrapped run, independently of the TextRuntime's own `HasDropshadow` value. The shadow's offset, blur, and color always come from the TextRuntime's `DropshadowOffsetX`/`DropshadowOffsetY`/`DropshadowBlur`/`DropshadowColor` properties; the tag only controls whether a given run draws it:

```csharp
// Initialize
var text = new TextRuntime();
text.HasDropshadow = true;
text.Text = "This has a shadow, [HasDropshadow=false]this does not[/HasDropshadow], and this does again";
text.AddToRoot();
```

{% hint style="info" %}
`HasDropshadow` toggles the shadow on MonoGame, KNI, FNA, and raylib. On SkiaSharp-based runtimes (Skia, Silk.NET, MAUI, WPF) the tag is parsed and stripped from the text but has no effect - the shadow always follows the TextRuntime's own `HasDropshadow` value there.
{% endhint %}

## Inline Glyphs

The `Glyph` tag inserts one character by its code point. Write the code point in decimal (`[Glyph=57344]`) or in hex with a `0x` prefix (`[Glyph=0xE000]`). A code point above U+FFFF, such as an emoji, becomes a surrogate pair. A tag with a value that is not a valid code point is removed from the text.

```csharp
// Initialize
var text = new TextRuntime();
text.Text = "Press [Glyph=0xE000] to jump";
text.AddToRoot();
```

The tag only substitutes the character, so the glyph appears when the font has it. It works the same on every runtime. To draw a picture, embed it in a bitmap font as described in [Custom Glyphs](../../files-and-fonts/advanced-font-effects.md#custom-glyphs).

{% hint style="info" %}
Available in November 2026, or now if building Gum from source.
{% endhint %}

## Links

The `Link` tag marks a range of text with an id. Gum does not style the range, and the tag is removed from the drawn text. Your code finds the range under a point with `GetTagAtIndex`, which takes a tag name and a character index and returns the tag that contains that character, or `null` if there is none. The index is in the displayed text (BBCode removed), the same index `GetCharacterIndexAtPosition` returns, so the two combine to find the link under the cursor.

The returned `TextTagSpan` has `Name`, `Argument` (the id, such as `shop`), `StartIndex`, and `EndIndex`. `EndIndex` is one past the last character in the tag. The tag name is not case sensitive. When tags with the same name are nested, `GetTagAtIndex` returns the innermost one. A tag is only found if it has a closing tag.

The following code opens a different page depending on which link the player clicks. The `Color` tags are optional. They make the links visible:

```csharp
// Class scope
TextRuntime linkText;

protected override void Initialize()
{
    GumUI.Initialize(this);

    linkText = new TextRuntime();
    linkText.Text =
        "Visit the [Link=shop][Color=Yellow]shop[/Color][/Link] " +
        "or read the [Link=help][Color=Yellow]help[/Color][/Link].";
    linkText.AddToRoot();

    base.Initialize();
}

protected override void Update(GameTime gameTime)
{
    GumUI.Update(gameTime);

    var cursor = GumUI.Cursor;
    if (cursor.PrimaryClick && linkText.HasCursorOver(cursor))
    {
        int index = linkText.GetCharacterIndexAtPosition(
            cursor.XRespectingGumZoomAndBounds(),
            cursor.YRespectingGumZoomAndBounds());

        TextTagSpan? link = linkText.GetTagAtIndex("Link", index);
        if (link != null)
        {
            // link.Value.Argument is "shop" or "help"
            OpenPage(link.Value.Argument);
        }
    }

    base.Update(gameTime);
}
```

For the index math behind `GetCharacterIndexAtPosition`, see [GetCharacterIndexAtPosition](getcharacterindexatposition.md).

{% hint style="info" %}
Available in November 2026, or now if building Gum from source.
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
   * `Func<int, string, LetterCustomization>` - the int value is the index of the letter in the whole displayed text, with all BBCode tags removed. The string represents the text inside the custom block. The returned LetterCustomization instance includes the desired modifications to the letter. Register with `Text.Customizations`.
   * `Func<int, string, LetterCustomization, LetterCustomization>` - same as above, but the third parameter is a LetterCustomization containing the letter's current state (color, offset, scale) as set by prior BBCode tags. This allows relative modifications such as darkening the current color. Register with `Text.ContextCustomizations`.
2. This function must be registered with the `RenderingLibrary.Graphics.Text` type using the appropriate dictionary.
3. A Custom tag must be included in the text string.

The int index counts displayed characters from the start of the whole text, not from the start of the block. In `"Gold: [Custom=Wave]50[/Custom]"` the function receives 6 for `5` and 7 for `0`, because `Gold: ` is six characters. The index is a UTF-16 index with the BBCode tags removed, so an emoji counts as two. The string parameter is only the text inside the block, so its `Length` is the block's length.

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

<figure><img src="../../../.gitbook/assets/01_07 39 37 (1).gif" alt=""><figcaption><p>Button adding gold and resetting the grow shirnk time.</p></figcaption></figure>

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
