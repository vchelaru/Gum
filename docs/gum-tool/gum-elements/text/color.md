# Color

### Introduction

The Color value controls a Text object's color. For more information on Text colors, see the general [Color page](../general-properties/color.md#example-setting-color).

### Color Multiplication

For default Text objects, the Color value modifies the displayed Text color. Gum creates .fnt and .png files where the color is white. If the .png includes any colors that are not white, then the resulting color is produced by _multiplying_ the color value with each pixel. Therefore, if a Font is outlined, then the black pixels remain black.

<figure><img src="../../../.gitbook/assets/15_08 19 58.gif" alt=""><figcaption><p>Changing the color value of outlines</p></figcaption></figure>
