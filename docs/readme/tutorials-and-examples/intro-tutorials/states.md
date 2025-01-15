# 7 - States

## Introduction

States allow you to set multiple variables at one time. Examples of states might include:

* A button with Regular, Highlighted, Pressed, and Disabled states
* A game logo in Large and Small modes
* A game HUD which can appear on and off screen

## Prerequisites

This tutorial builds upon the previous tutorial where a Button component was created. If you haven't yet, you should read through the earlier tutorials to create a Button component.

## Defining States

First we'll define two new states. All components and screens have a **Default** state automatically. This Default state is _uncategorize&#x64;**,**_ but all other states must be in a category. Therefore, we'll first add a new category:

1. Right-click in the States tab
2.  Select **Add Category**\


    <figure><img src="../../../.gitbook/assets/15_06 10 06.png" alt=""><figcaption><p>Add Category right-click option</p></figcaption></figure>
3. **Enter the name ButtonStateCategory**

&#x20;To add a new state:

1. Right-click on ButtonStateCategory
2.  Select Add State\


    <figure><img src="../../../.gitbook/assets/15_06 11 02.png" alt=""><figcaption><p>Add State right-click option</p></figcaption></figure>
3. Enter the name "Highlighted"
4. Click OK

![Enter the new state name](<../../../.gitbook/assets/15_06 11 35.png>)

The **Button** component now has a new state called **Highlighted**:

![Highlighted state](<../../../.gitbook/assets/15_06 13 05.png>)

## Setting Variables in States

Once a state is defined and selected, setting a variable associates that variable with the selected state. In other words, any variable that is set when the **Highlighted** state is selected results in that variable being added to that state.

For this example, we can make the button become a lighter blue when highlighted. To do this:

1. Verify the **Highlighted** state is selected
2. Select the **ColoredRectangleInstance**
3. Set the `Green` and `Red` values to `100`

Notice that the Green and Red values are rendered with a white background rather than green - indicating that they are explicitly set in the Highlight state.

![Red and Green Variables in Highlighted State](<../../../.gitbook/assets/15_06 15 30.png>)

{% hint style="warning" %}
Be careful when editing objects with multiple states. You may end up making changes without realizing that you are doing so in the wrong state.

Whenever you have a non-Default state selected, Gum displays a label telling you which state you are editing.\
\
![](<../../../.gitbook/assets/15_06 16 23.png>)
{% endhint %}

## Switching Between States

The values that have just been set apply **only** to the state that was selected when the changes were made - the Highlight state. This means that clicking on the Default state switches the button back to the default colors. By clicking on the states in Gum you can preview and edit states easily.

<figure><img src="../../../.gitbook/assets/15_06 17 42.gif" alt=""><figcaption><p>Changing states updates the Editor and Variables tabs to show the selected state</p></figcaption></figure>

## Category Variables

Whenever a state in a category sets a variable, that variable is set across all states in that category. So far this tutorial only created a single state called Highlighted, but if additional states are set, all will explicitly set the `Red` and `Green` variables. This topic is covered in more detail in the next tutorial.
