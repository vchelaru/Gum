---
description: Introduction
---

# Health Bar

## Introduction

Health bars are common UI elements in games. They are similar to progress bars, so this example could be used to create either. This example explains how to create a health bar component.

## Creating the Component

First we'll define the component:

1. Open Gum
2. Open or create a new Gum project
3. Right-click on the **Components** folder
4. Name the component "HealthBar"
5. Resize the **HealthBar** component so it is wider than it is tall. For example, assign `Width` to `200` and `Height` to `32`.

## Adding a Background

Next we'll add a background to our **HealthBar** Component

1.  Drag+drop a **ColoredRectangle** into the **HealthBar**\


    <figure><img src="../../../.gitbook/assets/image (5) (1) (2).png" alt=""><figcaption><p>Add ColoredRectangle to HealthBar</p></figcaption></figure>
2. Select the newly-created **ColoredRectangleInstance**
3. Select the **Alignment** tab
4.  Click the **Fill Dock** button\


    <figure><img src="../../../.gitbook/assets/image (7) (2).png" alt=""><figcaption><p>Fill Dock to make the background take up the entire size of its parent HealthBar component</p></figcaption></figure>
5.  Change the ColoredRectangleInstance color to black\


    <figure><img src="../../../.gitbook/assets/image (11) (2) (1).png" alt=""><figcaption></figcaption></figure>

Now we have a black background in our **HealthBar**

## Creating an Inner Container

The HealthBar displays its current health with another rectangle. This second rectangle will be contained inside a container to provide a boundary. To add an inner container:

1. Drag+drop a **Container** onto the **HealthBar**
2. Select the **Alignment** tab
3. Enter a **Margin** value of 4
4. Click the **Fill Dock** button

<figure><img src="../../../.gitbook/assets/image (156).png" alt=""><figcaption><p>Set Margin to 4, then click the Fill Dock button</p></figcaption></figure>

Now we have a ContainerInstance with the proper margin

<figure><img src="../../../.gitbook/assets/image (6) (2) (1).png" alt=""><figcaption></figcaption></figure>

## Adding the Foreground Rectangle

Finally we'll add the foreground rectangle which displays the health:

1. Drag+drop another ColoredRectangle onto the ContainerInstance. Be sure to drop it on ContainerInstance so that the newly-added ColoredRectangle is a child of the container
2. Click the Alignment tab
3. Set Margin back to 0
4. Click the Fill Dock button
5. Change the following values:
   1. X Units to Pixels from Left
   2. X Origin to Left
   3. Width to Percentage of Container
   4. Width to 100

Now, the Width value can change between 0 and 100 to indicate the health percentage.

<figure><img src="../../../.gitbook/assets/image (8) (2).png" alt=""><figcaption></figcaption></figure>

## Expose Width&#x20;

Next we'll expose the inner ColoredRectangle's `Width` property so it can be assigned per HealthBar instance:

1. Select the inner ColoredRectangle instance
2.  Right-click on its `Width` variable and select **Expose Variable**\


    <figure><img src="../../../.gitbook/assets/image (157).png" alt=""><figcaption><p>Click Expose Variable</p></figcaption></figure>
3. Enter an appropriate name such as "Percentage" and click OK

Now we can add instances of the HealthBar to a screen and control its fill percentage.

<figure><img src="../../../.gitbook/assets/16_05 29 45.gif" alt=""><figcaption><p>Percentage value updated on a ScrollBar instance</p></figcaption></figure>

\
