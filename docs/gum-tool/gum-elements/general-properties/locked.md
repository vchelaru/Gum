# Locked

{% hint style="warning" %}
The behavior of the Locked property is changing for the February 2025 release of Gum as outlined in this issue:\
[https://github.com/vchelaru/Gum/issues/273](https://github.com/vchelaru/Gum/issues/273)

Previous versions allowed locked items to be edited once selected, but they only prevented selection in the Editor tab.
{% endhint %}

### Introduction

The `Locked` property controls whether an instance can be clicked in the Editor tab, and whether any variables can be modified on the locked item.

If this value is true, then the following is true:

* Instance cannot be selected in the Editor tab
* Instance variables are all disabled and cannot be edited in the Variables tab (except un-locking the instance)
* Selected instances cannot be moved, resized, or rotated in the Editor tab
* Polygons cannot have points modified, added, or deleted in the Editor tab

<figure><img src="../../../.gitbook/assets/05_09 56 30.gif" alt=""><figcaption><p><code>Locked</code> instances cannot be selected by clicking on them in the editor window</p></figcaption></figure>

{% hint style="info" %}
The `Locked` property does not have any impact on the behavior of Gum objects at runtime, such as when running in FlatRedBall or MonoGame. This property only affects editor behavior.
{% endhint %}
