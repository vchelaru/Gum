# Locked

### Introduction

The `Locked` property controls whether an instance can be clicked in the preview window. If this value is true, then the instance cannot be clicked on directly, but must instead be selected through the **Project** tab.

<figure><img src="../../../.gitbook/assets/05_09 56 30.gif" alt=""><figcaption><p><code>Locked</code> instances cannot be selected by clicking on them in the editor window</p></figcaption></figure>



{% hint style="warning" %}
The behavior of the Locked property will change in future versions of Gum as defined in this issue:\
[https://github.com/vchelaru/Gum/issues/273](https://github.com/vchelaru/Gum/issues/273)
{% endhint %}

Once a locked object is selected it can still be edited normally, both in the **Variables** tab and in the **Editor** tab. Locking an instance prevents accidental selection in the Editor tab.

<figure><img src="../../../.gitbook/assets/05_09 57 33.gif" alt=""><figcaption></figcaption></figure>

{% hint style="info" %}
The `Locked` property does not have any impact on the behavior of Gum objects at runtime, such as when running in FlatRedBall or MonoGame. This property only affects editor behavior.
{% endhint %}
