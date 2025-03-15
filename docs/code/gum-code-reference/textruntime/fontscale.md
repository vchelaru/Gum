# FontScale

## Introduction

FontScale is used to multiply the size of text. By default FontScale is set to 1. Setting FontScale to 2 doubles its size.

Setting FontScale on Skia-based platforms is similar to setting FontSize, although slight kerning differences may exist.&#x20;

Setting FontScale on XNA-like platforms and FlatRedBall changs the size of the Text and can result in pixelated or blurry fonts. Unlike FontSize, changing FontScale does not require additional .fnt/png pairs, so it can be adjusted at runtime without the need for additional fonts.

## Example - Setting FontScale

The following code shows how to assign FontScale.

```csharp
MyText.FontScale = 2; // A value of 2 doubles the text's size
```
