---
title: 7 - States
category: Tutorials
order: 7
---


# Introduction
States allow you to set multiple variables at one time.  Examples of states might include:

* A button with Regular, Highlighted, Pressed, and Disabled states
* A game logo in Large and Small modes
* A game where multiple options can be selected

# Prerequisites
This tutorial builds upon the previous tutorial where a Button component was created.  To follow along you will need to have a Button component created as defined in the earlier tutorials.

# Defining states
First we'll define two new states.  All components and screens have a "Default" state automatically.  To add a new state:

# Select Edit->Add State ![](Usage Guide : States_GumEditAddState.png)
# Enter the name "Highlighted"
# Click OK ![](Usage Guide : States_GumEnterStateName.PNG)

The Button component will now have a new state called Highlighted:
![](Usage Guide : States_GumState.png)

# Setting variables in states
Once a state is defined and selected, setting a variable will associate that variable with a given state.  In other words, any variable that is set when the "Highlighted" state is selected will associate the variable with the Highlighted state.

For this example, we will make the button become a lighter blue when highlighted.  To do this:

# Verify the Highlighted state is selected
# Select the ColoredRectangleInstance
# Set the Green and Red values to 100

Notice that the Green and Red values are rendered in black rather than green - indicating that they are values that are explicitly set in the Highlight state.  

![](Usage Guide : States_GumStateValuesSet.png)

# Switching between states
The values that have just been set apply **only** to the state that was selected - the Highlight state.  This means that clicking on the Default state will switch the button back to the default colors.  By clicking on the states in Gum you can preview and edit states easily.

# States override Default
Newly-created states will not automatically override all values on all objects - only values which are explicitly set on a given state.  Therefore, any changes that are made in the Default state (excluding the Red and Green values on the ColoredRectangleInstance) will automatically apply to the Highlight state as well.  

This is reflected through which values are drawn with black vs. green text in the UI.