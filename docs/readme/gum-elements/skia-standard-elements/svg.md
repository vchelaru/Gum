# Svg

### Introduction

Svgs can display .svg files. Since these files are vector art, they can be resized without pixelation.

The following is an Svg instance displaying the FlatRedBall logo. Source SVG: [https://github.com/flatredball/FlatRedBallMedia/blob/master/FlatRedBall%20Logos/frb-logo-main.svg](https://github.com/flatredball/FlatRedBallMedia/blob/master/FlatRedBall%20Logos/frb-logo-main.svg)

<figure><img src="../../../.gitbook/assets/image (147).png" alt=""><figcaption><p>Svg displaying the FlatRedBall logo</p></figcaption></figure>

### Source File

The Source File variable controls the lottie animation displayed. This value can be set and changed just like source files on other elements such as a .png on a Sprite.

### Svg Width and Height

Width and Height values can be set on an Svg file just any other Gum element. Unlike rasterized objects such as Sprites, Svgs use Vector art so they can be resized and still retain crisp edges and details.

<figure><img src="../../../.gitbook/assets/30_06 13 47.gif" alt=""><figcaption><p>Resized SVG maintains crisp edges when resized</p></figcaption></figure>

Svgs are still rasterized in Gum, so they display pixels when the view is zoomed in, especially if the Svg has a small Width or Height.

<figure><img src="../../../.gitbook/assets/30_06 15 07.gif" alt=""><figcaption><p>Small Svgs display pixels if zoomed in</p></figcaption></figure>
