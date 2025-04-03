# Styling

## Introduction

Gum components are built with full styling support. This tutorial shows how to perform styling on your components. It begins with simple styling by changing fonts and colors, but then gets into more advanced topics like creating your own style colors and new components.

## Setup

If you already have a Gum project, either your own or one created in an earlier tutorial, then you can follow along with your existing project. If you do not have a project already set up, you should follow the Setup instructions.

This tutorial uses the DemoScreenGum page to check how styles appear in game. You are free to use any page, but the DemoScreenGum provides a good view of many components so we'll use that in this tutorial.

## Styling Overview

Gum provides a way to modify your styling at every level, whether you want to change colors and fonts, or redo the structure of all of your components.

Colors are perhaps the most obvious types of variables that might be changed. Gum includes a number of standard colors which are centrally located in a Styles component. We'll look at the Styles component a little later in this tutorial.

Font types and sizes are also common variables when changing styles. Gum also includes a centralized location for modifying fonts.

Styles can also include modifying images, such as changing the borders on a NineSlice. Gum projects include a set of borders which can be used. The source PNG can also be modified.

Styling can also involve larger changes to controls, such as replacing a button's NineSlice with a static image displayed by a Sprite, an animation, or even using Skia graphics.

## Colors

As mentioned above, the default Forms project includes a Styles component which includes a collection of colors used throughout all other components.

<figure><img src="../../../../.gitbook/assets/02_07 13 40.png" alt=""><figcaption><p>Styles component displaying colors and fonts</p></figcaption></figure>

The Styles component includes a section which defines all of the standard colors used in the Gum project.

These colors are defined on ColoredRectangle instances. We can select one of the ColoredRectangle instances to see its color.

<figure><img src="../../../../.gitbook/assets/02_07 16 09.png" alt=""><figcaption><p>PrimaryLight colored rectangle</p></figcaption></figure>

Notice that the rectangles are named based on their usage. Since gray, black, and white colors are so common, there are dedicated rectangles for those. Aside from those, each rectangle is named based on its usage. For example, the three primary rectangles (PrimaryLight, Primary, and PrimaryDark) indicate that their color is used as the main color throughout the Gum project.

For example, notice that DemoScrenGum uses the Primary color on labels, radio buttons, and labels.

<figure><img src="../../../../.gitbook/assets/02_08 07 06.png" alt=""><figcaption><p>DemoScreenGum displaying the primary color throughout the UI</p></figcaption></figure>

{% hint style="info" %}
The outlines that are drawn by Gum can get in the way of viewing styles. You can disable this by selecting **Edit ->** Properties, then unchecking **Show Outlines**.

![](<../../../../.gitbook/assets/02_08 10 43.png>)
{% endhint %}

We can change the color values i the Styles component by selecting any of the rectangles and changing their colors. The names suggest their purpose, so if you are changing the Primary color values, be sure to consider the Light and Dark suffixes. For example, we can change the teal colors to orange colors in Styles.

<figure><img src="../../../../.gitbook/assets/02_08 13 32.png" alt=""><figcaption><p>Primary colors changed to orange</p></figcaption></figure>

Now if we select our DemoScreenGum, notice that the UI has updated in response to these color changes.

<figure><img src="../../../../.gitbook/assets/02_08 14 37.png" alt=""><figcaption><p>DemoScreenGum restyled orange</p></figcaption></figure>

Of course, you are free to modify all other colors including the different shades of gray or the other colors like Success, Danger, and Warning.

## Fonts

We can also modify the fonts used in our project by changing the Text instances in the Styles component. We can modify the Font, Font Size, Is Bold, and Is Italic properties on any of our Text instances and these changes will update in our app.

For example, we can modify the Title text to have a larger font by changing `Font Size` to `38` .

<figure><img src="../../../../.gitbook/assets/02_17 20 13.png" alt=""><figcaption><p>Title Font Size set to 38</p></figcaption></figure>

This value updates the size of title labels on DemoScreenGum.

<figure><img src="../../../../.gitbook/assets/02_17 25 49.png" alt=""><figcaption><p>Titles in DemoScreenGum at a larger size</p></figcaption></figure>

The font face for the entire project can also be changed by selecting all of the Text instances and changing the styles.

To select all Text instances, expand the TextStyles container in the Styles component, select the first text, hold down shift, then select the last text.

<figure><img src="../../../../.gitbook/assets/02_17 55 16.gif" alt=""><figcaption><p>Select multiple texts</p></figcaption></figure>

Next, change the font to the desired value. Gum may need to generate the fonts if this is the first time it is used in your project, so this may take a few seconds.

<figure><img src="../../../../.gitbook/assets/02_17 57 33.png" alt=""><figcaption><p>Change the font on all Text instances at once</p></figcaption></figure>

This font should appear on all texts in the DemoScreenGum.

<figure><img src="../../../../.gitbook/assets/02_17 58 32.png" alt=""><figcaption><p>Updated fonts</p></figcaption></figure>

## Changing Default Colors

Gum components are styled using the default colors so they work well without any modifications, but you may want to change colors on components without making project-wide changes to the color styles.

For example, we can take a look at the TextBox component to see how it is styled.

By default it has a CaretInstance which uses the Primary color.

<figure><img src="../../../../.gitbook/assets/02_18 05 06.png" alt=""><figcaption><p>CaretInstance uses the Primary color</p></figcaption></figure>

Similarly, SelectionInstance uses the Accent color.

<figure><img src="../../../../.gitbook/assets/02_18 06 03.png" alt=""><figcaption><p>SelectionInstance uses the Accent color</p></figcaption></figure>

We can change the color used by picking a different color in the dropdown. For example, we could change the selection to use the PrimaryLight color.

<figure><img src="../../../../.gitbook/assets/02_18 06 56.png" alt=""><figcaption><p>SelectionInstance using the PrimaryLight color</p></figcaption></figure>

## Changing Colors in States

Some colors are applied depending on the state of the component. For example, the TextBox's Background changes depending on whether it is enabled or highlighted.

When making any styling edits, be sure to first check if the variable is modified by any states. If it is, then you must make your modifications on the non-default state.

As mentioned above, to change the TextBox's background, you must first select one of the states within the TextBoxCategory. To see if a variable is modified in a state, click on the category to see which variables are modified. In this case, the TextBoxCategory modifies the following variables:

* Background.ColorCategoryState
* FocusedIndicator.Visible
* PlaceholderTextInstance.ColorCategoryState
* TextInstance.ColorCategoryState

<figure><img src="../../../../.gitbook/assets/02_18 20 39.png" alt=""><figcaption><p>Variables modified by TextBoxCategory states</p></figcaption></figure>

So to change the background color of the text box, we must select each of the categories under TextBoxCategory, then change the variable in each one. The following image shows how to change the TextBox's background to Black in the Enabled state.&#x20;

<figure><img src="../../../../.gitbook/assets/03_05 05 09.png" alt=""><figcaption><p>Changing TextBoxBackground to Black</p></figcaption></figure>

You may need to modify the other states under TextBoxCategory as well so the TextBox appears as desired in all states.

{% hint style="info" %}
At runtime a TextBox always applies one of its TextBoxCategory states. However, these states do not apply in the Gum tool. Usually any changes made to the Enabled state should also be made to the Default state so the TextBox appears the same at runtime and in the tool.
{% endhint %}

As a reminder be sure to always check if a variable is modified by any states. If it is, then the change should be made in those states. Otherwise, it should be made in the Default state.

## Adding New Colors



UNDER CONSTRUCTION
