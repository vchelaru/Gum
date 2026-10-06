# Wraps Children

The _Wraps Children_ property controls whether children wrap or stack beyond their container's boundaries when the container's [Children Layout](children-layout.md) is set to Top to Bottom Stack or Left to Right Stack.

![Wraps children makes children wrap when using either Top to Bottom Stack or Left to Right Stack.](<../../../.gitbook/assets/04_19 58 31.gif>)

If a parent has Wraps children set to true, the wrapping adjusts in response to resizing the parent.

<figure><img src="../../../.gitbook/assets/04_20 02 33.gif" alt=""><figcaption><p>Resizing a parent can change wrapping</p></figcaption></figure>

Similarly, resizing a child may result in the stacking changing.

<figure><img src="../../../.gitbook/assets/04_20 05 40.gif" alt=""><figcaption><p>Resizing children can change wrapping</p></figcaption></figure>

The row height in a Left to Right Stack is determined by the largest child in the row.

<figure><img src="../../../.gitbook/assets/04_20 07 48.gif" alt=""><figcaption><p>Height of each item in the row determines row height</p></figcaption></figure>

Similarly, column width in a Top to bottom Stack is determined by the largest child in the column.

<figure><img src="../../../.gitbook/assets/04_20 09 32.gif" alt=""><figcaption><p>Width of each item in the column determines column width</p></figcaption></figure>

## Positioning Children Within a Row or Column

In a wrapping stack, each row (in a `Left to Right Stack`) or column (in a `Top to Bottom Stack`) acts as the parent of its children on the other axis. A child's `Y Units` and `Y Origin` in a `Left to Right Stack`, or its `X Units` and `X Origin` in a `Top to Bottom Stack`, position it within its row or column instead of within the whole container:

* `Pixels From Center` with a `Center` origin centers the child in its row or column.
* `Pixels From Bottom` (or `Pixels From Right`) with a `Bottom` (or `Right`) origin aligns the child to the far edge of its row or column. `Pixels From Baseline` uses the bottom of the row.
* `Percentage` places the child at a percentage of its row or column's size.
* `Pixels From Top` (or `Pixels From Left`) places the child relative to the start of its row or column.

A row or column is as large as its largest child, and rows and columns stay packed at the start of the container. They do not stretch to fill extra space. Because of this, a child alone in its row is the same size as its row, so centering it or aligning it to the far edge leaves it at the start of the row.

A row counts each child the same way a container with `Height Units` of `Relative to Children` does (see [Ignored Width Values](../general-properties/width-units.md#ignored-width-values)):

* An `X` or `Y` value counts from the edge it is measured from. For example, a child placed 10 pixels up from the bottom of its row makes the row 10 pixels taller than the child.
* A portion of a child placed outside its row does not make the row larger.
* A child positioned with `Percentage` does not count toward its row's size.
* A child whose size depends on its parent, such as `Percentage of Parent`, does not count toward its row's size.

Size units are unaffected. A child with `Height Units` of `Percentage of Parent` or `Relative to Parent` still sizes itself from the whole container, not its row.

Stacks that do not wrap position children on the other axis within the whole container.

{% hint style="warning" %}
**Breaking change in November 2026:** Before this version, a child in a wrapping stack was positioned within the whole container on the other axis, so a centered child was centered in the container rather than in its row or column. Available in November 2026, or now if building Gum from source. For more information see [Migrating to 2026 November](../../upgrading/migrating-to-2026-november.md).
{% endhint %}

## Wraps Children and Width Units

Wrapping of children can only be performed if the parent's size does not depend on its children (see more info below). If the parent's size does depend on its children, then the parent will expand to fit is children so wrapping will not occur.

If a parent container's Width Units is set to Relative to Children, then it adjusts in response to children size and positioning, so wrapping will not occur.

<figure><img src="../../../.gitbook/assets/18_05 54 32.gif" alt=""><figcaption><p>Stacking cannot occur if the parent uses a Width Units of Relative To Children</p></figcaption></figure>

A parent can use the following `Width Units` and `Height Units` with children wrapping:

* ✅Absolute
* ✅Percentage of Parent
* ✅Ratio of Parent
* ✅Percentage of Width/Height
* ✅Absolute Multiplied by Font Scale

A parent does not wrap its children if it uses:

* ❌Relative to Children (see below)

Note that Relative to Children can be used on the non-stacking axis. For example, if a parent uses `Left to Right Stack`, then it can still have its `Height Units` set to `Relative to Children`.

<figure><img src="../../../.gitbook/assets/18_05 58 33.gif" alt=""><figcaption><p>Left to Right Stack with Height Units set to Relative to Children</p></figcaption></figure>

## Relative to Children and Max Width and Max Height

If a container has a non-null Max Width, then it will expand according to its children until it reaches its max width. Once it reaches a max width, it wraps its children.

The following animation shows a container which has:

* Width Units of Relative to Children
* Max Width of 400
* Children Layout of Left to Right Stack
* Wraps Children set to True

<figure><img src="../../../.gitbook/assets/16_09 21 11.gif" alt=""><figcaption><p>Wraps Children allowed when max width is set</p></figcaption></figure>
