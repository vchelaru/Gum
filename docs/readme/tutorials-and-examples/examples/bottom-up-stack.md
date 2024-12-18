# Bottom-Up Stack

Bottom-up stacks can be used to display stacks of elements which should move up as more are added. This concept is similar to messages received in a chat window. Gum layout can be used to produce this type of stack.

A bottom-up stack will be a container, which could be an instance of a container or a component since components ultimately are containers. For this example we'll use a container.

<figure><img src="../../../.gitbook/assets/image (3) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Container instance</p></figcaption></figure>

For this container to stack we'll set the following variables:

*   Children Layout set to Top to Bottom Stack so all children stack vertically\


    <figure><img src="../../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Children Layout set to Top to Bottom Stack</p></figcaption></figure>
*   Height Units set to Relative to Children so the container resizes itself as more children are added\


    <figure><img src="../../../.gitbook/assets/image (2) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Height Units set to </p></figcaption></figure>
*   Height set to 0 so the height of the container is based purely on its children\


    <figure><img src="../../../.gitbook/assets/image (3) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Height set to 0</p></figcaption></figure>
*   Stack Spacing set to 2 (optional) to add spacing between each child\
    \


    <figure><img src="../../../.gitbook/assets/image (4) (1) (1) (1) (1).png" alt=""><figcaption><p>Stack Spacing set to 2</p></figcaption></figure>

Now the container can have children added. Any type of child will stack. For this tutorial we'll use ColoredRectangle instances. Add a few instances to the Container and they stack vertically.

<figure><img src="../../../.gitbook/assets/image (5) (1) (1).png" alt=""><figcaption><p>ColoredRectangle instances stacking vertically in a container</p></figcaption></figure>

Finally we can have the stack grow up instead of down.  To do this, change the following variables on the parent container:

*   Y Origin set to Bottom\


    <figure><img src="../../../.gitbook/assets/image (6) (1) (1).png" alt=""><figcaption><p>Y Origin set to Bottom</p></figcaption></figure>
*   Y Units set to Pixels from Bottom\


    <figure><img src="../../../.gitbook/assets/image (7) (1) (1).png" alt=""><figcaption><p>Y Units set to Pixels from Bottom</p></figcaption></figure>

Now as new children are added, the parent stack grows and all items shift up.

<figure><img src="../../../.gitbook/assets/08_21_40_58.gif" alt=""><figcaption><p>Stack grows upward as more children are added</p></figcaption></figure>

