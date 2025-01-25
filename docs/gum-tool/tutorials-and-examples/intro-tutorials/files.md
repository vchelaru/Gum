# 3 - Files

## Introduction

Gum supports loading image files for Sprites and NineSlices. This tutorial discusses how to load files, and how they are referenced in Gum.

## Setting up a workspace

First we'll set up a workspace:

1. Create a Screen called SpriteScreen.
2. Drag+drop a Sprite into the newly-created Screen

<figure><img src="../../../.gitbook/assets/14_05 31 43.gif" alt=""><figcaption><p>Drag+drop a Sprite onto SpriteScreen</p></figcaption></figure>

## Setting the Sprite Source File

The `Source File` property is the image that the Sprite displays. Usually `Source Files` are of the .png file format. To set the source file:

1. Select  SpriteInstance
2. Find `Source File` in the Variables tab
3. Click the "..." button to bring up a file window
4. Navigate to the location of the file you would like to load
5. Click "Open" in the file window
6. Once `Source File` is set, the Sprite displays the image in the Editor tab

![SpriteInstance displaying a bear Source File](<../../../.gitbook/assets/14_05 46 12.png>)

If you select a file which is not located in the same folder or a sub folder of your gum project, Gum asks if you would like to reference the file in its original location or create a copy.&#x20;

<figure><img src="../../../.gitbook/assets/14_05 42 46.png" alt=""><figcaption><p>Gum asks to copy files if they are outside of the project directory</p></figcaption></figure>

Usually it's best to copy the file to the Gum project folder so that the Gum project can be moved to different computers without breaking file references.

## Texture Coordinates

Sprites can display portions of their `Source File`. Files which combine multiple images are often called _sprite sheets_ or _tile sheets_, and are commonly used in game development to keep art organized and to improve performance.

For example, the following file contains images for an animated character, ground tiles, and other entities for a platformer game.

{% embed url="https://raw.githubusercontent.com/flatredball/FlatRedBallMedia/refs/heads/master/Art%20Kits/FRBeefcake/FRBeefcakeSpritesheet.png" %}

If we download this file and set it as our , then the sprite displays the entire file.

<figure><img src="../../../.gitbook/assets/14_05 52 53 (1).png" alt=""><figcaption><p>Sprite displaying entire sprite sheet</p></figcaption></figure>

We can display a portion of the Sprite rather than the entire file:

1. Click on the Texture Coordinates tab
2. Check the **Snap to grid** option to make it easier to select a region
3. Double-click anywhere on the image to select a region around the cursor
4. Move the selected region to the desired location to adjust the sprite's texture coordinate values

<figure><img src="../../../.gitbook/assets/14_06 06 03.gif" alt=""><figcaption><p>Texture coordinate values can be adjusted in the Texture Coordinates tab</p></figcaption></figure>
