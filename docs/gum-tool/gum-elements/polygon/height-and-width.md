# Height and Width

## Introduction

`Polygons` can be any shape, so their width and height values cannot be explicitly set. Instead, a `Polygon's` width and height values are determined by their points.

## Width and Height from Points

A `Polygon's` effective width and height is determined by its points. The furthest-right point determines the polygon's width and the furthest down point determines the polygon's height.

For example, a square `Polygon` with the furthest points at 128,128 has a width of 128 and a height of 128.

<figure><img src="../../../.gitbook/assets/10_15 48 29.png" alt=""><figcaption></figcaption></figure>

A `Polygon's` height can be used to affect parents, siblings, and children depending on the other instances' `Width Units`, `Height Units`, and stacking values.

For example, we can add a rectangle as a child of a `ColoredRectangle` to see how its points affect the size of the polygon.

<figure><img src="../../../.gitbook/assets/10_15 55 36.gif" alt=""><figcaption><p>Polygons in a ColoredRectangle. The ColoredRectangle sizes itself according to the Polygon's width and height</p></figcaption></figure>

We can see similar behavior if we place the Polygon in a stacking parent.



<figure><img src="../../../.gitbook/assets/10_16 10 42.gif" alt=""><figcaption></figcaption></figure>
