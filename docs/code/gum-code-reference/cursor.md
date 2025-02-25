# Cursor

## Introduction

Cursor provides processed input values for detecting the cursor position, pushes, and clicks. Cursor is used internally by Gum to perform operations such as clicking on buttons or dragging sliders.

## Accessing Cursor

Most runtimes create a Cursor internally. The cursor can be accessed using FormsUtilities.

```csharp
var cursor = FormsUtilities.Cursor;
```

## WindowOver

WindowOver returns the current InteractiveGue that the Cursor is hovering over. This returns the _visual_ that the user is over when using Forms controls.

A runtime instance is eligible for being the Cursor's WindowOver if its HasEvents variable is set to true, and if either of the following are true:

* It is an instance of a Component
* It is an instance of a Standard Element, but it has any cursor-related events assigned such as Push or Click.

For example, a Sprite may be considered WindowOver if it has a Click event assigned.

### Code Example - Preventing Click-Throughs

The WindowOver property can be used to detect if clicks should be consumed or passed down to your game. For example, you may have a HUD which displays buttons. If the user Cursor is over the HUD then the game should not process input.

The following code shows how to check if the user is over Gum UI.

```csharp
var cursor = FormsUtilities.Cursor;
var isOverUi = cursor.WindowOver != null;

if(isOverUi == false)
{
    PerformGameCursorLogic();
}
```
