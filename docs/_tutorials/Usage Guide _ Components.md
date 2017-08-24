---
title: Components
order: 2
---

# Introduction 
Components are objects which are created in Gum which can contain instances of other components and of standard objects.  Examples of components include:

* Check boxes
* Buttons
* Popup Menus

# Simple Button Example - Creating the Entity

To understand how components work, we'll create a simple Button component.  To do this:

# Right-click on the Components folder in Gum and select "Add Component"
# Name the Component "Button"
# Drag+drop a "ColoredRectangle" standard element into the Button component
# Drag+drop a "Text" standard element into the Button component

![](Usage Guide : Components_GumButton1.PNG)

Since both the ColoredRectangleInstance and TextInstance are using white text you may not be able to see the Text.  Let's change the ColoredRectangleInstance's color:

# Select the ColoredRectangleInstance
# Set Blue to 255 - this should change it from being green (default) to black (custom value).  To do this, simply delete and re-type 255 in the Blue box.
# Change Green to 0
# Change Red to 0

Now you should be able to see the Text on top of the rectangle:

![](Usage Guide : Components_GumWhiteTextBlueRect.PNG)

# Sizing the colored rectangle

At this point we have what will eventually become a button, but it still needs some work.  First, we're going to adjust the size of the objects contained in the button.  At this point you can see that the colored rectangle (the blue background for the button) is not the same size as the button.  Not only do we want to make the blue colored rectangle larger, but we also want it to automatically match the Button's size (the dotted outline).  

To do this:

# Selec the ColoredRectangleInstance
# Change Height Units to "RelativeToContainer"
# Change the Width Units to "RelativeToContainer
# Change the Height to 0.  This means that the Height of the ColoredRectangleInstance will match the Height of its container (the Button Component) since it's using "RelativeToContainer" Height Units.
# Change the Width to 0.  Just like with Height, this means that the Width of the ColoredRectangleInstance will match the Width of its container.

Now the ColoredRectangleInstance will automatically match the Button's Width and Height:

![](Usage Guide : Components_GumRelativeToContainerWidthHeight.PNG)

# Positioning the Text

Next we'll position the Text.  We'll want to adjust the Text so that it is always centered, and line-wraps with the size of the button.  First let's center the Text:

# Select the TextInstance
# Change its HorizontalAlignment to Center
# Change its VerticalAlignment to Center

At this point the Text is vertically and horizontally centered within its boundaries, but we want to have the boundaries centered within the Button.  To do this:

# Keep TextInstance selected
# Change the X Units to "PixelsFromCenterX"
# Change the X Origin to "Center"
# Change X to 0

Now let's make it centered on the Y as well:
# Keep the TextInstance selected
# Change the Y Units to "PixelsFromCenterY"
# Change the Y Origin to "Center"
# Change Y to 0

![](Usage Guide : Components_GumCenteredTextInButton.PNG)

Finally, let's make the width of the text match the width of the button.  For the Text we'll actually leave a border around the edge so the Text doesn't line wrap right against the edge of the button.  To do this:

# Keep the TextInstance selected
# Change the Width Units to "RelativeToContainer"
# Change Width to -40.  This means that the width of the Text will be 40 pixels less than the width of its container.  Since the button is centered this means a 20 pixel border on the left and 20 on the right (20+20=40).

![](Usage Guide : Components_GumBorderedText.PNG)

# Setting the Button default values

Buttons are typically wider than they are tall.  To match this common layout, let's set the default values on the Button:

# Select the Button component
# Change Width to 120
# Change Height to 36

Notice that whenever you change these values, the contained objects (text and colored rectangle) adjust automatically.

![](Usage Guide : Components_GumButtonResized.PNG)

# Using components as instances

Now that we have a component created, we can add instances of this component the same way we have added standard elements.  To do this:

# Create a new Screen.  I'll call mine MainMenu
# Drag+drop the Button component into the Screen

You can now resize and position the Button instance.  You can also add multiple buttons and adjust the individually.

![](Usage Guide : Components_GumMultipleButtonInstances.PNG)