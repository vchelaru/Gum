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

## Wraps Children and Width Units

Wrapping of children can only be performed if the parent's size does not depend on its children. If the parent's size does depend on its children, then the parent will expand to fit is children so wrapping will not occur.

If a parent container's Width Units is set to Relative to Children, then it adjusts in response to children size and positioning, so wrapping will not occur.

<figure><img src="../../../.gitbook/assets/18_05 54 32.gif" alt=""><figcaption><p>Stacking cannot occur if the parent uses a Width Units of Relative To Children</p></figcaption></figure>

A parent can use the following `Width Units` and `Height Units` with children wrapping:

* ✅Absolute
* ✅Percentage of Parent
* ✅Ratio of Parent
* ✅Percentage of Width/Height
* ✅Absolute Multiplied by Font Scale

A parent does not wrap its children if it uses:

* ❌Relative to Children

Note that Relative to Children can be used on the non-stacking axis. For example, if a parent uses `Left to Right Stack`, then it can still have its `Height Units` set to `Relative to Children`.

<figure><img src="../../../.gitbook/assets/18_05 58 33.gif" alt=""><figcaption><p>Left to Right Stack with Height Units set to Relative to Children</p></figcaption></figure>
