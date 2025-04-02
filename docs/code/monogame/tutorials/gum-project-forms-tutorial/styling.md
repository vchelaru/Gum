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

We can also modify the fonts used in our project by changing the Text instances in the Styles component.



UNDER CONSTRUCTION
