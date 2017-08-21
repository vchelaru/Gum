# Introduction

This article shows how to create an animated component.  It will contain an animation which can be used when the component first appears.

# Creating the Component

First we'll create a component which will be animated.  To do this:

# Right-click on Components
# Select "Add Component"
# Enter the name "TextComponent" and click OK
# Drag+drop the "Text" Standard into the TextComponent to create a Text instance
# Select the "Alignment" tab and click the middle button to have the TextInstance fill the TextComponent
# Select the Variables tab and change the VerticalAlignment and HorizontalAlignment to Center

![](Usage Guide: Creating an Animation_CreatingAnimationCreateComponent.gif)

# Creating the States

Now that we have a component we'll add the states needed for animation.  We'll add all states in a category called HideShow.  To create the states:

# Right click on the States section
# Select "Add Category"
# Enter the name "HideShow"
# Right-click on the HideShow folder
# Select "Add State"
# Enter the name "Hidden" and click OK
# Right-click on the HideShow folder
# Select "Add State"
# Select "Shown"

![](Usage Guide: Creating an Animation_AddHideShowStates.gif)

# Setting values in the states

Now that we have the states defined we can set values for the states.  In this case the only thing we'll be modifying is the TextInstance's "Font Scale" value.  To do this:

# Select TextInstance
# Select the "Hidden" state
# Set the Font Scale to 0.  This makes the Text so small that it's invisible
# Select the "Shown" state
# Set the Font Scale to 1.  This effectively makes the Text regular size

![](Usage Guide: Creating an Animation_SetFontScale.gif)

# Creating the Show animation

The two states we created above will be used as the keyframes for our animation.  The animation will begin in the Hidden state then interpolate to the Shown state.  To add this animation:

# Verify that TextComponent or any objects under it are selected
# Select "State Animation" ->"View Animations"
# Click the "Add Animation" button
# Name the animation "Show" and click OK
# Select the Show animation and click "Add State"
# Select the Hidden state and click OK - this is the first keyframe in our animation
# Click "Add State" again
# Select "Shown" and click OK

![](Usage Guide: Creating an Animation_AddShowAnimation.gif)

The animation can now be played or previewed:

![](Usage Guide: Creating an Animation_PreviewAnimation1.gif)

# Adjusting Interpolation Type

The "Interpolation Type" value sets how one keyframe blends to another.  By default keyframes use "Linear" interpolation.  When interpolating from one keyframe to another, the first one defines the interpolation type.  In our case the "Hidden" frame defines the interpolation type.  We can change this and preview the animation:

# Select the "Hidden" keyframe
# Change "Interpolation Type:" to "Elastic"

Playing the animation will reflect these changes.

![](Usage Guide: Creating an Animation_ElasticAnimation.gif)