# Cursor

## Introduction

Cursor provides processed input values for detecting the cursor position, pushes, and clicks. Cursor is used internally by Gum to perform operations such as clicking on buttons or dragging sliders.

The Cursor abstracts mouse and touch screen input, so it can be used regardless of input hardware.

## Accessing Cursor

Most runtimes create a Cursor internally.&#x20;

The cursor can be accessed through GumService.

```csharp
var cursor = GumService.Default.Cursor;
```

GumService.Default.Cursor was introduced in March 2025. Older versions of Gum can access the cursor through FormsUtilities.

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
var cursor = GumService.Default.Cursor;
var isOverUi = cursor.WindowOver != null;

if(isOverUi == false)
{
    PerformGameCursorLogic();
}
```

### Code Example - Diagnosing Clicks

The WindowOver property can be used to identify problems with items not receiving mouse events. For example, a Button may be a child of a parent container but be positioned outside of the parent's bounds. This results in the Button not receiving clicks. This can be diagnosed by printing the WindowOver to output.

```csharp
protected override void Update(GameTime)
{
    GumUI.Update(gameTime);
    var cursor = GumIO.Cursor;
    System.Diagnostics.Debug.WriteLine(cursor.WindowOver);
    base.Update(gameTime);
}
```

## WindowPushed

WindowPushed returns the InteractiveGue (Forms Visual) which was under the Cursor when its PrimaryPush was set to true. WindowPushed can be used to handle drag+drop operations.

For example, consider a game which allows the user to purchase items. The user can push and drag an item onto a container to buy the item. The following code shows how that might be performed:

```csharp
var cursor = GumService.Default.Cursor;

// A PrimaryClick means the cursor was released...
// and the WindowOver can be checked if it is over the area 
// where the user must drop purchased items.
if(cursor.PrimaryClick && cursor.WindowOver == BuyArea)
{
    // Get the item that was pushed
    var itemDragged = cursor.WindowPushed;
    
    // Make sure the user actually pushed on an item
    if(itemDragged is ItemDisplay itemDisplay)
    {
        // This assumes the ItemDisplay is bound to an ItemViewModel
        var itemToBuy = itemDisplay.BindingContext as ItemViewModel;
        // use itemToBuy to do your game's logic when buying an item    
    }
}
```
