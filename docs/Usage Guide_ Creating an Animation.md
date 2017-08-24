---
title: Creating an Animation
---

# Introduction

This article shows how to create an animated component.  It will contain an animation which can be used when the component first appears.

# Creating the Component

First we'll create a component which will be animated.  To do this:

1. Right-click on Components
1. Select "Add Component"
1. Enter the name "TextComponent" and click OK
1. Drag+drop the "Text" Standard into the TextComponent to create a Text instance
1. Select the "Alignment" tab and click the middle button to have the TextInstance fill the TextComponent
1. Select the Variables tab and change the VerticalAlignment and HorizontalAlignment to Center

![](Usage Guide: Creating an Animation_CreatingAnimationCreateComponent.gif)

# Creating the States

Now that we have a component we'll add the states needed for animation.  We'll add all states in a category called HideShow.  To create the states:

1. Right click on the States section
1. Select "Add Category"
1. Enter the name "HideShow"
1. Right-click on the HideShow folder
1. Select "Add State"
1. Enter the name "Hidden" and click OK
1. Right-click on the HideShow folder
1. Select "Add State"
1. Select "Shown"

![](Usage Guide: Creating an Animation_AddHideShowStates.gif)

# Setting values in the states

Now that we have the states defined we can set values for the states.  In this case the only thing we'll be modifying is the TextInstance's "Font Scale" value.  To do this:

1. Select TextInstance
1. Select the "Hidden" state
1. Set the Font Scale to 0.  This makes the Text so small that it's invisible
1. Select the "Shown" state
1. Set the Font Scale to 1.  This effectively makes the Text regular size

![](Usage Guide: Creating an Animation_SetFontScale.gif)

# Creating the Show animation

The two states we created above will be used as the keyframes for our animation.  The animation will begin in the Hidden state then interpolate to the Shown state.  To add this animation:

1. Verify that TextComponent or any objects under it are selected
1. Select "State Animation" ->"View Animations"
1. Click the "Add Animation" button
1. Name the animation "Show" and click OK
1. Select the Show animation and click "Add State"
1. Select the Hidden state and click OK - this is the first keyframe in our animation
1. Click "Add State" again
1. Select "Shown" and click OK

![](Usage Guide: Creating an Animation_AddShowAnimation.gif)

The animation can now be played or previewed:

![](Usage Guide: Creating an Animation_PreviewAnimation1.gif)

# Adjusting Interpolation Type

The "Interpolation Type" value sets how one keyframe blends to another. By default keyframes use "Linear" interpolation.  When interpolating from one keyframe to another, the first one defines the interpolation type.  In our case the "Hidden" frame defines the interpolation type.  We can change this and preview the animation:

1. Select the "Hidden" keyframe
1. Change "Interpolation Type:" to "Elastic"

Playing the animation will reflect these changes.

![](Usage Guide: Creating an Animation_ElasticAnimation.gif)