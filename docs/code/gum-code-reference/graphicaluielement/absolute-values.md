# Absolute Values

## Introduction

Absolute values can be obtained from a GraphicalUiElement by using the appropriate absolute values. these values return the value of the GraphicalUiElement relative to the screen.&#x20;

The following absolute values are available:

* AbsoluteLeft - Returns the absolute left edge of the GraphicalUiElement
* AbsoluteTop - Returns the absolute top edge of the GraphicalUiElement
* AbsoluteRight - Returns the absolute right edge of the GraphicalUiElement
* AbsoluteBottom - Returns the absolute bottom edge of the GraphicalUiElement
* AbsoluteX - Returns the absolute X of the GraphicalUiElement considering its XOrigin
* AbsoluteY - Returns the absolute Y of the GraphicalUiElement considering its YOrigin

## Code Example - Drawing a Sprite at the Absolute Position

The following assumes that container is a valid GraphicalUiElement. Its absolute position is used to draw a Sprite using SpriteBatch.

```csharp
var absoluteLeft = container.AbsoluteLeft;
var absoluteTop = container.AbsoluteTop;

_spriteBatch.Begin();
_spriteBatch.Draw(texture, new Vector2(absoluteLeft, absoluteTop), Color.White);
_spriteBatch.End();
```
