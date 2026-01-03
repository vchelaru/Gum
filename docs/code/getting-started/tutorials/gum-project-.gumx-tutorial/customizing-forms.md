# Customizing Forms

{% hint style="danger" %}
This tutorial series represents the old way to add a .gumx project to your MonoGame project. This tutorial was  retired in April 2025, replaced by the new [Gum Project Forms Tutorial](../gum-project-forms-tutorial/).

This tutorial is still syntactically valid but it is not recommended as of the April 2025 release:

[https://github.com/vchelaru/Gum/releases/tag/Release\_April\_27\_2025](https://github.com/vchelaru/Gum/releases/tag/Release_April_27_2025)
{% endhint %}

## Introduction

Gum Forms controls provide standardized functionality which can be fully customized. Forms controls place very few restrictions on their appearance and behavior. Even though a typical Button has a Text and NineSlice, it could be made of anything (really).

This tutorial shows how to customize Forms controls

This tutorial continues from previous tutorials, but initially it deletes all instances from the TitleScreen except a single ButtonStandard instance.

<figure><img src="../../../../.gitbook/assets/image (161).png" alt=""><figcaption><p>A single ButtonStandardInstance in TitleScreen</p></figcaption></figure>

{% hint style="danger" %}
This tutorial modifies the default Gum Forms components. You may want to make a copy of your current project or use version control such as Git to undo your changes in case in case you want to start over.
{% endhint %}

## Customizing ButtonStandardInstance in Gum

The simplest form of customization is to modify the instance directly in Gum. Since the ButtonStandardInstance is a regular Gum object, any of its properties can be modified as expected. For example we can modify its X, Y, Width, Height, and Button Display Text properties in Gum and these changes show up in game.

<figure><img src="../../../../.gitbook/assets/image (162).png" alt=""><figcaption><p>Button with its instance properties changed</p></figcaption></figure>

While we technically have modified variables on the ButtonStandardInstance, this is usually not what we mean by customizing.

## Customizing Properties on ButtonStandard

We can make modifications to ButtonStandard directly in Gum. We can select the ButtonStandard in Gum to see its individual components.

<figure><img src="../../../../.gitbook/assets/image (163).png" alt=""><figcaption><p>ButtonStandard with its contained instances</p></figcaption></figure>

Notice that by default the Default state is selected.

<figure><img src="../../../../.gitbook/assets/02_17 41 30.png" alt=""><figcaption><p>Default state is selected</p></figcaption></figure>

If we make modifications to the ButtonStandard, every instance reflects these changes. For example, we can select TextInstance and set the following values:

* Font = Impact
* Font Size = 18
* Is Bold = false (unchecked)

<figure><img src="../../../../.gitbook/assets/02_13 46 47.png" alt=""><figcaption><p>Customized ButtonStandard</p></figcaption></figure>

Now our instance in TitleScreen also reflects these changes, both in Gum and also in our game

<figure><img src="../../../../.gitbook/assets/02_13 47 43.png" alt=""><figcaption><p>ButtonStandardInstance with modified values</p></figcaption></figure>

## Changing ButtonStandard States

If we try to change the background color on our Button, it doesn't apply in our game. For example, we can change the Background's color value in Gum. the ButtonStandard background changes in Gum:

<figure><img src="../../../../.gitbook/assets/02_17 11 41.png" alt=""><figcaption><p>Background color updates in Gum</p></figcaption></figure>

However, if we run the game, the color reverts back to the default teal color. This happens because the button has multiple colors, not just one. Which color is displayed depends on whether the button is enabled, disabled, hovered, or pressed. We can see some of these states by running the game, moving the mouse over the button, and clicking on it.

<figure><img src="../../../../.gitbook/assets/02_17 16 20.gif" alt=""><figcaption><p>Button showing its enabled, highlighted, and pressed states</p></figcaption></figure>

These colors are all controlled by the ButtonStandard's states inside of its ButtonCategory. We can expand the category and click on each of the states to preview them in the Editor tab.

<figure><img src="../../../../.gitbook/assets/02_17 21 39.gif" alt=""><figcaption><p>ButtonStandard states</p></figcaption></figure>

Since one of the states is always active, the maroon color we set in the default state is overridden immediately by the Enabled state when the app runs.

Note that only the color value was overridden by the ButtonCategory states - the changes we made to Font and Font Size showed up in game. We can see which variables are modified by the ButtonCategory states by selecting ButtonCategory.

<figure><img src="../../../../.gitbook/assets/02_17 29 50 (1).png" alt=""><figcaption><p>Variables set by ButtonCategory</p></figcaption></figure>

Any change we make to the ButtonStandard component can be either done in the Default state if it is not associated with a particular state, or it can be made in the specific ButtonCategory state.

We can modify the variables that are already set in the states, or we can remove the variables completely. For simplicity we will clear the states and start from scratch. To do this, select the ButtonCategory, then press the X button next to each variable to clear it from all states in that category.

<figure><img src="../../../../.gitbook/assets/02_17 57 27.gif" alt=""><figcaption><p>Remove all variables from the ButtonCategory to create new states</p></figcaption></figure>

Now we can select each of the states and modify the background color as desired. Feel free to experiment as you create states. Keep in mind you can modify anything on the ButtonStandard including TextInstance and Background. If you want to revert variable changes, select the ButtonCategory and press the X button to remove the variable assignments on all states.

As before, Gum lets us preview the states by selecting them.

<figure><img src="../../../../.gitbook/assets/02_18 15 27.gif" alt=""><figcaption><p>Changed states</p></figcaption></figure>

Once these states have been changed, we can run the game and see the states in game.

<figure><img src="../../../../.gitbook/assets/02_18 16 27.gif" alt=""><figcaption><p>Customized ButtonStandard</p></figcaption></figure>

For more information on states and categories, see the [States](../../../../gum-tool/tutorials-and-examples/intro-tutorials/states.md) and [State Categories](../../../../gum-tool/tutorials-and-examples/intro-tutorials/state-categories.md) tutorials.

## Modifying Other Forms Components

All Forms components can be customized in Gum. As with the ButtonStandard component, the first thing to check is whether a component has states. For example, to modify TextBox, first check which variables are modified in TextBoxCategory.

<figure><img src="../../../../.gitbook/assets/image (166).png" alt=""><figcaption><p>Variables in TextBoxCategory</p></figcaption></figure>

After determining which variables are controlled by the TextBoxCategory, make the changes to the TextBox either in the default state (if the variable is not set in TextBoxCategory), or select the desired state in TextBoxCategory (if the variable is in TextBoxCategory).
