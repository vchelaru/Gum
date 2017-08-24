---
title: Relative Layout Unit Type
---

# Introduction

When a parent uses **RelativeToChildren** for its for its **Width Units** or **Height Units**, it must only consider children which are not positioned relative to the parents' Width or Height, respectively.

For the sake of brevity, this document will discuss **Width**, **Width Units**, **X**, and **X Units** values. All concepts also apply to **Height**, **Height Units**, **Y**, and **Y Units**.

# Gum Prevents Recursive

Conceptually, parent **Width Units** and children **X Units** can create recursive relationships. For example, consider the following situation:

* Parent.WidthUnits = RelativeToChildren
* Child.XUnits = Percentage
* Child.Width = 100
* Child.X = 50

In this case the **Parent** width depends on the **Child** **X** and **Width** values. But the **Child** **X** depends on the **Parent** **Width**. This creates a recursive relationship which cannot be resolved (or which can be resolved with unexpected results).

Therefore, when determining the **Width** of a parent using **RelativeToChildren** **WidthUnits**, all children which have position or size values relative to their parent are ignored.

Specifically, the following values are considered absolute, and any values besides these will result in a child being ignored when calculating a relative parent **Width**:

* WidthUnits = Absolute, PercentageOfSourceFile, RelativeToChildren
* XUnits = PixelsFromLeft,  PixelsFromMiddle, PixelsFromRight

Only the following values are considered absolute, and any values besides these will result in a child being ignored when calculating a relative parent **Height**:

* HeightUnits = Absolute, PercentageOfSourceFile, RelativeToChildren
* YUnits = PixelsFromTop, PixelsFromMiddle, PixelsFromBottom

Width and Height are calculated independently. This means that a child may be ignored when calculating **RelativeToChildren Width**, but may be considered when calculating **RelativeToChildren Height**.