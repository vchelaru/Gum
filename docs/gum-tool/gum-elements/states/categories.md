# Categories

## Introduction

Categories can be used to organize similar states into one group (such as a button's Pressed and Unpressed states).

A category can contain one or more states. States within a category have special behavior:

1. If one state in a category explicitly sets a variable (such as X), then all other states in that category will also explicitly set the variable.
2. Each category can be set individually on an instance of a component or standard element. In other words, if a component has two categories, each category can be assigned to a state within that category independently.

## Creating Categories

To create a new category:

1. Select a Screen, Component, or Standard element which should contain the new category
2. Right-click in the States tab in an empty space
3.  Select the Add Category item\\

    <figure><img src="../../../.gitbook/assets/05_21 39 23.png" alt=""><figcaption><p>Right click Add Category item</p></figcaption></figure>
4. Enter a name for the new category

After the new category is created it appears in the States tab as a folder.

<figure><img src="../../../.gitbook/assets/05_21 43 35.png" alt=""><figcaption><p>Newly-created category</p></figcaption></figure>

## Adding States to a Category

To add states to a category:

1. Right-click on the desired category
2.  Select Add State\\

    <figure><img src="../../../.gitbook/assets/05_21 45 50 (1).png" alt=""><figcaption><p>Right click Add State item</p></figcaption></figure>
3. Enter a name for the new state

Once the state has been created it can be selected and variables can be changed to add them to the new state.

## Categories Create Variables

Once a category is created, the screen, component, or standard element which contains the category is automatically given a variable for that category type. This variable can be assigned on the element itself or on instances of the element.

For example, consider a component with a category named ExampleCategory with two states: State1 and State2.

<figure><img src="../../../.gitbook/assets/05_21 52 36.png" alt=""><figcaption><p>ExampleCategory with two states</p></figcaption></figure>

This component is given a variable named Example Category State.

<figure><img src="../../../.gitbook/assets/image (197).png" alt=""><figcaption><p>Example Category State variable</p></figcaption></figure>

This value can be assigned in the default state, making the selected state automatically set by default on the component.

For example, the DefaultComponent can select State1 as its Example Category State.

<figure><img src="../../../.gitbook/assets/05_22 01 38.png" alt=""><figcaption><p>Example Category State assigned by default</p></figcaption></figure>

Doing so results in this value automatically being selected on new instances of the DefaultComponent.

## States Set by Other States

Once a category is created, it adds a variable to the component. This variable behaves like any other variable including being able to be set by other states.

For example, consider a component with the following categories and states:

* ColorCategory
  * Bright
  * Dark
* SizeCategory
  * Big
  * Small

These states can be combined in a new category. For example, a category called CombinedCategory can be created which can include states such as BrightBig or DarkSmall which in turn sets category variables.

<figure><img src="../../../.gitbook/assets/05_22 42 54.gif" alt=""><figcaption><p>States setting variables created by other categories</p></figcaption></figure>

## Explicit Values Across States in a Category

Normally, when a new category is created and new states are added, all states are _empty_ - they do not assign any variables. The value displayed in the properties window is inherited from the default state.

For example, the following image shows a component with a state called **State1** with no variables explicitly assigned. Notice all values are green:

![](<../../../.gitbook/assets/30_19 46 57.png>)

As mentioned in the introduction, if a variable is explicitly set on one state in a category, then all other states in that category will that same variable set to its default. For example, if we set the **X** variable in the **LeftSide** state, the **X** variable in the **RightSide** state will become explicitly set (black instead of green).

![Setting X on LeftSide also sets X on other states](<../../../.gitbook/assets/30_20 07 19.gif>)

Once the **X** variable is set on one state in a category, all other states in the same category will automatically have this value set - even new states:

![New state automatically having variables set](<../../../.gitbook/assets/30_20 08 43.gif>)

## Removing Variables from Categories

Variables can be removed from states, but this removal must be done at the category level rather than at the individual state. Doing so will remove all variables from all states within a category. To remove a variable in all states in a category:

1. Select the category itself (not the state)
2. Click the "X" button next to the variable
3. Confirm that you would like to remove the variable. Warning: this will remove the variable from all contained states.

![](<../../../.gitbook/assets/removevariablefromcategory (1).png>)

This will remove the assignment of the variable from all states in the category.
