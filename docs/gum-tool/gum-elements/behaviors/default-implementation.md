# Default Implementation

### Introduction

The Default Implementation property can be used to indicate which component is the default implementation for a behavior. This property is not used by the Gum tool, but instead exists for runtime implementations (such as FlatRedBall) to decide which type of component to create when an instance of a behavior is requested.

<figure><img src="../../../.gitbook/assets/03_18 47 10.png" alt=""><figcaption><p>ListBoxItemBehavior with a Default Implementation set to Controls/ListBoxItem</p></figcaption></figure>

{% hint style="info" %}
As of March 2025 this property is used in the following runtimes:

* FlatRedBall
* MonoGame/Kni/FNA

Additional runtimes may add support for this property in the future. If you need it for your project please make a request on GitHub or Discord.
{% endhint %}

### Common Usage

Behaviors are often used to help with the creation of components which need to have a certain set of states and instances. For example, the Button type in Gum Forms is associated with the ButtonBehavior.

At runtime, a game may need to create an instance of the Button type without specifying a component. For example, the following code can be used to create a button:

```csharp
var button = new Button();
button.Text = "Hello";
StackLayoutInstance.AddChild(button);
```

The Default Implementation property can help runtime libraries determine which component to create for the Button's visual.

The Button type is a good example of why this property might be needed because the default Forms components include multiple components which use ButtonBehavior.

<figure><img src="../../../.gitbook/assets/image (2) (1) (1).png" alt=""><figcaption><p>Multiple button components</p></figcaption></figure>

To resolve this ambiguity, the ButtonBehavior's Default Implementation is automatically set to Controls/ButtonStandard.

<figure><img src="../../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>ButtonBehavior using ButtonStandard as the Default Implementation</p></figcaption></figure>

For more information about whether you should set the Default Implementation, refer to the documentation for your particular runtime.

If you have created a custom runtime, such as a new ListBoxItem, you may need to change the Default Implementation for the ListBoxItem behavior.
