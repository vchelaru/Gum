# Padding

## Introduction

The concept of padding is often used to add spacing between the edge of a container and its children. Padding can be achieved by adding sub-containers.

## Creating a Container

First we'll create a top-level container. This container controls the size of all objects internally, including the background. We will also include a background object which is sized according to the container. To keep things simple, this example uses a dark blue ColoredRectangle.

The ColoredRectangle is set so its size matches its parent.

<figure><img src="../../../.gitbook/assets/15_06 57 25.png" alt=""><figcaption><p>Container with a blue background</p></figcaption></figure>

Next we can add another container to the top-level container. By default this container sits at the top-left of the parent when it is added with a drag+drop.

<figure><img src="../../../.gitbook/assets/15_06 58 37.gif" alt=""><figcaption><p>Container added with drag+drop</p></figcaption></figure>

To have the container fill its parent, but also include padding:

1. Select the inner container
2. Click the Alignment tab
3. Enter the desired padding in the Margin text box
4. Click the Dock Fill button

<figure><img src="../../../.gitbook/assets/15_07 00 18.png" alt=""><figcaption><p>Creating padding by havign an internal continer use Margin of 10</p></figcaption></figure>

This inner container can now be used to hold all children. Note that if you are creating a Component and you want to make this be the default container for children, you may want to set the Default Child Container to this inner container. For more information see the [Default Child Container](../../gum-elements/component/default-child-container.md) page.
