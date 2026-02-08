# Behaviors

## Introduction

Behaviors can define requirements which are reusable across multiple components to standardize instance names and behaviors. If a component uses a behavior, then the component is forced to include categories and instances according to the behavior definition.

Common behavior usage falls into one of two categories:

1. Behaviors for built-in controls such as Button and TextBox exist to make customization for these types of controls easier.
2. New behaviors can be created to match the syntax of controls defined in your game project. This is considered an advanced scenario and is rarely used.

The most common usage of behaviors is the automatic creation and inclusion of _Gum Forms_ behaviors. Therefore, it is unlikely that you will need to create new behaviors or edit existing behaviors.&#x20;

{% hint style="info" %}
C# programmers may find the concept of behaviors to be similar to interfaces in code. Behaviors define requirements for components, but they give components the flexibility to implement these requirements, just like interfaces define required properties and methods which classes can implement.
{% endhint %}

{% hint style="warning" %}
Behaviors place requirements for components in the Gum tool, but the behavior of these components at runtime depends on the control existing in the particular runtime that is being used.

As of February 2026, forms controls are not implemented in Skia-based runtimes. If this is a requirement for your project, please post an issue on GitHub or let us know on Discord.
{% endhint %}

## Default Behaviors

By default, empty projects contain no behaviors. If your project has added forms components, then it should contain a set of default behaviors matching the forms control types.

<figure><img src="../../../.gitbook/assets/image.png" alt=""><figcaption><p>Default behavior types</p></figcaption></figure>

{% hint style="info" %}
Future versions of Gum may add or remove behavior types, so don't worry if your list is different than the screenshot above.
{% endhint %}

These behaviors are used by the Gum runtime to decide whether a component should have the behavior of a particular control type.

For example, a component with a NineSlice background will not respond to cursor hover events by default. However, if the ButtonBehavior is added to this component, then the component is required to contain certain states which are used at runtime to react to hover, push, and disable states.

In other words, behaviors answer the question "How can I give default forms behavior to my component" by providing a required set of states, instances.

## Default Control Behaviors

As mentioned above, if you have added forms controls to your project, then you should have a set of components which already implement the default behaviors defined below.

For example, we can look at the `ButtonStandard` component which implements `ButtonBehavior`.

<figure><img src="../../../.gitbook/assets/08_07 34 25.png" alt=""><figcaption><p>ButtonBehavior used by ButtonStandard</p></figcaption></figure>

By using the ButtonBehavior, the ButtonStandard will automatically be associated with the Button type at runtime.

Furthermore, the `ButtonStandard` component is required to include the category and states defined by `ButtonBehavior`.

Notice the categories and states defined by ButtonBehavior:

<figure><img src="../../../.gitbook/assets/08_07 37 59.png" alt=""><figcaption><p>Behaviors defined by ButtonBehavior</p></figcaption></figure>

These automatically-added states are empty - they do nothing by default. For information on working with states, see the [States page](../states/).

Gum prevents the removal or renaming of any of these states from `ButtonStandard` since they are required by  `ButtonBehavior`.

<figure><img src="../../../.gitbook/assets/08_07 41 13.png" alt=""><figcaption><p>Removal is prevented if a state is defined by a used behavior</p></figcaption></figure>

## Adding Behaviors to Components

If you are creating a new component which should be used as a standard forms type, such as creating a new Button style, then you will need to add a behavior to the component. By adding a new behavior, Gum will add required states automatically and will display errors if any behavior requirements are missing.

For example, consider the creation of a new component which will have button behavior (responding visually to hover and push, enabled/disabled support, and click events).&#x20;

To add the ButtonBehavior to a component:

1. Select your component
2. Click the Behaviors tab
3. Click the Edit button
4. Check the desired behavior ( `ButtonBehavior` )
5. Click OK to apply the selected behavior

Gum automatically creates the ButtonCategory and required states. Keep in mind that these states are empty - it is up to you to select the states and customize your component appropriately.

<figure><img src="../../../.gitbook/assets/08_07 48 27.png" alt=""><figcaption><p>A component using ButtonBehavior</p></figcaption></figure>

## Behavior Instance Requirements

Some behaviors have instance requirements too. For example, the `TextBoxBehavior` has two required instances:&#x20;

1. TextInstance which uses a base type of Text
2. CaretInstance which can be of any type

<figure><img src="../../../.gitbook/assets/08_07 50 16.png" alt=""><figcaption><p><code>TextBoxBehavior</code> has required instances</p></figcaption></figure>

If this behavior is used in a component, Gum displays errors indicating that instances are missing.

<figure><img src="../../../.gitbook/assets/08_07 52 13.png" alt=""><figcaption></figcaption></figure>

You need to add instances to your component to satisfy these errors or else the component may not function properly at runtime, and may even cause runtime crashes.

{% hint style="info" %}
Notice that Gum is able to automatically create categories and states when a new behavior is added, but it does not automatically create required instances. This happens because Gum can add empty states which you can choose to fill in or leave as default.

By contrast, Gum cannot guess how to create instances for your components. For example, the CaretInstance required by a TextBox can be of any type - you may want to use a Sprite to display a texture, a Rectangle, or even a dedicated custom Caret component.

Future versions of Gum may provide shortcuts to create required types.
{% endhint %}
