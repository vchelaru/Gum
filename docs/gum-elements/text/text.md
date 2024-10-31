# Text

## Introduction

Text objects have a Text property which controls the displayed text. By default this value is set to "Hello".

## Changing Text

The Text property can be changed in the multi-line edit window.

![](../../.gitbook/assets/GumIsAwesome.png)

Text will wrap according to the Text object's Width.

![](<../../.gitbook/assets/LineWrappingTextGum (1).png>)

The enter key can be used to add new lines to text.

![](<../../.gitbook/assets/NewlinesGum (1).png>)

### Using BBCode for Inline Styling

Gum text supports inline styling using BBCode-like syntax. To add inline styling, surround text with variable assignment tags as shown in the following screenshot:

<figure><img src="../../.gitbook/assets/image (2) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Red color using BBCode syntax</p></figcaption></figure>

The following table shows the available variables that can be used for inline styling:



<table><thead><tr><th width="129">Tag</th><th width="357">Example</th><th>Result</th></tr></thead><tbody><tr><td>Color</td><td>This is [Color=orange]orange[/Color] text.</td><td><img src="../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt="" data-size="original"></td></tr><tr><td>Red Green Blue</td><td>This is [Red=0][Green=128][Blue=255]light blue[/Red][/Green][/Blue] text.</td><td><img src="../../.gitbook/assets/image (2) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt="" data-size="original"></td></tr><tr><td>FontScale</td><td>This is [FontScale=2]big[/FontScale] text.</td><td><img src="../../.gitbook/assets/image (3) (1) (1) (1) (1) (1).png" alt="" data-size="original"></td></tr><tr><td>IsBold</td><td>This is [IsBold=true]bold[/IsBold] text.</td><td><img src="../../.gitbook/assets/image (4) (1) (1).png" alt="" data-size="original"></td></tr><tr><td>IsItalic</td><td>This is [IsItalic=true]italic[/IsItalic] text.</td><td><img src="../../.gitbook/assets/image (6) (1).png" alt="" data-size="original"></td></tr><tr><td>Font</td><td>This is [Font=Papyrus]Papyrus[/Font] text.</td><td><img src="../../.gitbook/assets/image (7) (1).png" alt="" data-size="original"></td></tr><tr><td>FontSize</td><td>This is [FontSize=36]bigger[/FontSize] text.</td><td><img src="../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt="" data-size="original"></td></tr><tr><td>OutlineThickness</td><td>This is [OutlineThickness=2]outlined[/OutlineThickness] text.</td><td><img src="../../.gitbook/assets/image (8) (1).png" alt="" data-size="original"></td></tr></tbody></table>

Note that changing Font and FontSize results in new Fonts created in the Font Cache.

BBCode can span multiple lines, whether the newlines happen due to line wrapping or through the addition of newlines in the text.

<figure><img src="../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Text object with inline styling and explicit line breaks</p></figcaption></figure>

Multiple tags can overlap each other allowing you to combine tags for a single piece of text. For example, the following sets text to both bold and orange:

```bbcode
This is [Color=Orange][IsBold=true]bold and orange[/Color][/IsBold] text.
```

<figure><img src="../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Styled text that is bold and orange.</p></figcaption></figure>

Styles can contain other styles as many levels deep as necessary.

```bbcode
This [Color=Orange]is orange, [IsBold=true]bold[/IsBold], and [IsItalic=true]italic[/IsItalic][/Color] text.    
```

<figure><img src="../../.gitbook/assets/image (2) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption></figcaption></figure>
