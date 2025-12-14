# Styling

## Introduction

Gum components are built with full styling support. This tutorial shows how to perform styling on your components. It begins with simple styling by changing fonts and colors, but then gets into more advanced topics like creating your own style colors and customizing visuals.

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

<img src="../../../../.gitbook/assets/02_08 10 43.png" alt="" data-size="original">
{% endhint %}

We can change the color values in the Styles component by selecting any of the rectangles and changing their colors. The names suggest their purpose, so if you are changing the Primary color values, be sure to consider the Light and Dark suffixes. For example, we can change the teal colors to orange colors in Styles.

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

So to change the background color of the text box, we must select each of the categories under TextBoxCategory, then change the variable in each one. The following image shows how to change the TextBox's background to Black in the Enabled state.

<figure><img src="../../../../.gitbook/assets/03_05 05 09.png" alt=""><figcaption><p>Changing TextBoxBackground to Black</p></figcaption></figure>

You may need to modify the other states under TextBoxCategory as well so the TextBox appears as desired in all states.

{% hint style="info" %}
At runtime a TextBox always applies one of its TextBoxCategory states. However, these states do not apply in the Gum tool. Usually any changes made to the Enabled state should also be made to the Default state so the TextBox appears the same at runtime and in the tool.
{% endhint %}

As a reminder be sure to always check if a variable is modified by any states. If it is, then the change should be made in those states. Otherwise, it should be made in the Default state.

## Adding New Colors

The Styles component includes colors which cover the most common uses. Of course, as you are developing your project you may find that you need new colors which are not included in the Styles component. For example, the section above modified the TextBox to have a Black background. Instead, you might want to have a custom color for TextBoxes.

When adding a new color, it can be done by adding the color to the Styles component, or by directly referencing the color in the TextBox. We will cover both approaches here; however, it is recommended that you add colors to the Styles componenent. Even though it is a little more work, it is very helpful to have all of your colors referenced in a single spot rather than spread out throughout your project.

## Setting Color Directly on a Component

Color variables are ultimately no different than any other variable, so they can be directly assigned on a component. Of course, as mentioned above this approach can lead to color values being spread throughout your entire project making it harder to track them down later. This approach should only be used for very small projects, or to diagnose problems or run quick tests.

We will modify the variables on the TextBox again to show how to set the color directly. To do this:

1. Select the TextBox Background instance
2. Select the TextBoxCategory
3.  Click the X button to remove the Background.ColorCategoryState from all contained states - this means that we now intend to directly set the color on the background rather than using one of the pre-made colors\\

    <figure><img src="../../../../.gitbook/assets/03_05 44 52.png" alt=""><figcaption><p>Click X to remove the Background.ColorCategoryState variables</p></figcaption></figure>
4. Select the Enabled state
5. Directly assign the color on the background

<figure><img src="../../../../.gitbook/assets/03_05 47 21.gif" alt=""><figcaption><p>Setting color values on the TextBox's background</p></figcaption></figure>

As before, be sure to repeat this process for all of the other TextBoxCategory states. Also, be sure to assign the color value that you assigned on the Enabled state to the Default state as well.

As a reminder, the values assigned here will not automatically appear in the Styles component, so future changes require making changes directly on the TextBox.

## Adding New Colors to the Styles Component

As recommended above, keeping all colors in the Styles component can make it easier to maintain styles in the long run. This process requires slightly more work because we need to make modifications in multiple places. At a high level, the steps are:

1. Add a new **ColoredRectangle** to the Styles component
2. Create new states on the Standards which will use this color. These states will use variable references to stay up-to-date with any changes on the source Styles component rectangles. To use the color through the entire project we need to modify
   1. ColoredRectangle
   2. NineSlice
   3. Sprite
   4. Text
3. Update the **TextBox** (or any other component) to use this new color

### Adding a new ColoredRectangle to the Styles Component

First we'll define a new color in Styles. To do this:

1. Select an existing color
2. Copy/paste the rectangle (CTRL+C, CTRL+V)
3. Rename the new rectangle. Be sure to name the new color according to its use. In general you should avoid making the name the same as the color because the color may change in the future. Instead they can be generic names like "Error", or more specific names like "TextBoxBackground"
4. Set the desired color for the rectangle

<figure><img src="../../../../.gitbook/assets/03_05 58 09.png" alt=""><figcaption><p>TextBoxBackground color</p></figcaption></figure>

### Creating New Standard States

Next, we will add states to our Standard elements which reference this color. By adding new states, this new color appears in drop-downs, making it easy to select.

If the color is intended to be used across the entire app, then it should be referenced by all Standard elements which can display color. However, for this example we will only modify the NineSlice. As the name TextBoxBackground implies, this color is only intended to be used on TextBox backgrounds which are NineSlices.

To add a new state to the NineSlice:

1. Select the NineSlice standard
2. Expand ColorCategory
3.  Right-click on any of the existing states and select **Duplicate State**\
    \\

    <figure><img src="../../../../.gitbook/assets/03_06 05 36.png" alt=""><figcaption><p>Duplicate an existing NineSlice state</p></figcaption></figure>
4. Right-click on the new state and select **Rename State**
5. Enter the new name, which should match the color name we created earlier - **TextBoxBackground**

The new TextBoxBackground state already references the Styles component, but it still references the color of the state we duplicated. We can change this by replacing the name of the color with our new color.

In my case, the existing variable references are referencing the Accent color.

<figure><img src="../../../../.gitbook/assets/image (193).png" alt=""><figcaption><p>New state referencing old colors</p></figcaption></figure>

I can change these to instead reference the TextBoxBackground color. After these changes are made, press the tab key to apply them. The NineSlice should update its colors immediately.

<figure><img src="../../../../.gitbook/assets/03_06 12 04.png" alt=""><figcaption><p>NineSlice displaying the new TextBoxBackground color</p></figcaption></figure>

Now that we've set up `Variable References`, we can make changes to the color in our Styles component and those changes automatically apply to this new state.

### Using the New Color in TextBox

Now we can use this color on our TextBox. To do this:

1. Select the TextBox's Background
2.  If you have explicitly set colors in the previous section, select the TextBoxCategory and remove the explicitly-set Red, Green, and Blue variables\\

    <figure><img src="../../../../.gitbook/assets/03_06 20 15.png" alt=""><figcaption><p>Remove explicitly set color values from the Background</p></figcaption></figure>
3. Expand the TextBoxCategory state
4. Select the desired state, such as Enabled
5. Use the ColorCategory dropdown to select TextBoxBackground
6. Repeat the process for any other states that you would like to modify, including the Default state

<figure><img src="../../../../.gitbook/assets/03_06 22 10.png" alt=""><figcaption><p>Setting the Background to TextBoxBackground</p></figcaption></figure>

## Customizing NineSlice Visuals

The Gum Components used for forms controls (like TextBox and ButtonStandard) are built using standard elements such as Text and NineSlice. We can make any changes to these standard elements besides changing colors and fonts to further customize the appearance of our application.

Our ButtonStandard component uses a Background which is a NineSlice displaying the Bordered style. Remember, the ButtonStandard also uses the Primary color, so its color matches any changes you've made if you've been following along with this tutorial.

<figure><img src="../../../../.gitbook/assets/03_06 31 57.png" alt=""><figcaption><p>Bordered Style on ButtonStandard</p></figcaption></figure>

As mentioned above, it's best to check if a variable is modified by states before making changes. In this case it is not, so we are safe to make changes to this variable in the Default state.

<figure><img src="../../../../.gitbook/assets/03_06 33 27.png" alt=""><figcaption><p>Bordered Style is not modified by ButtonCategory</p></figcaption></figure>

We already have a number of styles that we can select for our button. Change the Style Category State to preview them.

<figure><img src="../../../../.gitbook/assets/03_06 37 55.gif" alt=""><figcaption><p>Change the Style Category to see different possible styles on ButtonStandard</p></figcaption></figure>

Just like our colors, these styles are defined on the NineSlice standard element. Specifically these styles define the texture coordinates for the NineSlice which references the standard UI png.

<figure><img src="../../../../.gitbook/assets/03_06 44 16.png" alt=""><figcaption><p>NineSlice style states change texture coordinates</p></figcaption></figure>

To make it easier to select, the UiSpritesheet.png aligns all of its art in 8 pixel increments. We can turn on snapping to see this in the Texture Coordinates tab.

<figure><img src="../../../../.gitbook/assets/03_06 48 20.png" alt=""><figcaption><p>Texture Coordinates window displaying 8-pixel grid</p></figcaption></figure>

This file can be viewed in explorer by clicking the folder button.

<figure><img src="../../../../.gitbook/assets/image (195).png" alt=""><figcaption><p>UISpriteSheet.png in folder</p></figcaption></figure>

This file is created by default when Forms components are added. The file in your project can be modified to add more styles. This file intentionally includes a lot of blank space so you can make changes for your own game.

Feel free to open this file and add more frames to be used by your NineSlice.

<figure><img src="../../../../.gitbook/assets/image (196).png" alt=""><figcaption><p>Blank space in UISpriteSheet.png</p></figcaption></figure>

Notice that most of the frames are white so that they can be colored in Gum using the pre-defined color values in the Styles component. If you intend for your borders to be dynamically colored, you should also use the white color. For example, a new rounded rectangle style can be added below the existing styles.

<figure><img src="../../../../.gitbook/assets/image (1) (1) (1) (1) (1).png" alt=""><figcaption><p>New rounded rectangle style</p></figcaption></figure>

Be sure to save your .png file so it shows up in Gum.

### Creating a new Style

1. Duplicate one of the existing style states in NineSlice
2. Rename the newly-created style as desired
3. Use the Texture Coordinate tab to select the new area of the sprite sheet

<figure><img src="../../../../.gitbook/assets/03_07 00 24 (1).png" alt=""><figcaption><p>New RoundedFilled style</p></figcaption></figure>

Once this style has been added, it can be referenced by any component using a NineSlice background, such as ButtonStandard.

<figure><img src="../../../../.gitbook/assets/03_07 02 29 (1).png" alt=""><figcaption><p>Button using RoundedFilled style</p></figcaption></figure>

{% hint style="info" %}
As mentioned above, white styles allow for dynamic coloring using the Styles component colors. If you would like to include the color as part of the border, you are free to do so, but this does limit your ability to set color explicitly. Whether you do this depends on your game's design, so as always feel free to experiment.
{% endhint %}

## Customizing Individual Component Instances

Component instances can be customized on an instance basis if the variable being customized is not associated with a state. For example, we can modify a button's Style Category State to change the text size.

To do this we must first expose the variable that should be available per instance. We can do this on the Button Standard by following these steps:

1. Expand the ButtonStandard component
2. Select TextInstance
3.  Right-click on the `Style State Category` variable and select **Expose Variable**\\

    <figure><img src="../../../../.gitbook/assets/03_21 23 37.png" alt=""><figcaption><p>Expose Variable right click option</p></figcaption></figure>
4. Enter an appropriate name, such as **TextStyle**

Now this variable can be accessed per-instance.

<figure><img src="../../../../.gitbook/assets/03_21 26 58.png" alt=""><figcaption></figcaption></figure>

## Customizing State Variables

At the time of this writing Gum does not support changing states values per-instance. Any changes to state variables, such as ButtonStandard's background, must be done at the component level. This means you must create a copy of the component and modify the states on the copy.

For example, we can create a copy of ButtonStandard by following these steps:

1. Select ButtonStandard
2. Press CTRL+C, CTRL+V to create a copy
3. Rename the new Button to ButtonDark
4. Select each of the states under ButtonCategory and change the background's color as desired

<figure><img src="../../../../.gitbook/assets/04_03 27 32.png" alt=""><figcaption><p>Change ButtonDark's Background color</p></figcaption></figure>
