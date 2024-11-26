# 1 - The Basics

## Introduction

This page walks you through the basics of using the Gum UI tool (which we'll refer to simply as Gum for this and all other documentation).

<figure><img src="../.gitbook/assets/image (10) (3).png" alt=""><figcaption><p>Empty Gum Project</p></figcaption></figure>

## Gum Elements

Gum separates its elements into three categories: Screens, Components, and Standard (Behaviors are an advanced topic that we'll skip for these tutorials):&#x20;

<figure><img src="../.gitbook/assets/image (15) (1).png" alt=""><figcaption><p>Screens, Components, and Standard folders</p></figcaption></figure>

Standard elements represent the building-blocks for screens and components, and all projects use the same set of standard elements. To see the list of elements, expand the Standard tree item. Clicking on any element will display it in the preview window.

<figure><img src="../.gitbook/assets/image (5) (2).png" alt=""><figcaption></figcaption></figure>

Sprite element is selected in the image above. Notice that since a **SourceFile** is not set, the Sprite renders as a red X.

### Standard Types

* ![](<../.gitbook/assets/image (51).png>) Circle - circle outline. These are usually not used for UI, but can be used if you are defining collision in your Gum objects for a game.
* ![](<../.gitbook/assets/image (53).png>) ColoredRectangle - filled-in rectangle. These are often used for solid-colored backgrounds and frames.
* ![](<../.gitbook/assets/image (54).png>) Container - invisible object used to contain other objects. These are used to provide margins, change layouts (such as vertical vs horizontal stacking), and to organize your UI.
* ![](<../.gitbook/assets/image (55).png>) NineSlice - visual object which uses nine sprites to create a resizable object from a source PNG (or portion of a PNG). The corner sprites (4) are not resized. The top, bottom, left, and right sprites are stretched on one axis. The middle sprite stretches both horizontally and vertically. These are used to create resizable frames.
* ![](<../.gitbook/assets/image (56).png>) Polygon - polygon outline which can have any number of points. These are usually not used for UI, but can be used if you are defining collision in your Gum objects for a game.
* ![](<../.gitbook/assets/image (57).png>) Rectangle - rectangle outline. These can be used for single-line frames or if you are defining collision in your Gum objects for a game.
* ![](<../.gitbook/assets/image (58).png>) Sprite - a visual object which displays a source PNG (or a portion of a PNG). These are used for icons, backgrounds, and other visual objects which are usually not resized dynamically.
* ![](<../.gitbook/assets/image (59).png>) Text - a visual object which can display charcters. These are used for any situation where text needs to be displayed such as labels and paragraphs.

Note that through plugins the Standard list can be expanded. For example, Gum supports additional standard types through a Skia plugin.

## Components

Components are objects which can contain standard elements and instances of other components. Components can be very simple, such as a Label, or very complex, such as an options menu with dozens of items.

### Screens

Screens are objects which can contain standard elements and instances of other components. Unlike Components, Screens cannot be added to other Screens. Screens exist mainly for organization.

## Components vs. Screens

Components and screens are similar - both can contain instances of standard elements, and both can contain other components. The only difference between screens and components is that screens cannot contain other screens.

You can think of screens like a screen in a video game. Examples include a main menu, credits screen, options screen, and level selection screen. You can think of components as elements which are composed of multiple standard elements. Examples include a Button component which is made up of a Sprite instance and a Text instance, or a Logo component which may be made up of multiple Sprites and Text objects.

## Creating a Screen

To create a screen:

1.  Right-click on the Screens tree item and select **Add Screen**

    <figure><img src="../.gitbook/assets/image (9) (2).png" alt=""><figcaption><p>Right-click, Add Screen option</p></figcaption></figure>
2. Enter the name of the new screen - such as **MainMenu**
3.  The newly-created screen is created and selected

    <figure><img src="../.gitbook/assets/image (3) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>The MainMenu screen in Gum</p></figcaption></figure>

## Adding instances

Instances of standard and component elements can be added to screens and components. To add an instance:

1. Select the destination screen or component. For example, select the **MainMenu** screen
2. Push the left mouse button (but don't release it) on the Text item. If you happen to release the mouse button, this will select the Text item, so you need to re-select the destination (MainMenu).
3. Drag the Text item onto the preview window
4.  Release the mouse button. A new text instance appears in your screen.

    <figure><img src="../.gitbook/assets/02_20 51 44.gif" alt=""><figcaption><p>Adding a Text instance to the MainMenu screen</p></figcaption></figure>

## Editing in the preview window

Once an instance is a part of a screen or component it can be edited visually in the preview window. The selected instance has eight (8) handles surrounding it. These are called the _resize handles_ and can be used to change the selected instance's width and height.

<figure><img src="../.gitbook/assets/image (60).png" alt=""><figcaption><p>A selected Text instance with resize handles</p></figcaption></figure>

In the case of the Text object, the resize handles are used to control how the text object performs line wrapping.

You can use the resize handles to resize the instance, or you can simply push the mouse button and drag inside the instance to change its position. Notice that an object's outline is displayed when the cursor is hovering over the instance.

<figure><img src="../.gitbook/assets/03_09 10 50.gif" alt=""><figcaption><p>Moving and resizing a Text object.</p></figcaption></figure>
