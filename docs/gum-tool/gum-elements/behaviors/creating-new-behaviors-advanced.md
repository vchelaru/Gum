# Creating New Behaviors (Advanced)

## Introduction

Behaviors are used to standardize state, category, and instance names. The most common usage of behaviors is with Gum Forms. Of course, behaviors can also be used to standardize names in your project for components which are not intended to be with Gum Forms.

This document shows how to create a ButtonBehavior from-scratch. Keep in mind that the ButtonBehavior is a standard type of behavior that exists in most projects, so this is only for illustrative purposes.

### Creating a Behavior

To add a behavior:

1. Right-click on the Behaviors folder
2.  Select **Add Behavior:**

    <figure><img src="../../../.gitbook/assets/image (82).png" alt=""><figcaption><p>Add Behavior menu item</p></figcaption></figure>
3. Enter the new behavior name. Often time the word **Behavior** is added at the end of the name, such as **ButtonBehavior**

New behaviors appear in the Project tab.

<figure><img src="../../../.gitbook/assets/image (83).png" alt=""><figcaption><p>ButtonBehavior in the Behaviors folder</p></figcaption></figure>

Once a behavior is created, it can be given categories, states, and instances. Components which use this behavior are required to have matching categories and states.

The process of adding and removing states to behaviors is the same as adding and removing states in other elements. For more information, see the [States](../states/) page.

For example, the ButtonBehavior may have the following:

* ButtonCategory (Category)
  * Enabled (State)
  * Disabled (State)
  * Focused (State)
  * Pushed (State)

<figure><img src="../../../.gitbook/assets/19_05 44 01.png" alt=""><figcaption><p>ButtonCategory defined on ButtonBehavior</p></figcaption></figure>

A behavior can have as many categories and states as needed.

Once a behavior is added, it can be used in a component. To add a behavior to a component, drag+drop the behavior onto the component in the tree view.

<figure><img src="../../../.gitbook/assets/19_05 44 59.gif" alt=""><figcaption><p>Add a behavior to a component by drag+dropping the behavior on the component in the Project tab</p></figcaption></figure>

Behaviors can also be added and removed on the component's Behaviors tab:

1. Select a component which should use the behavior
2. Click the Behaviors tab
3. Click the Edit button
4. Check the desired behaviors - a component may use multiple behaviors
5. Click OK

<figure><img src="../../../.gitbook/assets/03_04 09 26.gif" alt=""><figcaption><p>Button component adding the ButtonBehavior</p></figcaption></figure>

Notice that once a behavior is added to a component, the component automatically creates the matching categories and states.

If the behavior is selected in the Behaviors tab, the required states and categories are highlighted in the States tab.

<figure><img src="../../../.gitbook/assets/03_04 07 52.png" alt=""><figcaption><p>States and Categories required by the selected behavior are highlighted</p></figcaption></figure>

These categories cannot be removed as long as the component uses the behavior.

### Category and State Requirements

As mentioned above, if a component uses a behavior, then the component is required to include all of the states and categories defined by the behavior. If a behavior is added to a component, then all states and categories in the behavior are automatically added to the component. Keep in mind that newly-added states do not automatically assign any values. The behavior only requires that the states exist but it does not decide which variables are assigned by the states. These required states can even be left to their default so they have no affect on the component.

Required states and categories cannot be removed or renamed. Required states cannot be moved to different categories.

<figure><img src="../../../.gitbook/assets/image (2) (1) (1) (1).png" alt=""><figcaption><p>Renaming and deleting states and categories required by behaviors is not allowed</p></figcaption></figure>

If a new category or state is added to a behavior, all components which use the behavior also have the new category or state added.

<figure><img src="../../../.gitbook/assets/19_06 02 42.gif" alt=""><figcaption><p>Adding states and categories in a component adds the states and categories to all components using the behavior</p></figcaption></figure>

If a state or category is removed from a behavior, Gum does not remove the state or category from components which implement the behavior. Behaviors only define what is required, but they do not prevent components from defining additional states and categories. Also, the states on components may still be needed even if the behavior is removed. Therefore, if you remove any states or categories from a behavior, you may need to manually remove the same states and categories from components which use the behavior if these are no longer needed.

### Instance Requirements

Behaviors can include instances, resulting in required instances existing in components which use the behavior. Instances in behaviors only include two properties:

* Name
* Base Type

Instances in behaviors only require that instances in components have these two matching properties. All other properties can be set to any value.

To add an instance to a behavior, drag+drop a standard element or component onto the behavior in the Project tab.

<figure><img src="../../../.gitbook/assets/21_07 01 58.gif" alt=""><figcaption><p>Drag+drop standard elements or components onto behaviors to create instances in the behavior</p></figcaption></figure>

An instance can have its Name changed, Base Type changed, or removed.

<figure><img src="../../../.gitbook/assets/21_07 04 12.gif" alt=""><figcaption><p>The Variables tab lets you change Name and Base Type. Right-click to delete an instance.</p></figcaption></figure>

If a component is missing a behavior then the Error window provides information about the missing requirement.

<figure><img src="../../../.gitbook/assets/image (128).png" alt=""><figcaption><p>Button component is missing a SpriteInstance which is required by the ButtonBehavior</p></figcaption></figure>

{% hint style="info" %}
At this time Gum does not automatically add required instances to components which need them. This may change in future versions of Gum. For now, instances must be manually added to resolve errors.

<img src="../../../.gitbook/assets/21_07 08 10.gif" alt="" data-size="original">
{% endhint %}
