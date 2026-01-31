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
var textInstance = new TextRuntime();
textInstance.Text = "Hello world";
textInstance.AddToRoot();
```

<figure><img src="../../../.gitbook/assets/09_21 40 55.png" alt=""><figcaption><p>Text</p></figcaption></figure>

TextRuntimes can be added as children of controls. For example, the following code shows how to create a TextRuntime and add it to a Stackpanel named MainStackPanel.

```csharp
var textInstance = new TextRuntime();
textInstance.Text = "Hello world";
MainStackPanel.AddChild(textInstance);
```

## Example - Obtaining an Existing TextRuntime

TextRuntimes are used in Gum Forms controls for all Text display. For example, Button instanes use a TextRuntime named TextInstace, as shown in the following code block:

```csharp
var button = new Button();
var buttonVisual = (ButtonVisual)button.Visual;
var textRuntime = buttonVisual.TextInstance;
textRuntime.FontScale = 2; 
// additional modifications can be made to textRuntime
```
