# Shape Properties

## Introduction

Shape properties are shared by the standard elements that draw vector shapes. Unlike [General Properties](../general-properties/README.md), which every Standard Element and Component shares, shape properties only appear on shape elements such as Circle and Rectangle.

These properties control how a shape is filled, whether it draws a gradient, and whether it casts a dropshadow:

* [Has Dropshadow](has-dropshadow.md)
* [Is Filled](is-filled.md)
* [Use Gradient](use-gradient.md)

The following element types expose these properties:

* **Circle** and **Rectangle** - the core (non-Skia) shape standard elements.
* **ColoredCircle**, **RoundedRectangle**, and **Arc** - Skia standard elements.
* **Line** supports Has Dropshadow and Use Gradient, but not Is Filled (a line draws as a stroke and has no fill).
