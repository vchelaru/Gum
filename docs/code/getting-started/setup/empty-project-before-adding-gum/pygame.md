---
description: Gum can be used in Pygame using the PythonNET package.
---

# Pygame

## Experimental Note

{% hint style="danger" %}
_**WARNING:**_ This project is an experiment and proof of concept. The goal was to see if PythonNET could be used to export the C# GUM UI backend Layout Engine to be used in other tools like Python with PyGame.

Currently it "works" but there are many missing features and bugs.

Right now it only draws a RECTANGLE that encompasses the entire size of the GUM UI's [GraphicalUiElement](../../../gum-code-reference/graphicaluielement/).

You can then add other objects to this root object (that are currently only drawing rectangles).

You can't control the color, it starts at color value RGB(20, 20, 20) and increases in color intensity by 20 and wraps around at 256.

If this ends up being something that others want, please join the [discord](https://discord.gg/EvqwmSQuBz) or put tickets into [github](https://github.com/vchelaru/Gum).
{% endhint %}

Gum can be used in Pygame. To begin, create an empty Pygame project. For more information see [https://www.pygame.org/wiki/GettingStarted](https://www.pygame.org/wiki/GettingStarted)
