# Text Wrapping

## Introduction

`TextRuntime` automatically wraps text according to its `Width` values. By default, text wrapping is effectively disabled for new `TextRuntime` instances, but it may be enabled if the TextRuntime is already a part of another control, such as a `Button`.

Text wrapping is determined based on the `Text` property and the size that is given to render the text based on the TextRuntime's `Width` and `WidthUnits`.

Text wrapping will not occur if the `TextRuntime`'s `WidthUnits` are set to `RelativeToChildren` since the instance automatically resizes itself to fit all letters. Otherwise, text wrapping may occur if the `Text` value is long enough.

For a more detailed discussion of WidthUnits see the [WidthUnits page](../../../gum-tool/gum-elements/general-properties/width-units.md).

## Code Example

The following code creates a Text instance which wraps its text. Since its `Width` is set to 100 absolute, the wrapped text must fit inside the `TextRuntime`'s absolute width.

```csharp
var text = new TextRuntime();
text.Text = "This text is long enough that it should wrap to multiple lines";
text.Width = 100;
text.WidthUnits = DimensionUnitType.Absolute;
mainPanel.AddChild(text);
```

<figure><img src="../../../.gitbook/assets/30_05 32 30.png" alt=""><figcaption><p>Text wrapping within the bounds of a TextRuntime</p></figcaption></figure>

TextRuntime treats its internal letters as children. If WidthUnits is changed to RelativeToChildren then the text no longer wraps since the TextRuntime sizes itself to contain all letters horizontally as shown in the following code block:

```csharp
var text = new TextRuntime();
text.Text = "This text is long, but it should no longer wrap because WidthUnits=RelativeToChildren";
text.WidthUnits = DimensionUnitType.RelativeToChildren;
// Width no longer affects wrapping, but it may still be needed to add padding
// if anything else depends on the Text instance's width
mainPanel.AddChild(text);
```

<figure><img src="../../../.gitbook/assets/30_05 36 27.png" alt=""><figcaption><p>Long text without wrapping</p></figcaption></figure>



