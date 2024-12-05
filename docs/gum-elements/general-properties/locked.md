# Locked

### Introduction

The Locked property controls whether an instance can be clicked in the preview window. If this value is true, then the instance cannot be clicked on directly, but must instead be selected through the Project tab.

<figure><img src="../../.gitbook/assets/05_09 56 30.gif" alt=""><figcaption><p>Locked istances cannot be selected by clicking on them in the editor window</p></figcaption></figure>

Once a locked object is selected it can still be edited normally, both in the Variables tab and in the Editor tab. Locking an instance prevents accidental selection in the Editor tab.

<figure><img src="../../.gitbook/assets/05_09 57 33.gif" alt=""><figcaption></figcaption></figure>

{% hint style="info" %}
The Locked property does not have any impact on the behavior of Gum objects at runtime, such as when running in FlatRedBall or MonoGame. This property only affects editor behavior.
{% endhint %}
