# 7 - States

## Introduction

States allow you to set multiple variables at one time. Examples of states might include:

* A button with Regular, Highlighted, Pressed, and Disabled states
* A game logo in Large and Small modes
* A game HUD which can appear on and off screen

This tutorial shows how to add states which can be used to display if a player is low, medium, or high health by adjusting the color of the fill bar.

## Prerequisites

This tutorial builds upon the previous tutorials where a HealthBar component was created. If you haven't yet, you should read through the earlier tutorials to create a HealthBar component.

## Defining States

First we'll define a category and our three states. All components and screens have a **Default** state automatically. This Default state is _uncategorize&#x64;**,**_ but all other states must be in a category. Therefore, we'll first add a new category:

1. Right-click in the States tab
2.  Select **Add Category**

    <figure><img src="../../../.gitbook/assets/30_06 28 54.png" alt=""><figcaption><p>Add Category right-click option</p></figcaption></figure>
3. Enter the name HealthCategory

Although it's not necessary, categories often have the name "Category" at the end.

To add a new state:

1. Right-click on HealthCategory
2.  Select Add State

    <figure><img src="../../../.gitbook/assets/30_06 28 19.png" alt=""><figcaption><p>Add State right-click option</p></figcaption></figure>
3. Enter the name "High"
4. Click OK
5. Add a second state named Medium
6. Add a third state named Low

The **HealthBar** component should now have three states.

![HealthBar states](<../../../.gitbook/assets/30_06 30 45.png>)

## Setting Variables in States

Once a state is defined and selected, you can make changes to the component and any of those changes are applied to the selected state. In other words, any variable that is set when the **High** state is selected results in that variable being added to that state.

For this component, we want to change the Fill color depending on whether the health is High, Medium, or Low.

Our High state should display a green color, and if you've set your Fill to be green, then this is already set how we want. This means we don't have to make any changes to the High state.

Instead, we'll make the fill yellow in the Medium state:

1. Select the Medium state
2. Select the Fill Rectangle instance under HealthBar
3. Change the Fill Color variable to a yellow color. Note that the fact that our rectangle is called "Fill" and that we are changing "Fill Color" is a coincidence. We named our Rectangle Fill because it fills the health bar. The `Fill Color` exists on every Rectangle, and controls the inner color of a rectangle, as opposed to `Stroke Color` which controls the outline color.

Although this might seem like a simple change, a lot has happened in response to this change, so we should take a moment to break it down.

First, by changing the `Fill Color` value on the Medium state, we have set every state to explicitly set its color. We can see this by noticing that every state now has an edited icon.

<figure><img src="../../../.gitbook/assets/30_06 37 28.png" alt=""><figcaption><p>Edited Icon on State</p></figcaption></figure>

This tells us that the Fill instance is modified by every state in the `HealthCategory`. Even the High and Low states, which haven't been modified, are now explicitly setting the color to the default green.

{% hint style="warning" %}
Be careful when editing objects with multiple states. You may end up making changes without realizing that you are doing so in the wrong state.

Whenever you have a non-Default state selected, Gum displays a label telling you which state you are editing.\
\
![](<../../../.gitbook/assets/30_06 39 15.png>)

Any unintentional change can be undone by pressing CTRL+Z
{% endhint %}

We can modify the Low state to be a red color:

1. Select the Low state
2. Make sure Fill is still selected
3. Change the `Fill Color` variable to a red color

Now we can select any state and see what our state looks like immediately.

<figure><img src="../../../.gitbook/assets/30_06 42 09.gif" alt=""><figcaption><p>States previewed in the Gum tool</p></figcaption></figure>

## States and Instances

Now that we have added a new category to our HealthBar component, we can assign per-instance.

<figure><img src="../../../.gitbook/assets/30_06 44 01.png" alt=""><figcaption></figcaption></figure>

Notice that the `Health Category State` variable is independent of `Health Percent`. Although the two are conceptually connected, Gum considers these to be two unrelated variables which can be adjusted individually. These variables would typically be connected in code in a game.

## What's Next - More Categories

This tutorial covers how to set up a state, but the next tutorial dives deeper into categories, including using multiple categories to mix variable assignments.
