# Ignored By Parent Layout

### Introduction

`Ignored By Parent Layout` determines whether the parent's [Children Layout](../container/children-layout.md) places an instance. When true, the instance is positioned by its own `X`, `Y`, units and origin as if the parent's `Children Layout` were `Regular`. Its siblings stack or fill grid cells as if it were not there, so it takes no slot, no `Stack Spacing` and no grid cell.

This value has no effect when the parent's `Children Layout` is `Regular`.

{% hint style="info" %}
Available in December 2026, or now if building Gum from source.
{% endhint %}

### Example: Outline Around a Stacked Component

A component named `Unit` uses `Top To Bottom Stack` and contains an image and a health bar. To draw an outline around the whole unit, add a Rectangle to `Unit` and set:

* `Width Units` and `Height Units` to `Relative To Parent`, with `Width` and `Height` of `0`
* `Ignored By Parent Layout` to true

The outline covers the whole unit, and the image and health bar stack exactly as they did before the outline was added.

### Parent Size

`Ignored By Parent Layout` does not change whether the parent counts the instance when it sizes itself to its children. The parent counts it the same way a `Regular` parent counts a child, from its position and size. To leave it out of the parent's size, set [Ignored By Parent Size](ignored-by-parent-size.md) to true.
