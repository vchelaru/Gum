# 4 - Components

## Introduction

Components are reusable objects which can contain instances of other components and of standard objects. Examples of components include:

* Health Bars
* Buttons
* Popup Menus

Components can also be simple such as a button or more complex UI elements such as a full Options menu with dozens of instances.

Components can be made from-scratch or can be imported. Gum provides a full set of components for common controls including Button, TextBox, and ListBoxes, but for this tutorial we will be exploring how to build a component from scratch.

## Creating a Component and Instances

The first step in creating a component is to add an empty component and name it:

1. Right-click on the Components folder in Gum and select **Add Component**
2. Name the Component "HealthBar" (no spaces)

Next we'll add two Rectangle instances. One will be used as the background for the health bar, and one will be used as the fill for the health bar.

1. Drag+drop a Rectangle from the Standard folder into your Health Bar
2. Repeat this again to create a 2nd rectangle

If you dragged the instances on the tree view, the two Rectangle instances will overlap. We will adjust their positions later in this tutorial.

![HealthBar Component with two overlapping Rectangle instances](<../../../.gitbook/assets/30_05 37 15.png>)

## Naming Instances

Next we'll rename the instances so their names match their purpose. Rename the first rectangle Background and the second Fill. Notice that in Gum, the first item is the top-most, so you can think of it in the order that you would read text.

<figure><img src="../../../.gitbook/assets/30_05 40 41.png" alt=""><figcaption><p>Renamed Rectangle instances</p></figcaption></figure>

In this case, since Fill comes after Background, then it will draw on top. This ordering will become important when we make changes to these two instances later in the tutorial.

If you renamed them in the wrong order, you can also adjust their order in the component by selecting one of the instances, holding down the Alt key, and then pressing the up or down arrows to reorder the selected instance. You can also drag+drop to reorder, but be careful to not create an accidental parent/child relationship between the instances.

## Setting Background Variables

Next we'll adjust the Background Rectangle:

1. Select Background
2. Check the `Is Filled` variable
3. Set `Fill Color` to black - you can click on the colored box to open the color editor, or you can type the hex value `000000`&#x20;
4. Set `Stroke Width` to 0 - note that the stroke may not disappear since the Fill rectangle is still overlapping Background
5. Click on the `Alignment` tab and click the Fill button in the Dock row. This causes the Background to fill the size of its parent, and to adjust its own size if the parent HealthBar size changes.

Now the Background should be a solid black background to the entire component

<figure><img src="../../../.gitbook/assets/30_05 47 34.png" alt=""><figcaption><p>Background filling the entire component</p></figcaption></figure>

Before moving on, you might want to spend some time adjusting values on your background. Although we removed the stroke and changed the background to black, you may want your HealthBar to have a different appearance. Feel free to try changing other values to get a feel for the different options provided by Rectangle.

Also, note that we clicked the Fill Dock button. This button is a shortcut which changes a number of variables on the component. Specifically this changed:

* X, X Units, X Origin - the background will always be positioned horizontally according to its parent's center
* Y, Y Units, Y Origin - the background will always be positioned vertically according to its parent's center
* Width, Width Units - the background will always grow or shrink horizontally to fill its parent
* Height, Height Units - the background will always grow or shrink vertically to fill its parent

For more information on docking, see the [Dock page](../../gum-elements/general-properties/dock.md).

## Setting Fill Variables

Next we'll adjust our Fill Rectangle, starting with its color:

1. Select Fill
2. Check the `Is Filled` variable
3. Set `Fill Color` to a green color - the exact value doesn't matter too much so pick a color you like
4. Set `Stroke Width` to 0

<figure><img src="../../../.gitbook/assets/30_06 00 25.png" alt=""><figcaption><p>Fill displaying a Green color</p></figcaption></figure>

Unlike Background, the Fill rectangle needs to adjust in response to display how much health a player has. Specifically, the Fill rectangle needs to always fill its parent vertically, but its width depends on a health value.

First, we can adjust the height to fill its parent. To do this, set these values:

* Set Height to 0
* Set Height Units to Relative to Parent

<figure><img src="../../../.gitbook/assets/30_06 01 20.png" alt=""><figcaption><p>Fill matching its parent's height</p></figcaption></figure>

Height Units and Width Units can be used to adjust the size of your object relative to its parent or children. In this case the parent is the entire health bar itself. Note that we've set up our Height and Height Units to be relative to parent manually, which is exactly what was done earlier when clicked the Fill Dock button. As mentioned earlier, the Fill Dock button is just a shortcut for setting these types of values.

Next we'll adjust our fill bar so that the width is a percentage of its parent rather than an absolute value.

* Set Width to any value between 0 and 100. It should be 50 by default which is a good default
* Set Width Units to Percent of Parent

Now if we adjust the Width value, the health bar width adjusts relative to its parent.

<figure><img src="../../../.gitbook/assets/30_06 05 01.gif" alt=""><figcaption><p>Fill using Percent of Parent Width</p></figcaption></figure>

## Changing HealthBar Width and Height

Now that we have set up both Background and Fill to be relative to their parent's size, we can adjust the size of the HealthBar component and all of the instances should also adjust automatically.

Select the HealthBar component and set the following values:

* Set Width to 200
* Set Height ot 24

<figure><img src="../../../.gitbook/assets/30_06 08 28.png" alt=""><figcaption><p>HealthBar with adjusted Width and Height</p></figcaption></figure>

## Creating Component Instances

Now that we have a component created, we can add instances of this component the same way we have added standard elements. To do this:

1. Create a new Screen. I'll call mine GameScreen
2. Drag+drop the HealthBar component into the Screen

You can move and resize the newly-created HealthBarInstance in your screen. Notice that Background and Fill adjust in response to size changes on health bar.

You can also create copies by pressing CTRL+C, CTRL+V. Note that newly-created instances overlap the copied instance, so you need to move pasted instances.

<figure><img src="../../../.gitbook/assets/30_06 12 13.png" alt=""><figcaption><p>HealthBar instances in a screen</p></figcaption></figure>

## What's Next?

Although we can can change the size and position of our health bars, we are missing some very important functionality - the ability to change how much health is displayed. The next tutorial discusses how to expose variables so that each instance can be modified.
