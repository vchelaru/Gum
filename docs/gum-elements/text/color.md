# Color

### Introduction

The Color value controls a Text object's color. The Color value is created by combining the Red, Green, and Blue values.

The Color value can be changed through the color picker, or by changing the individual Red, Green, and Blue color values.

<figure><img src="../../.gitbook/assets/15_08 16 53.gif" alt=""><figcaption><p>Changing the Color values by changing Red, Green, Blue, or using the color picker</p></figcaption></figure>

### Color Multiplication

For default Text objects, the Color value modifies the displayed Text color. Gum creates .fnt and .png files where the color is white. If the .png includes any colors that are not white, then the resulting color is produced by _multiplying_ the color value with each pixel. Therefore, if a Font is outlined, then the black pixels remain black.

<figure><img src="../../.gitbook/assets/15_08 19 58.gif" alt=""><figcaption><p>Changing the color value of outlines</p></figcaption></figure>

