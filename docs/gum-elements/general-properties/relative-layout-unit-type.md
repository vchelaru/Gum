---
title: Relative Layout Unit Type
---

# Relative Layout Unit Type

## Introduction

When a parent uses **RelativeToChildren** for its for its **Width Units** or **Height Units**, it must only consider children which are not positioned relative to the parents' Width or Height, respectively.

For the sake of brevity, this document will discuss **Width**, **Width Units**, **X**, and **X Units** values. All concepts also apply to **Height**, **Height Units**, **Y**, and **Y Units**.

## Gum Prevents Recursive

Conceptually, parent **Width Units** and children **X Units** can create recursive relationships. For example, consider the following situation:

* Parent.WidthUnits = RelativeToChildren
* Child.XUnits = Percentage
* Child.Width = 100
* Child.X = 50

In this case the **Parent** width depends on the **Child** **X** and **Width** values. But the **Child** **X** depends on the **Parent** **Width**. This creates a recursive relationship which cannot be resolved (or which can be resolved with unexpected results).

Therefore, when determining the **Width** of a parent using **RelativeToChildren** **WidthUnits**, all children which have position or size values relative to their parent are ignored.

Specifically, the following values are considered absolute, and any values besides these will result in a child being ignored when calculating a relative parent **Width**:

* WidthUnits = Absolute, PercentageOfSourceFile, RelativeToChildren
* XUnits = PixelsFromLeft, PixelsFromMiddle, PixelsFromRight

Only the following values are considered absolute, and any values besides these will result in a child being ignored when calculating a relative parent **Height**:

* HeightUnits = Absolute, PercentageOfSourceFile, RelativeToChildren
* YUnits = PixelsFromTop, PixelsFromMiddle, PixelsFromBottom

Width and Height are calculated independently. This means that a child may be ignored when calculating **RelativeToChildren Width**, but may be considered when calculating **RelativeToChildren Height**.

### Examples

To help explain the way units work, this section two ColoredRectangles. Other types such as Container could be used, but colored rectangle is easier to see in pictures and animations so we'll use this type. Also, like the section above this section uses X and Width values, but the same concepts apply to Y and Height values.

Initially the Parent has a Width of 0 and a Width Units of **Relative to Children**.

<figure><img src="../../.gitbook/assets/21_06 05 57.png" alt=""><figcaption><p>Parent with Width of 0 and Width Units of Relative to Children</p></figcaption></figure>

This results in the Parent's width being controlled by the _right side_ of the Child (the red ColoredRectangle). In other words, if the Child is moved or if its Width changes, the Parent's Width also changes.

<figure><img src="../../.gitbook/assets/21_06 07 43.gif" alt=""><figcaption><p>Child changing the width of Parent when it is moved or resized</p></figcaption></figure>

In this case, the Parent's Width depends on its Child - specifically the _right side_. The Child's right side is absolute - it does not depend on the parent, so this relationship is not circular. However, if we were to adjust the Child so that its **X Units** depended on the parent's width, such as by changing **X Units** to **Pixels from Right,** the child would now depend on the Parent's width, creating a circular relationship. We can see that once this circular relationship is established, the parent's actual width changes to 0.

<figure><img src="../../.gitbook/assets/21_06 27 25.gif" alt=""><figcaption><p>Parent Width changing to 0</p></figcaption></figure>

To reiterate, after this change, the child's right side depends on the parent's right side, and the parent's right side depends on the child's right side. This circular dependency is resolved by Gum with the parent ignoring the child - in this case the Parent behaves as if the child does not exist when it determines its actual width. This results in the Parent's width depending on no children, so it is set to 0 (the value set on the Parent's width).

These types of circular dependencies can cause confusion but they are okay to use in actual projects. Typically these types of situations exist when a Parent has a mix of children - some of which use absolute positioning, some of which depend on their parent.

For example, we can add a second child - a yellow ColoredRectangle maned Child2. If Child2's right side is absolute, then the Parent will use it to determine its size. Notice that when this happens, the red Child ColoredRectangle adjusts appropriately.

<figure><img src="../../.gitbook/assets/21_06 39 56.gif" alt=""><figcaption><p>Child2 using absolute X and Width, so it adjusts Parent Width</p></figcaption></figure>

The properties are applied in the following order:

1. Child2's right side is determined using its absolute X and Width values
2. Parent's Width value is set according to Child2's right side. Parent ignores Child (the red box) because Child's right side depends on Parent's right side.
3. Child sets its X according to Parent's right side

A practical example of how this type of relationship might be used is a situation where Parent contains objects that should control its size, but also has elements which react to the size such as a top bar.

<figure><img src="../../.gitbook/assets/21_06 39 09.gif" alt=""><figcaption><p>Child as a top bar, adjusting in response to Parent's width which is controlled by Child2</p></figcaption></figure>
