# LottieAnimation

### Introduction

LottieAnimation instances can render animations in the [Lottie format](https://en.wikipedia.org/wiki/Lottie_\(file_format\)). Lottie files are animated files, usually with vector art, which serve as an alternative to gifs. Since they are often vector art, Lottie files can be resized without pixelation.&#x20;

Lottie files can be downloaded from various sites or created in application such as Adobe After Effects. If you are interested in testing out Lottie animations, you may want to check [lottiefiles.com](https://lottiefiles.com/) for sample Lottie files.

The following shows a Lottie animation playing in Gum. Source file: [https://lottiefiles.com/free-animation/city-skyline-HFnJYQZLPP](https://lottiefiles.com/free-animation/city-skyline-HFnJYQZLPP)

<figure><img src="../../../.gitbook/assets/30_05 38 06 (1) (1).gif" alt=""><figcaption><p>LottieAnimation in Gum</p></figcaption></figure>

### Source File

The Source File variable controls the lottie animation displayed. This value can be set and changed just like source files on other elements such as a .png on a Sprite.

### Lottie Width and Height

Width and Height values can be set on a Lottie file just like any other Gum element. Unlike rasterized objects such as Sprites, LottieAnimations use Vector art so they can be resized and still retain crisp edges and details.

<figure><img src="../../../.gitbook/assets/30_05 41 15.gif" alt=""><figcaption><p>LottieAnimations can be resized and maintain their crisp visuals</p></figcaption></figure>

LottieAnimations are still rasterized in Gum, so they display pixels when the view is zoomed in, especially if the LottieAnimation has a small Width or Height.

<figure><img src="../../../.gitbook/assets/30_05 44 20.gif" alt=""><figcaption><p>LottieAnimations are still rasterized so zooming in shows pixels</p></figcaption></figure>
