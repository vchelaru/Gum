---
title: GetAbsoluteX
---

# GetAbsoluteX

The GetAbsoluteX method returns the absolute distance (in pixels) from the top-left of the screen to the top-left of the GraphicalUiElement, regardless of the GraphicalUiElement's X Units or X Origin.

### Code Example - Mouse Hit Test

To detect whether the mouse is ove ra GraphicalUiElement, the absolute properties can be used. This code does not assume a paritcular API for mouse coordiantes.

```csharp
// Assume MouseX is the screen space X coordinate and MouseY is the screen space Y coordinate
var isMouseOver = 
    MouseX > graphicalUiElement.GetAbsoluteLeft() &&
    MouseX < graphicalUiElement.GetAbsoluteRight() &&
    MouseY > graphicalUiElement.GetAbsoluteTop() &&
    MouseY < graphicalUiElement.GetAbsoluteBottom();
// use isMouseOver to perform additional logic
```
