# Text Wrapping

## Introduction

`TextRuntime` does not automatically wraps its displayed text, but line wrapping can be enabled by changing the `WidthUnits`.&#x20;

{% hint style="info" %}
Text wrapping behavior changed in the 2025 May 28 release. Previously, TextRuntime instances had WidthUnits set to Absolute, resulting in the text wrapping. This behavior was confusing for new users, so it has been changed. For more information, see [2025 May 28 release notes page](../../../gum-tool/upgrading/migrating-2025-april-27-to-may-28.md).
{% endhint %}

Text wrapping is determined based on the `Text` property and the size that is given to render the text based on the TextRuntime's `Width` and `WidthUnits`.

Text wrapping will not occur if the `TextRuntime`'s `WidthUnits` are set to `RelativeToChildren` (default behavior) since the instance automatically resizes itself to fit all letters. Otherwise, text wrapping may occur if the `Text` value is long enough.

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

## Text.IsMidWordLineBreakEnabled (XNA-likes only)

XNA-like environments (MonoGame, FNA, KNI, and FlatRedBall) control whether line wrapping takes place mid-word, or only on whitespace characters. As of June 2025 line wrapping only occurs on whitespace characters, but this behavior can be changed. For example, the following code shows how to set a TextRuntime instance to break apart words when wrapping.

```csharp
var text = new TextRuntime();
// Set this value before changing width-related properties.
// This is a global value, so it is used by all TextRuntime
// instances:
RenderingLibrary.Graphics.Text.IsMidWordLineBreakEnabled = true;
text.Width = 100;
text.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
text.Text = "abcdefghijklmnopqrstuvwxyz";
stackPanel.AddChild(text);
```

<figure><img src="../../../.gitbook/assets/22_14 00 30.png" alt=""><figcaption><p>Text wrapping mid-word </p></figcaption></figure>
