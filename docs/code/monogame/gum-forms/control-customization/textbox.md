# TextBox

## Introduction

This page shows how to customize TextBox instances.

## TextBox and DefaultTextBoxRuntime

By default TextBox instances use a Visual of type DefaultTextBoxRuntime which inherits from DefaultTextBoxBaseRuntime. The full source for these two types can be found here:

{% embed url="https://github.com/vchelaru/Gum/blob/master/MonoGameGum/Forms/DefaultVisuals/DefaultTextBoxRuntime.cs" %}

{% embed url="https://github.com/vchelaru/Gum/blob/master/MonoGameGum/Forms/DefaultVisuals/DefaultTextBoxBaseRuntime.cs" %}

## Customizing Selection

The following code shows how to customize the selection color:

```csharp
var textBox = new TextBox();
var selection = (ColoredRectangleRuntime)textBox
    .Visual.GetChildByNameRecursively("SelectionInstance")!;

selection.Color = Microsoft.Xna.Framework.Color.Blue;
```
