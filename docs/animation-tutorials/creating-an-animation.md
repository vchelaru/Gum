# 2 - Creating an Animation

## Introduction

This article shows how to create an animated component. It will contain an animation which can be used when the component first appears.

## Creating the Component

First we'll create a component which will be animated. To do this:

1. Right-click on **Components**
2. Select **Add Component**
3. Enter the name **TextComponent** and click OK
4. Drag+drop the **Text** Standard into the **TextComponent** to create a Text instance
5. Select the **Alignment** tab and click the middle button to have the TextInstance fill the TextComponent

<figure><img src="../.gitbook/assets/06_11 33 32.gif" alt=""><figcaption></figcaption></figure>

## Creating the States

Now that we have a component we'll add the states needed for animation. We'll add all states in a category called HideShow. Animation states should always be categorized. To create the states:

1. Right click in the States list box
2. Select **Add Category**
3. Enter the name **HideShow**
4. Right-click on the **HideShow** folder
5. Select **Add State**
6. Enter the name **Hidden** and click OK
7. Right-click on the **HideShow** folder
8. Select **Add State**
9. Select **Shown**

<figure><img src="../.gitbook/assets/06_11 36 47.gif" alt=""><figcaption></figcaption></figure>

## Setting values in the states

Now that we have the states defined we can set values for the states. In this case the only thing we'll be modifying is the TextInstance's **Font Scale** value. To do this:

1. Select **TextInstance**
2. Select the **Hidden** state
3. Set the **Font Scale** to 0. This makes the Text so small that it's invisible
4. Select the **Shown** state
5. Verify the **Font Scale** is 1, or set it to 1 if not. This makes the Text regular size

<figure><img src="../.gitbook/assets/06_11 38 57.gif" alt=""><figcaption></figcaption></figure>

## Creating the Show animation

The two states we created above will be used as the keyframes for our animation. The animation will begin in the Hidden state then interpolate to the Shown state. To add this animation:

1. Verify that **TextComponent** or any objects under it are selected
2. Select **State Animation** ->**View Animations**
3. Click the **Add Animation** button
4. Name the animation **Show** and click **OK**
5. Select the Show animation and click **Add State**
6. Select the **Hidden** state and click **OK** - this is the first keyframe in our animation
7. Click **Add State** again
8. Select **Shown** and click **OK**

<figure><img src="../.gitbook/assets/06_11 41 08.gif" alt=""><figcaption></figcaption></figure>

The animation can now be played or previewed:

<figure><img src="../.gitbook/assets/06_11 42 21.gif" alt=""><figcaption></figcaption></figure>

## Adjusting Interpolation Type

The **Interpolation Type** value sets how one keyframe blends to another. By default keyframes use **Linear** interpolation, which is a constant change from one state to another. When interpolating from one keyframe to another, the first keframe defines the interpolation type. In our case the **Hidden** frame defines the interpolation type. We can change the Interpolation Type and preview the animation:

1. Select the **Hidden** keyframe
2. Change **Interpolation Type** to **Elastic**

Playing the animation will reflect these changes.

<figure><img src="../.gitbook/assets/06_11 44 11.gif" alt=""><figcaption></figcaption></figure>
