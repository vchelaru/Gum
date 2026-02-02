# Default Child Container

Default Child Container specifies the default parent for children of the selected component.

By default this value is blank, which means that newly added children treat the entire component as their parent. If this value is set, children which are dropped on instances of this component type use the instance as their parent.

Default Child Container is typically set on containers which are designed to hold children, but which have margins or decoration around the dedicated container instance. Examples include list boxes, tree views, and frames.

{% hint style="info" %}
Default Child Container is a property that simplifies the addition of new children to a container. Changing this value will not change already-added children. This property is not required, since the Parent can be manually typed and set to the inner container using the "dot". See below for more information on dot assignments.
{% endhint %}

### Example

Consider a Component named Frame which has two instances: OuterRectangle and InnerRectangle.

<figure><img src="../../../.gitbook/assets/image (1) (1) (3).png" alt=""><figcaption><p>Frame component with two children</p></figcaption></figure>

This Component is designed to keep all of is children inside the InnerRectangle, so that any child automatically respects the margin specified by InnerRectangle.

To make this kind of relationship the default, the Frame can set its Default Child Container property to InnerRectangle.

<figure><img src="../../../.gitbook/assets/image (2) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Setting the Default Child Container</p></figcaption></figure>

Once this value is set, instances which are drag+dropped onto Frame instances use the InnerRectangle as their parent, as shown in the following animation.

<figure><img src="../../../.gitbook/assets/11_20 05 09.gif" alt=""><figcaption><p>Adding a child automatically uses the Default Child Container</p></figcaption></figure>

### Parent Details

When one instance is drag+dropped onto another instance, the Parent property is set according to the parent's Default Child Container.

Using the example above, the RectangleInstance is dropped on the ContainerTestInstance. Since the ContainerTestInstance is of type Frame, then the Default Child Container is applied on the drop, which results in the RectangleInstance's Parent being set to `ContainerTestInstance.InnerRectangle`.

<figure><img src="../../../.gitbook/assets/02_08 43 00.png" alt=""><figcaption></figcaption></figure>

## Ignoring Default Child Container

As mentioned above, if an instance is added to a parent component, the instance automatically attaches itself to the parent's `Default Child Container`. This can be undone by manually changing the `Parent` property.

For example, the `Parent` can be manually changed to `ContainerTestInstance`.

<figure><img src="../../../.gitbook/assets/22_11 38 45.gif" alt=""><figcaption><p>Changing the Parent to the name of the instance can force a child to be attached to the root of the parent</p></figcaption></figure>
