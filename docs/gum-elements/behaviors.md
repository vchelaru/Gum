# Behaviors

### Introduction

Behaviors can define requirements which can be reused across multiple components to standardize instance names and behaviors. If a component uses a behavior, then it is forced to include categories and instances according to the behavior definition.

Behaviors are used to define requirements for components, to simplify the creation of new components, and to reduce the chances of spelling and implementation mistakes.

{% hint style="info" %}
Currently the Gum tool supports instance requirements on behaviors, but these requirements must be added by modifying the behavior XML file rather than adding instances through the tool. This will likely change in future versions of Gum
{% endhint %}

{% hint style="info" %}
C# programmers may find the concept of behaviors to be similar to interfaces in code. Behaviors define requirements for components, but they give components the flexibility to implement these requirements, just like interfaces define required properties and methods which classes can implement.
{% endhint %}

### Creating a Behavior

To add a behavior:

1. Right-click on the Behaviors folder
2.  Select **Add Behavior**\


    <figure><img src="../.gitbook/assets/image (82).png" alt=""><figcaption><p>Add Behavior menu item</p></figcaption></figure>
3. Enter the new behavior name. Often time the word **Behavior** is added at the end of the name, such as **ButtonBehavior**

New behaviors appear in the Project tab.

<figure><img src="../.gitbook/assets/image (83).png" alt=""><figcaption><p>ButtonBehavior in the Behaviors folder</p></figcaption></figure>

Once a behavior has been created, it can be given categories and states. Any component which uses this behavior is required to have the same categories and states. States can be added and removed the same as when working with states in other elements. For more information, see the [States](states/) page.

For example, the ButtonBehavior may have the following:

* ButtonCategory (Category)
  * Enabled (State)
  * Disabled (State)
  * Focused (State)
  * Pushed (State)

<figure><img src="../.gitbook/assets/image (84).png" alt=""><figcaption><p>ButtonCategory defined on ButtonBehavior</p></figcaption></figure>

A behavior can have as many categories and states as needed.

Once a behavior is added, it can be used in a component. To add a behavior to a component

1. Select a component which should use the behavior
2. Click the Behaviors tab
3. Click the Edit button
4. Check the desired behaviors - a component may use multiple behaviors
5. Click OK

<figure><img src="../.gitbook/assets/28_05 20 28.gif" alt=""><figcaption><p>Button component adding the ButtonBehavior</p></figcaption></figure>

Notice that once a behavior is added to a component, the component automatically creates the matching categories and states.

These categories cannot be removed as long as the component continues to use the behavior.
