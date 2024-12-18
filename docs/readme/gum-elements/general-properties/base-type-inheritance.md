# Base Type (Inheritance)

### Introduction

Gum Screens and Components support defining a base type. By setting a base type, a screen or component automatically inherits the following from the base type:

* Variable values
* Exposed variables
* Instances
* Available variables, such as stacking if inheriting from a container

Inheritance is useful if your project needs multiple screens or components which share common variables or instances.

### Component Inheritance

All components use inheritance even if the Base Type variable is not set explicitly. By default components inherit from the Container type.

<figure><img src="../../../.gitbook/assets/image (85).png" alt=""><figcaption><p>Button component inheriting from Container</p></figcaption></figure>

By inheriting from the Container type, components have access to all component variables such as Children Layout.&#x20;

### Inheriting from Standard Types

Components can inherit from standard types. For example, instead of inheriting from Container a component may inherit from ColoredRectangle. By doing so, it has access to all properties on the ColoredRectangle type.

<figure><img src="../../../.gitbook/assets/image (87).png" alt=""><figcaption><p>Component inheriting from ColoredRectangle</p></figcaption></figure>

{% hint style="info" %}
Most components inherit from the Container type. If a component needs to display visuals, such as a ColoredRectangle, typically the ColoredRectangle is added as a child to the component rather than being used as a Base Type.
{% endhint %}

### Inheriting from Components

Components can inherit from other components. By doing so the component inherits all children and exposed variables.

A component which inherits from another component is often called a _derived_ component. The component which is being inherited from is often called a _base_ component.

A base component can be used to define instances which the derived component can modify. For example a component named ButtonBase may define that all components have a ColoredRectangle named Background and a Text named TextInstance.

<figure><img src="../../../.gitbook/assets/image (88).png" alt=""><figcaption><p>Example component named ButtonBase</p></figcaption></figure>

If another component uses ButtonBase as its Base Type, then this component automatically gets Background and TextInstance children which match the base instances.

<figure><img src="../../../.gitbook/assets/28_05 53 21.gif" alt=""><figcaption><p>Setting the base to ButtonBase automatically adds the same children to CancelButton</p></figcaption></figure>

The CancelButton can modify variables on the added children. For example the CancelButton can modify the Text on the TextInstance and the color values on the Background.

<figure><img src="../../../.gitbook/assets/image (89).png" alt=""><figcaption><p>CancelButton with modified children variables (Text and color values)</p></figcaption></figure>

The derived component has the following restrictions when working with children

* Name cannot be changed. For example Background must always be named Background.
* Base Type cannot be changed. For example, the base type for Background must be ColoredRectangle
* Children defined in the base cannot be removed. For example, the Background child cannot be deleted from CancelButton

If the base type adds new instances, then the derived types automatically get the same instances added as well. Similarly, if the base type deletes a child, then the child is also removed from the derived type.

<figure><img src="../../../.gitbook/assets/28_05 58 17.gif" alt=""><figcaption><p>Children added and removed to base types are also added and removed on the derived types</p></figcaption></figure>

Derived types get access to all of the exposed variables in the base type. For example, if ButtonBase exposes the TextInstance's Text property, this is also available on the derived component.

<figure><img src="../../../.gitbook/assets/28_06 00 59.gif" alt=""><figcaption><p>Exposed variables are inherited</p></figcaption></figure>

### Base Type vs States

Base types allow for the customization of a component, including the creation of many variants. Similarly, States also allow for the customization of a component in similar ways. When deciding between whether to use states or inheritance, keep the following in mind:

* States are often used to set variables temporarily, while inheritance is permanent. For example, a button may set its background color values in response to being highlighted. By contrast, a Cancel button may always say "Cancel".
* States do not allow for the creation of new instances. Although derived components cannot delete children which are defined by their Base Type, derived components can add additional instances.
* Components can use multiple categories. Therefore, it may not be clear which category defines the type of component. A component can only have one Base Type, so its type is defined clearly.
