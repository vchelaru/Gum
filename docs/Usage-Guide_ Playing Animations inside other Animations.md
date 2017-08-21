# Introduction

Gum supports creating animations which can play other animations.  This is especially useful when creating animations in Screens that contain components which themselves have animations.  This tutorial will build upon the [previous tutorial](Usage-Guide_-Creating-an-Animation) where we created an animated component called TextComponent.

# Creating a Screen

First we'll create a Screen called AnimatedScreen.  To do this:

# Right-click on Screens
# Select "Add Screen"
# Enter the name "AnimatedScreen" and click the OK button
# Drag+drop a few TextComponents into the Screen and spread them out visually

![](Usage Guide: Playing Animations inside other Animations_AddScreenAndText.gif)

# Defining the initial state

The animation we will be creating in our Screen will start with all TextComponents being invisible, then each one appearing by playing their Show animation.  The animations will be slightly staggered.  First we'll add the initial state where all of the TextComponents are invisible.  To do this:

# Verify that the AnimatedScreen is selected
# Right-click in the states area and select "Add State"
# Name the state "AllInvisible" and click OK
# Select one of the TextComponents 
# Set its State to Hidden
# Repeat setting the State to Hidden for the other TextComponents

![](Usage Guide: Playing Animations inside other Animations_MakeAllInvisibleState.gif)

# Creating the Animation

Now we have all of the states and animations that we'll use as keyframes in our animation.  To create the animation:

# Select AnimatedScreen
# Select "State Animation" -> "View Animations"
# Click "Add Animation"
# Name the animation "ShowAll"
# Select the ShowAll animation
# Click "Add State"
# Select "AllInvisible" and click OK

The animation now sets all TextComponents to their Hidden state initially.

![](Usage Guide: Playing Animations inside other Animations_CreateScreenAnimation1.gif)

# Adding Sub-Animations

Next we'll be adding animations to animate the TextComponent instances to visible.  To do this:

# Bring up the animation window for AnimatedScren if it is not already showing
# Select "ShowAll"
# Click "Add Sub-animation"
# Select the first TextComponentInstance
# Select the Show animation and click OK
# Select the newly-created animation and set its Time to 0.5
# Repeat the above steps to add animations for the other two TextComponents, but set their times to 1.0 and 1.5

![](Usage Guide: Playing Animations inside other Animations_AddingSubAnimations.gif)

Now the animation can be played or previewed with the slider bar:

![](Usage Guide: Playing Animations inside other Animations_PreviewAndPlayingSubAnimations.gif)