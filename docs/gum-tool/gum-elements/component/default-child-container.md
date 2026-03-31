# Default Slot

{% hint style="info" %}
Previous versions of Gum called this variable `Default Child Container`. This was changed to `Default Slot` in the February 2026 release.
{% endhint %}

Default Slot specifies where children are added to instances of the selected component.

By default this value is blank, which means that newly added children treat the entire component as their parent. If Default Slot value is set, children which are dropped on instances of this component type use the Default Slot as their parent.

Default Slot is typically set on containers which are designed to hold children, but which have margins or decoration around the dedicated container instance. Examples include list boxes, tree views, and frames.

{% hint style="info" %}
The Default Slot property simplifies the addition of new children to a container. Changing this value will not change already-added children. This property is not required, since the Parent can be manually typed and set to the inner container using the "dot". See below for more information on dot assignments.
{% endhint %}

Multiple instances can act as slots. For information on using multiple slots, see the [Is Slot](../general-properties/is-slot.md) page.

### Example

Consider a Component named Frame which has two instances: OuterRectangle and InnerRectangle.

<figure><img src="../../../.gitbook/assets/image (1) (1) (3).png" alt=""><figcaption><p>Frame component with two children</p></figcaption></figure>

This Component is designed to keep all of is children inside the InnerRectangle, so that any child automatically respects the margin specified by InnerRectangle.

To make this kind of relationship the default, the Frame can set its Default Slot property to InnerRectangle.

<figure><img src="../../../.gitbook/assets/23_05 10 35.png" alt=""><figcaption><p>Setting the Default Child Container</p></figcaption></figure>

Once this value is set, instances which are drag+dropped onto Frame instances use the InnerRectangle as their parent, as shown in the following animation.

<figure><img src="../../../.gitbook/assets/11_20 05 09.gif" alt=""><figcaption><p>Adding a child automatically uses the Default Child Container</p></figcaption></figure>

### Parent Details

When one instance is drag+dropped onto another instance, the Parent property is set according to the parent's Default Slot.

Using the example above, the RectangleInstance is dropped on the ContainerTestInstance. Since the ContainerTestInstance is of type Frame, then the Default Slot is applied on the drop, which results in the RectangleInstance's Parent being set to `ContainerTestInstance.InnerRectangle`.

<figure><img src="../../../.gitbook/assets/02_08 43 00.png" alt=""><figcaption></figcaption></figure>

## Ignoring Default Slot

As mentioned above, if an instance is added to a parent component, the instance automatically attaches itself to the parent's `Default Slot`. This can be undone by manually changing the `Parent` property.

For example, the `Parent` can be manually changed to `ContainerTestInstance`.

<figure><img src="../../../.gitbook/assets/22_11 38 45.gif" alt=""><figcaption><p>Changing the Parent to the name of the instance can force a child to be attached to the root of the parent</p></figcaption></figure>
