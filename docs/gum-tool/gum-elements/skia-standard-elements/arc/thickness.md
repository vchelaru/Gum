# Thickness

### Introduction

Thickness controls the width of the arc line in pixels. This value can be increased to make the line arc thicker.

<figure><img src="../../../../.gitbook/assets/26_16 19 17.gif" alt=""><figcaption><p>Changing the thickness chnages the width of the arc's line</p></figcaption></figure>

Thickness can be increased to create a wedge.

<figure><img src="../../../../.gitbook/assets/26_16 21 12.gif" alt=""><figcaption><p>Increasing Thickness to create a wedge</p></figcaption></figure>

Note that Thickness can be increased to any value including values larger than the radius of the arc. Large values can result in the arc rendering past its center point creating a _bowtie_ shape.

<figure><img src="../../../../.gitbook/assets/image (136).png" alt=""><figcaption><p>Bowtie created by setting thickness to be larger than the radius of the arc</p></figcaption></figure>

Other undesirable rendering effects can happen with large thicknesses when working with arcs which have one size (width or height) larger than the other, as shown in the following image:

<figure><img src="../../../../.gitbook/assets/image (137).png" alt=""><figcaption><p>Arck with the values Width=340, Height=160, Thickness=95 rendering incorrectly</p></figcaption></figure>
