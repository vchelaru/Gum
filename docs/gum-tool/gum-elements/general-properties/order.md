# Order

### Introduction

Children in a Screen, Component, or parent instance are drawn top-to-bottom, so that children further down are drawn on top.&#x20;

<figure><img src="../../../.gitbook/assets/image (149).png" alt=""><figcaption><p>Red is the first item drawn, so it is under the other rectangles. Blue is the last, so it appears on top.</p></figcaption></figure>

### Changing Order

Gum provides a number of ways to reorder instances.&#x20;

Items can be right-clicked in the editor to change their order.

* Bring to Front - reorders the instance so that it is in front of all of its siblings.
* Move Forward - moves the instance in front of the sibling that is in front of it. In other words, moves the item forward by one index.
* Move In Front Of - moves the instance in front of the selected sibling.
* Move Backward - moves the instance behind the sibling that is currently behind it. In other words, moves the item backwards by one index.
* Send to Back - reorders the instance so that it is behind all of its siblings.

<figure><img src="../../../.gitbook/assets/01_05 35 51.gif" alt=""><figcaption><p>Right-click options can be used to reorder instances</p></figcaption></figure>

Items can be re-ordered in the Project tree view by holding the alt key and pressing up or down.

<figure><img src="../../../.gitbook/assets/01_05 39 57.gif" alt=""><figcaption><p>Alt+arrow keys can be used to reorder items</p></figcaption></figure>

### Parent and Children Ordering

Gum uses a _hierarchical ordering_ which means that a parent and all of its children draw before any of the siblings of the parent. For example, a container and all of its children draw before any other siblings of the container.

The following animation shows a container named ContainerInstance2 which draws on top of ContainerInstance1. All children of ContainerInstance2 also draw on top of children of ContainerInstance1.

<figure><img src="../../../.gitbook/assets/01_05 46 24.gif" alt=""><figcaption><p>If a parent draws on top of other instances, then its children also draw on top</p></figcaption></figure>

If a parent is reordered, then all of its children also respect the new order. For example, if ContainerInstance2 is sent to the back, then all of its children draw below ContainerInstance1 and its children.

<figure><img src="../../../.gitbook/assets/01_05 48 48.gif" alt=""><figcaption><p>If a parent is sent to the back, all children also draw behind other siblings of the parent container.</p></figcaption></figure>

### Order and Stacking / Grid

If a parent uses a stack or grid layout for its Children Layout variable, then the order of the children in the Project tab determines their order in the stack or grid.

For more information, see the [Children Layout](../container/children-layout.md) page.
