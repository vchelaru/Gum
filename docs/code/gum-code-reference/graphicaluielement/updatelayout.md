# UpdateLayout

## Introduction

UpdateLayout performs a layout call resulting in the GraphicalUiElement's renderable object updating its X, Y, Width, and Height values. UpdateLayout may recursively call UpdateLayout the caller's parent and children.

Usually UpdateLayout does not need to be called explicitly since it is automatically called in response to Values on the GraphicalUiElement changing. The most common situation for calling UpdateLayout is when the GraphicalUiElement CanvasWidth or CanvasHeight properties change, such as in response to a game window resizing. For more information, see the [CanvasHeight](canvasheight.md) page.

## UpdateLayout Updates Renderable Components

Most GraphicalUiElements wrap a renderable component such as a Text, Sprite, or Rectangle. Although applications treat GraphicalUiElements as if they are renderable themselves, that is technically not the case. Rather, GraphicalUiElements are simply wrappers around renderable components. The GraphicalUiElement is responsible for performing layouts and ultimately setting the internal Renderable's Parent, X, Y, Width, and Height. This is done in the UpdateLayout call.

If a GraphicalUiElement has not changed, then it does not call UpdateLayout for performance reasons.  For performance reasons Gum attempts to minimize the number of UpdateLayout calls performed. Of course, it is impossible to eliminate all UpdateLayout calls, especially when a Screen or Component is first created.

## Properties Which Call UpdateLayout

Gum attempts to always keep renderable components up-to-date. Therefore, Gum automatically calls UpdateLayout if a GraphicalUiElement's property changes which may cascading impacts on the contained renderable, children of the GraphicalUiElement, or the parent of the GraphicalUiElement.
