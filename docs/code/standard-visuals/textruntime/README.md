# TextRuntime

## Introduction

The TextRuntime object is used to draw strings to the screen. It supports a variety of options for rendering text including alignment, fonts, coloring, and line wrapping.

A TextRuntime are often used in the following situations:

1. Adding diagnostics to your game to easily display information on screen
2. Adding visuals to a Forms control, such as adding another label to a Button
3. Modifying Visuals, such as a TextInstance in a ButtonVisual

TextRuntime corresponds to the Text type in the Gum UI tool, and shares all of the same properties. For more information see the [Text](../../../gum-tool/gum-elements/text/text.md) page.

## Example - Creating a TextRuntime

To create a TextRuntime, instantiate it and add it to root as shown in the following code:

```csharp
// Initialize
var textInstance = new TextRuntime();
textInstance.Text = "Hello world";
textInstance.AddToRoot();
```

<figure><img src="../../../.gitbook/assets/09_21 40 55.png" alt=""><figcaption><p>Text</p></figcaption></figure>

TextRuntimes can be added as children of controls. For example, the following code shows how to create a TextRuntime and add it to a Stackpanel named MainStackPanel.

```csharp
// Initialize
var textInstance = new TextRuntime();
textInstance.Text = "Hello world";
MainStackPanel.AddChild(textInstance);
```

## Example - Obtaining an Existing TextRuntime

TextRuntimes are used in Gum Forms controls for all Text display. For example, Button instanes use a TextRuntime named TextInstace, as shown in the following code block:

```csharp
// Initialize
var button = new Button();
var buttonVisual = (ButtonVisual)button.Visual;
var textRuntime = buttonVisual.TextInstance;
textRuntime.FontScale = 2;
// additional modifications can be made to textRuntime
```

## Tab Characters

A tab character (`\t`) in `Text` advances the width of four spaces in the font on MonoGame, KNI, FNA, and raylib. The tab is a fixed amount wide, not a tab stop, so text after it does not line up to a column.

```csharp
// Initialize
var textRuntime = new TextRuntime();
textRuntime.Text = "Name:\tGum";
```

On SkiaGum a tab advances by the text layout's own tab width rather than four spaces, so the same string can measure differently than it does on the other runtimes.
