# Max Width

## Introduction

The `Max Width` variable sets the maximum width in pixels. The value is applied after all other layout so it can be used to overwrite automatically-assigned width.

By default this value is `<NULL>` which means there is no `Max Width`.

<figure><img src="../../../.gitbook/assets/13_06 38 36.gif" alt=""><figcaption><p>A ColoredRectangle with no <code>Max Width</code></p></figcaption></figure>

If `Max Width` is assigned (not `<NULL>`), then the effective width cannot be larger than the `Max Width` value. The following animation shows that width is limited to a `Max Width` of `100`.

<figure><img src="../../../.gitbook/assets/13_06 40 31.gif" alt=""><figcaption><p><code>Max Width</code> set to <code>100</code> limits the ColoredRectangle's width</p></figcaption></figure>

Notice that the `Width` variable can still be set to a value larger than `Max Width`, but it does not apply visually.

`Max Width` can also be used if `Width Units` is set to values other than `Absolute`. For example, `Max Width` can be used to limit the width of a ColoredRectangle when `Width Units` is `Relative to Parent`.

<figure><img src="../../../.gitbook/assets/13_06 45 03.gif" alt=""><figcaption><p>ColoredRectangle's width limited by its <code>Max Width</code> of <code>100</code></p></figcaption></figure>

## Max Width and Relative to Children

Note that Max Width can prevent a container from growing according to its children. For more information, see the [Relative to Children Width Units page](width-units.md#relative-to-children).

A container with a `Left to Right Stack` grows to its `Max Width` when its children need more space. The child that crosses the max extends past the container's right edge. If the container also has `Wraps Children` checked, it stops at the right edge of the last child that fits, since the next child moves to a new row. For more information see the [Children Layout](../container/children-layout.md#stacking-and-container-height-units-and-width-units) page.

{% hint style="warning" %}
**Breaking change in November 2026:** Before this version, a stack without `Wraps Children` stopped growing at the last child that fit under its max, the same as a wrapping stack, so it could end up smaller than its max. Available in November 2026, or now if building Gum from source. For more information see [Migrating to 2026 November](../../upgrading/migrating-to-2026-november.md).
{% endhint %}
