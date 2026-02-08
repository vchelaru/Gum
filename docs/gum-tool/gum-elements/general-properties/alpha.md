# Alpha

## Introduction

`Alpha` controls an instance's transparency. A fully opaque instance has an `Alpha` of 255. A fully transparent instance has an `Alpha` of 0.

<figure><img src="../../../.gitbook/assets/image (172).png" alt=""><figcaption><p>Sprites with Alpha of 255, 200, 150, 100, 50, and 0</p></figcaption></figure>

An object's transparency is a combination of its `Alpha`, [Blend](blend.md), and its [Source File](../sprite/source-file.md). Skia elements may also have transparent portions due to their shape (such as [ColoredCircle](../skia-standard-elements/coloredcircle.md) and [RoundedRectangle](../skia-standard-elements/roundedrectangle/)) as well as [dropshadows](../skia-standard-elements/general-properties/has-dropshadow.md).

## Alpha and Children

By default the Alpha property affects the selected instance only - it does not cascade down to its children. For example, the following shows a parent white ColoredRectangle with a child blue ColoredRectangle. If the white ColoredRectangle's `Alpha` property changes, the BlueRectangle's opacity does not change.

<figure><img src="../../../.gitbook/assets/10_10 02 03.gif" alt=""><figcaption><p>Parent Alpha does not change child opacity</p></figcaption></figure>

A parent can affect its children's transparency if the parent is a container with `Is Render Target` set to true. For example, if the white rectangle is added to a Container, the Container can make its entire contents transparent.

<figure><img src="../../../.gitbook/assets/11_05 30 25.gif" alt=""><figcaption><p>Entire Container Alpha makes all children transparent</p></figcaption></figure>

Note that by setting Is Render Target to true, the entire container's Alpha can be adjusted rather rather than the alpha value cascading to each individual child. This Alpha value is used to control transparency _after_ all children have been drawn. We can see the difference between a partially-transparent Container and each child individually being made partially transparent by overlapping two children ColoredRectangles.

The rectangles on the left each have an `Alpha` value of `255`. These rectangles are in a Container `Is Render Target` set to true and an `Alpha` set to `128`.

The rectangles on the right each have an `Alpha` of `128`, so the red rectangle is visible behind the blue rectangle.

<figure><img src="../../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Container Alpha on the left, individual Alpha on the right</p></figcaption></figure>
