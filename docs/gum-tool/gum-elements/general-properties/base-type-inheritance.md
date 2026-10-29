# Base Type (Inheritance)

### Introduction

The `Base Type` variable can be used to specify both inheritance but also the type of an instance.

`Base Type` on an instance indicates its type. This is usually automatically set when an instance is first created - usually by drag+dropping a component or standard element onto its target component or screen.

Gum Screens and Components support changing `Base Type` to specify inheritance. By setting `Base Type`, a screen or component automatically inherits the following:

* Variable values
* Exposed variables
* Instances
* Available variables, such as stacking if inheriting from a container

Inheritance is useful if your project needs multiple screens or components which share common variables or instances.

### Instance Base Type

All instances must have a `Base Type` set. For example, the following instance is of type Container.

<figure><img src="../../../.gitbook/assets/29_05 44 38.png" alt=""><figcaption><p>Instance named ContainerInstance is of type Container</p></figcaption></figure>

`Base Type` is automatically assigned when a new instance is created. For example, the following animation shows a new ColoredRectangle instance created. Note that its `Base Type` is automatically assigned to ColoredRectangle.

<figure><img src="../../../.gitbook/assets/29_05 46 26.gif" alt=""><figcaption><p>Creating a new ColoredRectangle instance assigns the base type to ColoredRectangle</p></figcaption></figure>

`Base Type` can be changed after an instance is created. Keep in mind that doing so does not change its name or any other properties, so you may need to manually adjust properties when converting between different `Base Types`. Also, note that default values may change when switching from one `Base Type` to another.

The following shows a ColoredRectangle changed to a Container. Since Container and ColoredRectangle have different default `Width` and `Height` values, changing `Base Type` results in changes to default size too.

<figure><img src="../../../.gitbook/assets/29_05 48 42.gif" alt=""><figcaption><p>Changing <code>Base Type</code> can change other variables such as <code>Width</code> and <code>Height</code></p></figcaption></figure>

### Component Inheritance

All components use inheritance even if their `Base Type` variable is not set explicitly. By default components inherit from the **Container** type.

<figure><img src="../../../.gitbook/assets/image (85).png" alt=""><figcaption><p>Button component inheriting from Container</p></figcaption></figure>

By inheriting from the **Container** type, components have access to all **Container** variables such as Children Layout.

### Inheriting from Standard Types

Components can inherit from standard types. For example, instead of inheriting from **Container** a component may inherit from **ColoredRectangle**. By doing so, it has access to all properties on the **ColoredRectangle** type.

<figure><img src="../../../.gitbook/assets/image (87).png" alt=""><figcaption><p>Component inheriting from ColoredRectangle</p></figcaption></figure>

{% hint style="info" %}
Most components inherit from the **Container** type. If a component needs to display visuals, such as a **ColoredRectangle**, typically the ColoredRectangle is added as a child to the component rather than being used as a `Base Type`.
{% endhint %}

### Inheriting from Components

Components can inherit from other components. By doing so the component inherits all children and exposed variables from its base component.

A component which inherits from another component is often called a _derived_ component. The component which is being inherited from is often called a _base_ component.

A base component can be used to define instances which the derived component can modify. For example a component named ButtonBase may define that all components have a ColoredRectangle named Background and a Text named TextInstance.

<figure><img src="../../../.gitbook/assets/image (88).png" alt=""><figcaption><p>Example component named ButtonBase</p></figcaption></figure>

If another component uses ButtonBase as its `Base Type`, then this component automatically gets Background and TextInstance children which match the base instances.

<figure><img src="../../../.gitbook/assets/28_05 53 21.gif" alt=""><figcaption><p>Setting the base to ButtonBase automatically adds the same children to CancelButton</p></figcaption></figure>

The CancelButton can modify variables on the added children. For example the CancelButton can modify the Text on the TextInstance and the color values on the Background.

<figure><img src="../../../.gitbook/assets/image (89).png" alt=""><figcaption><p>CancelButton with modified children variables (Text and color values)</p></figcaption></figure>

The derived component has the following restrictions when working with children instances:

* Name cannot be changed. For example Background must always be named Background.
* Base Type cannot be changed. For example, the base type for Background must be ColoredRectangle
* Children defined in the base cannot be removed. For example, the Background child cannot be deleted from CancelButton. Children can be made invisible.

If the base type adds new instances, then the derived types automatically get the same instances added as well. Similarly, if the base type deletes a child, then the child is also removed from the derived type.

<figure><img src="../../../.gitbook/assets/28_05 58 17.gif" alt=""><figcaption><p>Children added and removed to base types are also added and removed on the derived types</p></figcaption></figure>

Derived types get access to all of the exposed variables in the base type. For example, if ButtonBase exposes the TextInstance's Text property, this is also available on the derived component.

<figure><img src="../../../.gitbook/assets/28_06 00 59.gif" alt=""><figcaption><p>Exposed variables are inherited</p></figcaption></figure>

### Base Type vs States

`Base Types` allow for the customization of a component, including the creation of many variants. Similarly, States also allow for the customization of a component in similar ways. When deciding between whether to use states or inheritance, keep the following in mind:

* States are often used to set variables temporarily, while inheritance is permanent. For example, a button may set its background color values in response to being highlighted. By contrast, a Cancel button may always say "Cancel".
* States do not allow for the creation of new instances. Although derived components cannot delete children which are defined by their `Base Type`, derived components can add additional instances.
* Components can use multiple categories. Therefore, it may not be clear which category defines the type of component. A component can only have one Base Type, so its type is defined clearly.

### Circular Base Types (GUM0014)

A circular base type is a loop of inheritance, such as component A with a `Base Type` of B while B has a `Base Type` of A, or a component whose `Base Type` is itself. Neither element can inherit anything, because each one waits on the other.

Gum prevents these in the tool. The `Base Type` dropdown leaves out components that derive from the element you are editing, and typing the name of one shows a message that the assignment would create a circular reference and puts the previous `Base Type` back.

A loop can still appear when a project file is edited outside of Gum, for example when merging changes from version control. Gum opens the project anyway and reports a **GUM0014** error in the [Errors tab](../../editor-tab.md) on each element in the loop, naming it (`A -> B -> A`). `gumcli check` reports GUM0014 too. Until the loop is broken, Gum treats every element in it as having no `Base Type`, so none of them show inherited variables, instances, or exposed variables.

To resolve it, change or clear the `Base Type` of one element in the loop, either in the tool or in its element file.

{% hint style="info" %}
Available in November 2026, or now if building Gum from source.
{% endhint %}

### Component With No Base Type (GUM0015)

A component file with an empty `<BaseType>` loads, but code generation then writes a class with no base class. `gumcli check` reports **GUM0015** as an error on each such component. Screens and standard elements are not checked, since they legitimately have no `Base Type`.

To resolve it, set `<BaseType>` in the component's `.gucx` file to `Container`, or to the component it derives from.

{% hint style="info" %}
Available in December 2026, or now if building Gum from source.
{% endhint %}

### Missing Base Instance (GUM0016)

A component that derives from another component must list each of the base's instances with `<DefinedByBase>true</DefinedByBase>`. When a hand-written or out-of-date file leaves one out, Gum adds the instance in memory at load, so nothing breaks, but the file on disk is stale. `gumcli check` reports **GUM0016** as a warning naming the missing instance.

To resolve it, add an `<Instance>` with that name and `<DefinedByBase>true</DefinedByBase>` to the file, or run `gumcli resave` to write it.

{% hint style="info" %}
Available in December 2026, or now if building Gum from source.
{% endhint %}

### Variable Without an Instance Prefix (GUM0017)

Variables on an instance are written with the instance name as a prefix, such as `TextInstance.Text`. When a component file sets `Text` with no prefix, but the component has no variable by that name while one of its instances does, the line sets nothing or fails at runtime. `gumcli check` reports **GUM0017** as an error and suggests the prefixed names.

To resolve it, add the instance prefix to the variable name in the file.

{% hint style="info" %}
Available in December 2026, or now if building Gum from source.
{% endhint %}
