# Default Child Container

Default Child Container specifies which instance should contain children which are added to instances of the current component.

By default this value is blank, which means that children will treat the entire component as their parent. If this value is set, children which are dropped on instances of this component type will use the instance as their parent.

Default Child Container is typically set on containers which are designed to hold children, but which have margins or decoration around the dedicated container instance. Examples include list boxes, tree views, and frames.

### Example

Consider a Component named Frame which has two instances: OuterRectangle and InnerRectangle.

<figure><img src="../../.gitbook/assets/image (1) (1) (3).png" alt=""><figcaption></figcaption></figure>

This Component is designed to keep all of is children inside the InnerRectangle, so that any child automatically respects the margin specified by InnerRectangle.

To make this kind of relationship the default, the Frame can set its Default Child Container property to InnerRectangle.

<figure><img src="../../.gitbook/assets/image (2) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption></figcaption></figure>

Once this value is set, instances which are drag+dropped onto Frame instances will use the InnerRectangle as their parent, as shown in the following animation.

<figure><img src="../../.gitbook/assets/11_20 05 09.gif" alt=""><figcaption></figcaption></figure>

### Parent Details

When one instance is drag+dropped onto another instance, the Parent property is set according to the parent's Default Child Container.

Using the example above, the RectangleInstance is dropped on the ContainerTestInstance. Since the ContainerTestInstance is of type Frame, then the Default Child Container is applied on the drop, which results in the RectangleInstance's Parent being set to ContainerTestInstance.InnerRectangle.

<figure><img src="../../.gitbook/assets/image (2) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption></figcaption></figure>
