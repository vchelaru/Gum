# Arc

### Introduction

Arcs are curved lines with variable thickness. Arcs can also be used to draw wedges if the line thickness is large enough.

<figure><img src="../../../../.gitbook/assets/image (135).png" alt=""><figcaption><p>Three arc instances - a default arc, a rounded arc with gradient graphics, and an arc creating a wedge</p></figcaption></figure>

### Arc dimensions

Arcs draw inside their bounds, with the edge of the arc touching the bounding rectangle. The thickness of the arc remains consisntent regardless of the bound width and height.

The following shows an arc with a Sweep Angle of 270 degrees, being resized in the Editor tab.

<figure><img src="../../../../.gitbook/assets/26_16 14 29.gif" alt=""><figcaption><p>Arc thickness remains constant when an arc's dimensions change</p></figcaption></figure>

### Arc and Circle

An Arc with a `Sweep Angle` of `360` draws the same pixels as a [Circle](../../circle.md) with the same bounds when the Arc's `Thickness` equals the Circle's `Stroke Width`.

{% hint style="info" %}
On MonoGame, KNI, and FNA this match is available in November 2026, or now if building Gum from source. Before then, the Arc's stroke was about one pixel wider than the Circle's.
{% endhint %}

### Arcs in Non-Square Bounds

When `Width` and `Height` differ, an Arc fills the bounds as an ellipse. A Circle differs: it always uses the shorter side.

On MonoGame, KNI, and FNA, an Arc in non-square bounds is a circle at the shorter side stretched to fill the bounds. This has three effects:

* `Thickness` is exact along the shorter side and larger along the longer side. For example, an Arc that is 60 by 40 with a `Thickness` of `8` is `8` thick along the short side and `12` thick along the long side.
* A `Thickness` below `2` draws about `2` thick.
* Dashes, round ends, and radial gradients stretch with the Arc.

Skia draws the same `Thickness` all the way around the Arc.

{% hint style="info" %}
The elliptical Arc on MonoGame, KNI, and FNA is available in November 2026, or now if building Gum from source. Before then, it drew a circle as wide as `Width`, which overflowed bounds that were shorter than they were wide.
{% endhint %}
