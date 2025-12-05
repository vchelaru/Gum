# Layout Overview

## Introduction

Gum provides a flexible layout engine for creating responsive UI. Gum objects can be docked, anchored, stacked, wrapped, positioned, and sized dynamically.

Layouts can be performed quickly using common containers such as StackPanel. Dock and Anchor methods can be used to position objects relative to their parent container. Individual _unit_ values can be used to fine-tune the position of objects.

## Parent/Child Relationship

Gum objects can be added to other Gum objects to create parent/child relationships. For example, the following shows two rectangles inside a parent container. Notice that the rectangles are positioned relative to the parent's position.

<figure><img src="../../.gitbook/assets/15_10 25 52.gif" alt=""><figcaption><p>Children are positioned relative to their parent</p></figcaption></figure>

Hierarchies can be many levels deep, allowing for complex, reactive layouts. Multiple containers can be used to adjust how different areas of a component react to changes in size.

<figure><img src="../../.gitbook/assets/15_10 38 15.gif" alt=""><figcaption></figcaption></figure>

## Dock and Anchor

Gum objects can be docked and anchored to create common layout behaviors.

Anchor adjusts the position of objects so that they reset against the sides, corners, or center of their parent.

<figure><img src="../../.gitbook/assets/15_16 56 39.png" alt=""><figcaption></figcaption></figure>

Dock adjusts the position and size of an object so it sits against one or more edges of its parent. Docking can be used to adjust the width an object and also anchor it to the top or bottom.

<figure><img src="../../.gitbook/assets/15_17 00 23.png" alt=""><figcaption></figcaption></figure>

Similarly, Dock can be used to adjust an object vertically and optionally anchor it to the left or right.

<figure><img src="../../.gitbook/assets/15_17 03 14.png" alt=""><figcaption></figcaption></figure>

## Units and Origins

Objects have the following Units and Origin values which provide more fine-tuned control over positioning and sizing. For example, an object can be positioned under its parent with the following code:

```csharp
buttonInstance.Dock(Dock.Bottom);
// after docking, set the YOrigin to Top, resulting in the button's 
// top being docked to the bottom of its parent
```

<figure><img src="../../.gitbook/assets/15_17 11 10.png" alt=""><figcaption></figcaption></figure>

## Container Stack and Grid

Parent containers can stack, wrap, and arrange their children in a grid.

<figure><img src="../../.gitbook/assets/15_17 30 53.png" alt=""><figcaption></figcaption></figure>
