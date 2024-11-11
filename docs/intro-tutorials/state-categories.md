# 8 - State Categories

## Introduction

States are a powerful way to create expressive groups of variables. Some UI elements may require a combination of states to be applied simultaneously.

For example, consider creating a CheckBox element. This CheckBox may have one set of states for whether it is checked:

* Checked
* Unchecked

But it may also need a second set of states for being enabled and disabled (which may modify the color of the Text and graphics for the check box:

* Enabled
* Disabled

In this case multiple states need to coexist and may be combined. Categories allow you to organize states so that multiple states can be set simultaneously.

## Creating categorized states

For this tutorial we'll create a new component. This component will have state categories for size and for color. To do this:

1. Open Gum
2. Create a new Component called "CategoryDemo"
3. Right-click anywhere in the State box and select "Add Category"
4. Enter the name "Size" for the new category and click OK
5. Repeat the above steps to create a "Color" category

![](../.gitbook/assets/GumAddCategoryCalledSize.PNG)

![](../.gitbook/assets/GumAddCategory.png)

Now we can add states to the categories. To do this:

1. Right-click on the "Size" category and select "Add State"
2. Enter the name "Small" for the new state
3. Right-click on the "Size" category again and select "Add State"
4. Add a second state to "Big"
5. Right-click on the "Color" category and select "Add State
6. Add a state called "Red"
7. Right-click on the "Color" category again and select "Add State
8. Add a state called "Blue"

![](../.gitbook/assets/GumStatesInCategories.PNG)

## Adding visuals

Now that we have states set up we need to add a visual element to the component so that we can see our changes.

To do this, drag+drop a ColoredRectangle into your component

![](<../.gitbook/assets/GumColoredRectangleInComponent (1).PNG>)

## Setting variables in states

To modify a state, you can select it and edit in the preview window or change properties in the Variables tab to modify what the state sets. Notice that normally for a component like this the ColoredRectangleInstance would have its width and height be relative to its container, but we're not doing this for the sake of keeping the tutorial shorter.

First we'll set the Size states. To do this:

1. Select the "Big" state
2. Resize the colored rectangle so it is larger than the default
3. Select the "Small" state
4. Resize the colored rectangle so it is smaller than the default

![](../.gitbook/assets/GumSmallState.PNG)

![](../.gitbook/assets/GumBigState.PNG)

Next we'll set the Color states. To do this:

1. Select the "Red" state
2. Set the Red, Green, Blue values to: 255, 0, 0
3. Select the "Blue" state
4. Set the Red, Green, Blue values to: 0, 0, 255

![](<../.gitbook/assets/GumBlueState (1).PNG>)

![](../.gitbook/assets/GumRedState.PNG)

## Viewing multiple states on an instance

Now that we have our CategoryDemo component set up with multiple categories, we can view these states on any CategoryDemo instance. To do this:

1. Create a Screen called CategoryDemoScreen
2. Drop an instance of the CategoryDemo component into the CategoryDemoScreen
3. Select the newly-created CategoryDemoInstance
4. Scroll down in the Variables list and notice that the instance has drop-downs for each category.
5. You can set each state independently and the states will combine

![](../.gitbook/assets/GumCombinedStates.PNG)

![](../.gitbook/assets/GumLookCategoriesOnInstance.PNG)

### Categories and Variables

If a variable is modified in one of the states in a category, then all of the states in that category are automatically assigned the default value, and this value is explicitly set. This concept makes working with states far more predictable.

For example, we can consider a component which has:

* A single ColoredRectangle
* A category called RectangleSizeCategory
* States called Big, Medium, and Small

Initially, all states in a category do not explicitly assign any variables. We can see this by selecting the category and observing the Variables tab.

<figure><img src="../.gitbook/assets/image (5) (1) (1) (1).png" alt=""><figcaption><p>RectangleSizeCategory which does not set any variables</p></figcaption></figure>

If a variable is changed in one of the states in the category, then that variable propagates to all other states, and the Category lists this as one of the variables that it modifies.

For example, we can select the Big category and change the ColoredRectangle.Width property to 150.

<figure><img src="../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Setting ColoredRectangleInstance Width to 150</p></figcaption></figure>

Once this value is changed, the RectangleSizeCategory lists this as a variable that it modifies in the Variables tab.

<figure><img src="../.gitbook/assets/26_15 41 43.png" alt=""><figcaption><p>RectangleSizeCategory lists any variables that it changes</p></figcaption></figure>

If we select any of the other states in the category, they show that they explicitly set the Width value as well (the value has a white background instead of light green). The value is inherited from the default state.

<figure><img src="../.gitbook/assets/image (2) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Width value set by Medium state</p></figcaption></figure>

Once a variable is set in a category, all states are required to set this value. A variable cannot be removed from a single state in a category. Rather, to remove a variable, all states in the category must remove the variable. This can be done by selecting the category and pressing the X button next to the variable name.

<figure><img src="../.gitbook/assets/image (3) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Press the X next to a variable on a category to remove the assignment of that variable on all states in the category</p></figcaption></figure>

