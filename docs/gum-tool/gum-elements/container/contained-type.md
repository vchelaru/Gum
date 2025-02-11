# Contained Type

### Introduction

Contained Type enables code generation and Gum runtimes (such as FlatRedBall) to create strongly-typed containers.

{% hint style="info" %}
Currently the Contained Type variable does not have any affect on objects in the Gum tool and it exists only to support strongly-typed runtimes. This may change in future versions of the Gum tool.
{% endhint %}

### Common Usage

Some Gum components or Containers may exist to hold a list of a particular type of item. For example, consider a game which includes a row of hearts to show the player's current health.

<figure><img src="../../../.gitbook/assets/29_05 21 54.png" alt=""><figcaption><p>Row of hearts displaying the player's health</p></figcaption></figure>

In this particular case, the hearts can be filled or empty to show the current and max health, but the max health can also be increased. If the max health increases, then a new heart instance is added to the container at runtime.

Since this container should only ever contain instances of a Heart component, then the container's Contained Type can be set to Heart.

<figure><img src="../../../.gitbook/assets/image (2) (1).png" alt=""><figcaption><p>HealthContainer instance with its Contained Type set to Heart</p></figcaption></figure>

In this example, FlatRedBall respects the Contained Type variable and generates a generic list.

<figure><img src="../../../.gitbook/assets/image (1) (1) (1) (1) (2) (1).png" alt=""><figcaption><p>ContainerRuntime can be generic in FlatRedBall, so it respects the Contained Type variable</p></figcaption></figure>

As mentioned above, the implementation of this variable depends on the runtime you are using. If you are using a runtime which does not implement this feature and you would like to have it added, please create a GitHub issue or make a request in Discord.
