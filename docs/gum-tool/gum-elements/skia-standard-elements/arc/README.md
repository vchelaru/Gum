# Arc

### Introduction

Arcs are curved lines with variable thickness. Arcs can also be used to draw wedges if the line thickness is large enough.

<figure><img src="../../../../.gitbook/assets/image (135).png" alt=""><figcaption><p>Three arc instances - a default arc, a rounded arc with gradient graphics, and an arc creating a wedge</p></figcaption></figure>

### Arc dimensions

Arcs draw inside their bounds, with the edge of the arc touching the bounding rectangle. On Skia, the thickness of the arc remains consistent regardless of the bounds width and height. For MonoGame, KNI, and FNA, see [Arcs in Non-Square Bounds](#arcs-in-non-square-bounds).

The following shows an arc with a Sweep Angle of 270 degrees, being resized in the Editor tab.

<figure><img src="../../../../.gitbook/assets/26_16 14 29.gif" alt=""><figcaption><p>On Skia, arc thickness remains constant when an arc's dimensions change</p></figcaption></figure>

### Arc and Circle

An Arc with a `Sweep Angle` of `360` draws the same pixels as a [Circle](../../circle.md) with the same bounds when the Arc's `Thickness` equals the Circle's `Stroke Width`.

{% hint style="info" %}
On MonoGame, KNI, and FNA this match is available in November 2026, or now if building Gum from source. Before then, the Arc's stroke was about one pixel wider than the Circle's.
{% endhint %}

### Arcs in Non-Square Bounds

In non-square bounds, an Arc on MonoGame, KNI, and FNA is stretched to fill the bounds, so it is thicker along the longer side. An Arc 60 wide and 40 tall with a `Thickness` of `8` is about `12` thick at the left and right.

<figure><img src="../../../../.gitbook/assets/21_05 19 29.png" alt=""><figcaption><p>Arc that is 60 wide and 40 tall with a Thickness of 8, a Start Angle of 90 and a Sweep Angle of 270</p></figcaption></figure>

{% hint style="info" %}
The stretched Arc on MonoGame, KNI, and FNA is available in November 2026, or now if building Gum from source.
{% endhint %}
