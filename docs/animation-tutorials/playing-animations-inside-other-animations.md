---
title: 3 - Playing Animations Inside Other Animations
order: 3
---

# 3 - Playing Animations inside other Animations

## Introduction

Gum supports creating animations which can play other animations. This is especially useful when creating animations in Screens that contain components which themselves have animations. This tutorial will build upon the [previous tutorial](https://github.com/vchelaru/Gum/tree/8c293a405185cca0e819b810220de684b436daf9/docs/Animation%20Tutorial/Usage-Guide_-Creating-an-Animation/README.md) where we created an animated component called TextComponent.

## Creating a Screen

First we'll create a Screen called AnimatedScreen. To do this:

1. Right-click on Screens
2. Select "Add Screen"
3. Enter the name "AnimatedScreen" and click the OK button
4. Drag+drop a few TextComponents into the Screen and spread them out visually

![](../.gitbook/assets/AddScreenAndText.gif)

## Defining the initial state

The animation we will be creating in our Screen will start with all TextComponents being invisible, then each one appearing by playing their Show animation. The animations will be slightly staggered. First we'll add the initial state where all of the TextComponents are invisible. To do this:

1. Verify that the AnimatedScreen is selected
2. Right-click in the states area and select "Add State"
3. Name the state "AllInvisible" and click OK
4. Select one of the TextComponents 
5. Set its State to Hidden
6. Repeat setting the State to Hidden for the other TextComponents

![](../.gitbook/assets/MakeAllInvisibleState.gif)

## Creating the Animation

Now we have all of the states and animations that we'll use as keyframes in our animation. To create the animation:

1. Select AnimatedScreen
2. Select "State Animation" -&gt; "View Animations"
3. Click "Add Animation"
4. Name the animation "ShowAll"
5. Select the ShowAll animation
6. Click "Add State"
7. Select "AllInvisible" and click OK

The animation now sets all TextComponents to their Hidden state initially.

![](../.gitbook/assets/CreateScreenAnimation1.gif)

## Adding Sub-Animations

Next we'll be adding animations to animate the TextComponent instances to visible. To do this:

1. Bring up the animation window for AnimatedScren if it is not already showing
2. Select "ShowAll"
3. Click "Add Sub-animation"
4. Select the first TextComponentInstance
5. Select the Show animation and click OK
6. Select the newly-created animation and set its Time to 0.5
7. Repeat the above steps to add animations for the other two TextComponents, but set their times to 1.0 and 1.5

![](../.gitbook/assets/AddingSubAnimations.gif)

Now the animation can be played or previewed with the slider bar:

![](../.gitbook/assets/PreviewAndPlayingSubAnimations.gif)

