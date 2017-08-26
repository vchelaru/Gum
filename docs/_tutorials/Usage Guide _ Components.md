---
title: 4 - Components
order: 4
---

# Introduction 
Components are objects which are created in Gum which can contain instances of other components and of standard objects.  Examples of components include:

* Check boxes
* Buttons
* Popup Menus

# Simple Button Example - Creating the Entity

To understand how components work, we'll create a simple Button component.  To do this:

1. Right-click on the Components folder in Gum and select "Add Component"
1. Name the Component "Button"
1. Drag+drop a "ColoredRectangle" standard element into the Button component
1. Drag+drop a "Text" standard element into the Button component

![](GumButton1.PNG)

Since both the ColoredRectangleInstance and TextInstance are using white text you may not be able to see the Text.  Let's change the ColoredRectangleInstance's color:

1. Select the ColoredRectangleInstance
1. Set Blue to 255 - this should change it from being green (default) to black (custom value).  To do this, simply delete and re-type 255 in the Blue box.
1. Change Green to 0
1. Change Red to 0

Now you should be able to see the Text on top of the rectangle:

![](GumWhiteTextBlueRect.PNG)

# Sizing the colored rectangle

At this point we have what will eventually become a button, but it still needs some work.  First, we're going to adjust the size of the objects contained in the button.  At this point you can see that the colored rectangle (the blue background for the button) is not the same size as the button.  Not only do we want to make the blue colored rectangle larger, but we also want it to automatically match the Button's size (the dotted outline).  

To do this:

1. Select the ColoredRectangleInstance
1. Change Height Units to "RelativeToContainer"
1. Change the Width Units to "RelativeToContainer
1. Change the Height to 0.  This means that the Height of the ColoredRectangleInstance will match the Height of its container (the Button Component) since it's using "RelativeToContainer" Height Units.
1. Change the Width to 0.  Just like with Height, this means that the Width of the ColoredRectangleInstance will match the Width of its container.

Now the ColoredRectangleInstance will automatically match the Button's Width and Height:

![](GumRelativeToContainerWidthHeight.PNG)

# Positioning the Text

Next we'll position the Text.  We'll want to adjust the Text so that it is always centered, and line-wraps with the size of the button.  First let's center the Text:

1. Select the TextInstance
1. Change its HorizontalAlignment to Center
1. Change its VerticalAlignment to Center

At this point the Text is vertically and horizontally centered within its boundaries, but we want to have the boundaries centered within the Button.  To do this:

1. Keep TextInstance selected
1. Change the X Units to "PixelsFromCenterX"
1. Change the X Origin to "Center"
1. Change X to 0

Now let's make it centered on the Y as well:

1. Keep the TextInstance selected
1. Change the Y Units to "PixelsFromCenterY"
1. Change the Y Origin to "Center"
1. Change Y to 0

![](GumCenteredTextInButton.PNG)

Finally, let's make the width of the text match the width of the button.  For the Text we'll actually leave a border around the edge so the Text doesn't line wrap right against the edge of the button.  To do this:

1. Keep the TextInstance selected
1. Change the Width Units to "RelativeToContainer"
1. Change Width to -40.  This means that the width of the Text will be 40 pixels less than the width of its container.  Since the button is centered this means a 20 pixel border on the left and 20 on the right (20+20=40).

![](GumBorderedText.PNG)

# Setting the Button default values

Buttons are typically wider than they are tall.  To match this common layout, let's set the default values on the Button:

1. Select the Button component
1. Change Width to 120
1. Change Height to 36

Notice that whenever you change these values, the contained objects (text and colored rectangle) adjust automatically.

![](GumButtonResized.PNG)

# Using components as instances

Now that we have a component created, we can add instances of this component the same way we have added standard elements.  To do this:

1. Create a new Screen.  I'll call mine MainMenu
1. Drag+drop the Button component into the Screen

You can now resize and position the Button instance.  You can also add multiple buttons and adjust the individually.

![](GumMultipleButtonInstances.PNG)