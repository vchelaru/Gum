# 7 - States

## Introduction

States allow you to set multiple variables at one time. Examples of states might include:

* A button with Regular, Highlighted, Pressed, and Disabled states
* A game logo in Large and Small modes
* A game where multiple options can be selected

## Prerequisites

This tutorial builds upon the previous tutorial where a Button component was created. To follow along you will need to have a Button component created as defined in the earlier tutorials.

## Defining states

First we'll define two new states. All components and screens have a "Default" state automatically. This Default state is _uncategorize&#x64;**,**_ but all other states must be in a category. Therefore, we'll first add a new category:

1. Right-click in the States tab
2.  Select **Add Category**\


    <figure><img src="../.gitbook/assets/image (67).png" alt=""><figcaption><p>Add Category right-click option</p></figcaption></figure>
3. **Enter the name ButtonStateCategory**

&#x20;To add a new state:

1. Right-click on ButtonStateCategory
2.  Select Add State\


    <figure><img src="../.gitbook/assets/image (68).png" alt=""><figcaption><p>Add State right-click option</p></figcaption></figure>
3. Enter the name "Highlighted"
4. Click OK

![](<../.gitbook/assets/GumEnterStateName (1).PNG>)

The Button component will now have a new state called Highlighted:

![](<../.gitbook/assets/30_14 35 35.png>)

## Setting variables in states

Once a state is defined and selected, setting a variable will associate that variable with a given state. In other words, any variable that is set when the "Highlighted" state is selected will associate the variable with the Highlighted state.

For this example, we will make the button become a lighter blue when highlighted. To do this:

1. Verify the Highlighted state is selected
2. Select the ColoredRectangleInstance
3. Set the Green and Red values to 100

Notice that the Green and Red values are rendered with a white background rather than green - indicating that they are values that are explicitly set in the Highlight state.

![Red and Green Variables in Highlighted State](<../.gitbook/assets/30_14 37 18.png>)

## Switching between states

The values that have just been set apply **only** to the state that was selected - the Highlight state. This means that clicking on the Default state will switch the button back to the default colors. By clicking on the states in Gum you can preview and edit states easily.

## Category Variables

Whenever a state in a category sets a variable, that variable is set across all states in that category. So far this tutorial only created a single state called Highlighted, but if additional states are set, all will explicitly set the Red and Green state. This topic is covered in more detail in the next tutorial.
