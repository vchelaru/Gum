# TextBox

## Introduction

This page shows how to customize TextBox instances.

## Customizing Selection

The following code shows how to customize the selection color:

```csharp
 var textBox = new TextBox();
 textBox.AddToRoot();
 var textBoxVisual = (TextBoxVisual)textBox.Visual;
 var selection = textBoxVisual.SelectionInstance;
 selection.Color = Microsoft.Xna.Framework.Color.Blue;
```

<figure><img src="../../../../.gitbook/assets/14_23 00 56.gif" alt=""><figcaption></figcaption></figure>
