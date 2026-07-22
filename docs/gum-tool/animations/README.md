# Animations

The **Animations** tab lets you create state-based animations — timelines that move an element between its [States](../tutorials-and-examples/intro-tutorials/states.md) over time. An animation is a sequence of keyframes, where each keyframe applies one of the element's states at a particular point in time, and Gum interpolates between them during playback.

By default the **Animations** tab is hidden. To show it, select **View** ▸ **View Animations** from the main menu.

{% hint style="warning" %}
**TODO:** Screenshot of the **Animations** tab.
{% endhint %}

## Creating Animations

Animations are defined per element (Screen, Component, or Standard element) and are saved alongside that element. For a step-by-step walkthrough of creating an animation, adding keyframes, looping, and nesting animations inside one another, see the [Animation Tutorials](../tutorials-and-examples/animation-tutorials/README.md).

## Named Events

The **Add Named Event** option places a named marker at a point in time on an animation.

{% hint style="info" %}
**Named Events** are not used by Gum's own runtimes (MonoGame, KNI, FNA, raylib, SkiaSharp, Silk.NET) — they are saved and loaded but never raised. They exist for integration with FlatRedBall (FRB1). Unless you are using Gum with FlatRedBall (FRB1), you can ignore the **Add Named Event** option.
{% endhint %}
