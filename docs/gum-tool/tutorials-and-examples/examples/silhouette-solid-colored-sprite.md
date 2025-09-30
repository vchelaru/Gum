# Silhouette (Solid Colored Sprite)

## Introduction

Render targets and blend modes can be combined to create a silhouette of any shape. This example uses a Sprite with transparency. It uses the Bear.png file:

<figure><img src="../../../.gitbook/assets/Bear.png" alt=""><figcaption></figcaption></figure>

## Creating a Silhouette Component

To create a Component that can display a Silhouette, first create a new component named SilhouetteSprite.

Add a ColoredRectangle instance and a Sprite instance. The order matters - the Sprite should be drawn on top of the ColoredRectangle (it should show up 2nd in the Project tree view).

<figure><img src="../../../.gitbook/assets/image (203).png" alt=""><figcaption></figcaption></figure>

For this example, we will use the Alignment tab to adjust each item:

* SilhouetteSprite (main component) - Dock Size to Children
* ColoredRectangleInstance - Dock Fill

The Sprite should already be sized according to its source file, although you can change this size if desired.

The ColoredRectangleInstance defines the color of the silhouette so change it to a desired color.

Your component should look similar to the following image:

<figure><img src="../../../.gitbook/assets/29_22 10 26.png" alt=""><figcaption><p>SilhouetteSprite initial setup</p></figcaption></figure>

Next we'll adjust the transparency values. To do this:

1. Set SilhouetteSprite `Is Render Target` to `true`
2. Set SpriteInstance `Blend` to `MinAlpha`

Your sprite's silhouette should now appear, using the underlying color as its own color.

<figure><img src="../../../.gitbook/assets/29_22 12 37.png" alt=""><figcaption></figcaption></figure>
