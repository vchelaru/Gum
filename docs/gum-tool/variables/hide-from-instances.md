# Hide from Instances

## Introduction

Variables on a component can be hidden from instancesusing the **Hide from Instances** right-click option. When a variable is hidden, it no longer appears in the **Variables** tab when selecting an instance of that component. The variable remains fully functional on the component definition itself — it is only hidden from instances.

{% hint style="warning" %}
TODO: Add gif showing right-clicking a variable, selecting Hide from Instances, then showing the variable is no longer visible on an instance of that component.
{% endhint %}

Hiding variables is useful for:

* Simplifying the **Variables** tab on instances by removing variables that are never changed per-instance
* Preventing accidental changes to variables that could break a component's layout or behavior, such as `Children Layout` on a container that relies on a specific stacking mode

{% hint style="info" %}
The **Hide from Instances** option is also available on screens. Since screens cannot be instanced in the tool, this only affects runtime code generation.
{% endhint %}

## Hiding a Variable

To hide a variable from instances:

1. Select the component definition (not an instance)
2. Right-click the variable in the **Variables** tab
3. Select **Hide from Instances**

The variable displays "Hidden from instances" text beneath it to indicate its status.

{% hint style="warning" %}
TODO: Add screenshot showing a variable with the "Hidden from instances" label displayed beneath it.
{% endhint %}

## Showing a Hidden Variable

To make a hidden variable visible on instances again:

1. Select the component or screen definition
2. Right-click the hidden variable
3. Select **Show on Instances**

## Inheritance

Hidden variable settings are inherited. If a base component hides a variable, all components that derive from it also hide that variable on their instances. A derived component does not need to re-hide variables that are already hidden by a base type.
