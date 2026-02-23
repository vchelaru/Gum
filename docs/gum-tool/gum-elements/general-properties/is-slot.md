# Is Slot

## Introduction

By default, when one instance (such as a NineSlice) is attached to another instance (such as a Container), the child attaches to the root of the parent. However, complex components often have internal structures. For example, a Window may have an InnerPanel which acts as the "landing zone" for children.

Slots allow you to designate specific internal parts of a component as an "attachment point". Any instance in a component can be designated as a slot by checking the `Is Slot` variable.

Note that instances can be marked as Default Slots for automatic parenting. For more information see the [Default Slot](../component/default-child-container.md) page.

## Example - Using Is Slot

Setting an instance's `Is Slot` to true is a way to tell Gum that it should be available as a parent. Any number of instances can be slots.

For example, consider a Component with three children: Header, Body, and Footer. In this case all three instances should be considered slots.

To set the value, select each instance and check `Is Slot`.

<figure><img src="../../../.gitbook/assets/23_05 25 43.png" alt=""><figcaption><p>Is Slot set to true</p></figcaption></figure>

If we have an instance of this HeaderBodyFooterComponent in a screen, then other instances can use any of the slots as their parent value. For example, the following screenshot shows a Text instance's available parents. Notice that all three slots are available.

<figure><img src="../../../.gitbook/assets/23_05 27 23.png" alt=""><figcaption><p>Available slots</p></figcaption></figure>

We can select any of these as the Text's parent.

<figure><img src="../../../.gitbook/assets/23_05 28 55.png" alt=""><figcaption><p>Text using a Footer slot</p></figcaption></figure>

