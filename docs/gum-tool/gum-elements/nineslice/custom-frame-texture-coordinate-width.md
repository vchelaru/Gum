# Custom Frame Texture Coordinate Width

## Introduction

The `Custom Frame Texture Coordinate Width` property allows a `NineSlice` to customize the number of pixels used on the source texture when defining its outer frame. This allows for fine control over which parts of a NineSlice stretch and which parts are used as the corners and edges.

By default this value is `null`, which means the `NineSlice` automatically dedicates 1/3 of the texture for the edges.

<figure><img src="../../../.gitbook/assets/image (22).png" alt=""><figcaption></figcaption></figure>

## Changing Custom Frame Texture Coordinate Width

If the `Custom Frame Texture Coordinate Width` value is changed, then the source texture applies a fixed pixel size to the borders. For example, using the image above, the frame can be changed to 3 so that only the black and white pixels are part of the border.

<figure><img src="../../../.gitbook/assets/16_06 03 33.png" alt=""><figcaption><p>NineSlice with an explicitly-set 3 pixel border width</p></figcaption></figure>

### Custom Frame Texture Coordinate Width Set to 0

If `Custom Frame Texture Coordinate Width` is set to a value of 0, then the `NineSlice` does not draw a border. In other words, setting this value to 0 results in the `NineSlice` behaving like a `Sprite`.

<figure><img src="../../../.gitbook/assets/21_07 48 58.gif" alt=""><figcaption></figcaption></figure>
