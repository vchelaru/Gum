# Max Height

## Introduction

The `Max Height` variable sets the maximum height in pixels. This value is applied after all other layout so it can be used to overwrite automatically-assigned height.

By default this value is `<NULL>` which means there is no `Max Height`.

<figure><img src="../../../.gitbook/assets/13_06 25 05.gif" alt=""><figcaption><p>A ColoredRectangle with no <code>Max Height</code></p></figcaption></figure>

If `Max Height` is assigned (not `<NULL>`), then the effective height cannot be larger than the `Max Height` value. The following animation shows that height is limited to a `Max Height` of `100`.

<figure><img src="../../../.gitbook/assets/13_06 25 49.gif" alt=""><figcaption><p><code>Max Height</code> set to <code>100</code> limits the ColoredRectangle's height</p></figcaption></figure>

Notice that the `Height` variable can still be set to a value larger than the `Max Height`, but it does not apply visually.

`Max Height` can also be used if `Height Units` is set to values other than `Absolute`. For example, `Max Height` can be used to limit the height of a ColoredRectangle when `Height Units` is `Relative to Parent`.

<figure><img src="../../../.gitbook/assets/13_06 30 56.gif" alt=""><figcaption><p>Rectangle's height limited by its <code>Max Height</code> of <code>100</code></p></figcaption></figure>

## Max Height and Relative to Children

Note that Max Height can prevent a container from growing according to its children. For more information, see the [Relative to Children Height Units page](height-units.md#relative-to-children).

A container with a `Top to Bottom Stack` grows to its `Max Height` when its children need more space. The child that crosses the max extends past the container's bottom edge. If the container also has `Wraps Children` checked, it stops at the bottom edge of the last child that fits, since the next child moves to a new column. For more information see the [Children Layout](../container/children-layout.md#stacking-and-container-height-units-and-width-units) page.

{% hint style="warning" %}
**Breaking change in November 2026:** Before this version, a stack without `Wraps Children` stopped growing at the last child that fit under its max, the same as a wrapping stack, so it could end up smaller than its max. Available in November 2026, or now if building Gum from source. For more information see [Migrating to 2026 November](../../upgrading/migrating-to-2026-november.md).
{% endhint %}
