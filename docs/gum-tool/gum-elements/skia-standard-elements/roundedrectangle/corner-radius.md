# Corner Radius

### Introduction

The `Corner Radius` variable controls the radius of each of the four corners on a rounded rectangle. A value of 0 results in a sharp corner. Increasing this value makes the corners more rounded.&#x20;

<figure><img src="../../../../.gitbook/assets/30_05 49 33.gif" alt=""><figcaption></figcaption></figure>

Corner Radius is restricted to half of the smallest absolute dimension. In other words, if the RoundedRectangle is too small to fit its set CornerRadius, then the effective CornerRadius shrinks.

The following shows a RoundedRectangle with a `Corner Radius` of 60. If it is resized to have an effective width or height of less than 120, then the effective radius shrinks.

<figure><img src="../../../../.gitbook/assets/29_13 24 32.gif" alt=""><figcaption><p>Corner radius shrinking in response to smaller size</p></figcaption></figure>

Similarly, setting a Corner Radius that is larger than half the Width or Height does not affect the RoundedRectangle.

<figure><img src="../../../../.gitbook/assets/29_13 27 04.gif" alt=""><figcaption></figcaption></figure>
