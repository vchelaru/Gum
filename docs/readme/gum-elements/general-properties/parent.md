# Parent

### Introduction

The `Parent` variable allows UI elements to be positioned and sized according to other UI elements. Parenting hierarchies can go many levels deep and the parent/child relationship can be visualized by the white line connecting the parent to the child when the child is selected.

### Parents and Units

Parents control control the position of their children. Parents can also control the size of their objects depending on the child's `Width Units` and `Height Units`.

For example, if a parent is moved, its children move along with it. For more information on positioning children, see the [X Units](x-units.md) and [Y Units](y-units.md) pages.

<figure><img src="../../../.gitbook/assets/07_06 43 54.gif" alt=""><figcaption><p>Moving a parent also moves its children</p></figcaption></figure>

Children can also be sized according to their parents. For more information on sizing children according to their parent, see the [Width Units](width-units.md) and [Height Units](height-units.md) pages.

<figure><img src="../../../.gitbook/assets/07_06 45 58.gif" alt=""><figcaption><p>Children can be resized according to their parent</p></figcaption></figure>

### Children Outside of Parent Bounds

Children can be placed outside of their parent's bounds. In the simplest case, a child can be dragged outside of its parent's bounds in the editor.

<figure><img src="../../../.gitbook/assets/07_06 48 01.gif" alt=""><figcaption><p>Children can be placed outside of their parents' bounds</p></figcaption></figure>

Children outside of their parent's bounds still follow all of the same rules for sizing and positioning, but there are some important things to keep in mind.

Children placed outside of a parent do not affect the parent's effective width or height. Any unit value that depends on children or parents only considers the **immediate child or parent** and does not look at sizes beyond the immediate relationship.

For example if a parent has 100 effective width, and its child is given an `X` of 200, the parent's effective width remains 100 (assuming the parent does not size itself according to its children). This is important when other children are sized or positioned according to the parent's width.

The following animation shows three instances:

1. A parent container&#x20;
2. A blue rectangle which is sized according to the parent rectangle
3. A yellow rectangle which is moved outside of the bounds of its parent

Notice that when the parent resizes the blue rectangle is also resized, but when the yellow rectangle is moved outside of the parent bounds, the parent and blue rectangle are not resized.

<figure><img src="../../../.gitbook/assets/07_06 55 18.gif" alt=""><figcaption><p>Children can exist outside of the parent bounds without resizing the parent</p></figcaption></figure>

Children outside of bounds may not respond to click events unless the parent is explicitly checking for click events outside of its bounds. This is a property which is set at runtime. For more information see the [RaiseChildrenEventsOutsideOfBounds](../../../gum-code/gum-code-reference/interactivegue/raisechildreneventsoutsideofbounds.md) page.

### Example - Drag+Drop in the Tree View

To change `Parent` in the **Project** tab:

1. Select a child
2. Drag+drop the child onto the desired parent

<figure><img src="../../../.gitbook/assets/11_20 21 41.gif" alt=""><figcaption><p>Drag+drop a child onto the desired parent</p></figcaption></figure>

The child can be detached from its `Parent` by drag+dropping it onto the Component.

<figure><img src="../../../.gitbook/assets/11_20 22 36.gif" alt=""><figcaption><p>Drag+drop a child onto its root component or screen to detach it from its current parent</p></figcaption></figure>

Drag+dropping onto a parent may set the `Parent` property to an instance inside of the parent's Component type sets its Default Child Container value. For more information see the [Default Child Container](../component/default-child-container.md) page.

### Example - Using the Dropdown

To set Parent by name:

1. Select the desired child
2. Change the `Parent` property to the desired parent:

<figure><img src="../../../.gitbook/assets/11_20 20 04.gif" alt=""><figcaption><p>Change Parent using the dropdown</p></figcaption></figure>
