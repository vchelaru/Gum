# Default Implementation

### Introduction

The Default Implementation property can be used to indicate which component is the default implementation for a behavior. This property does is not used by the Gum tool, but instead exists for runtime implementations (such as FlatRedBall) to decide which type of component to create when an instance of a behavior is requested.

This property may not be used by a particular runtime. For example, at the time of this writing the MonoGame runtime does not use this property.

### Common Usage

Behaviors are often used to help with the creation of components which need to have a certain set of states and instances. For example, the Button type in Gum Forms uses a category named ButtonCategory containing multiple states such as Enabled and Disabled.

At runtime, a game may need to create an instance of the Button type without specifying the component associated with the Button forms type. The Default Implementation property can help runtime libraries (such as FlatRedBall) determine which component to create for the Button's visual.

The Button type is a good example of why this property might be needed because the default Forms components include multiple components which implement ButtonBehavior.

<figure><img src="../../../.gitbook/assets/image (2) (1) (1).png" alt=""><figcaption><p>Multiple button components</p></figcaption></figure>

To resolve this ambiguity, the ButtonBehavior's Default Implementation is automatically set to Controls/ButtonStandard.

<figure><img src="../../../.gitbook/assets/image (1) (1) (1) (1) (1).png" alt=""><figcaption><p>ButtonBehavior using ButtonStandard as the Default Implementation</p></figcaption></figure>

For more information about whether you should set the Default Implementation, refere to the documentation for your particular runtime.
