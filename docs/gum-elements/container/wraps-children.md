# Wraps Children

The _Wraps Children_ property controls whether children wrap or stack beyond their container's boundaries when the container's [Children Layout](children-layout.md) is set to Top to Bottom Stack or Left to Right Stack.

![Wraps children makes children wrap when using either Top to Bottom Stack or Left to Right Stack.](<../../.gitbook/assets/04\_19 58 31.gif>)

If a parent has Wraps children set to true, the wrapping adjusts in response to resizing the parent.

<figure><img src="../../.gitbook/assets/04_20 02 33.gif" alt=""><figcaption><p>Resizing a parent can change wrapping</p></figcaption></figure>

Similarly, resizing a child may result in the stacking changing.

<figure><img src="../../.gitbook/assets/04_20 05 40.gif" alt=""><figcaption><p>Resizing children can change wrapping</p></figcaption></figure>

The row height in a Left to Right Stack is determined by the largest child in the row.

<figure><img src="../../.gitbook/assets/04_20 07 48.gif" alt=""><figcaption><p>Height of each item in the row determines row height</p></figcaption></figure>

Similarly, column width in a Top to bottom Stack is determined by the largest child in the column.

<figure><img src="../../.gitbook/assets/04_20 09 32.gif" alt=""><figcaption><p>Width of each item in the column determines column width</p></figcaption></figure>

