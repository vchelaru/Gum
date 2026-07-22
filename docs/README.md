# Introduction

<figure><img src=".gitbook/assets/gum-logo-normal-512.png" alt=""><figcaption></figcaption></figure>

Gum is the best Game UI Layout tool available. It provides a flexible, efficient layout engine capable of producing virtually any layout. Gum can be used in a variety of contexts including in the [FlatRedBall game engine](https://docs.flatredball.com/gum/), [MonoGame](code/getting-started/setup/adding-initializing-gum/monogame-kni-fna/), [raylib](code/getting-started/setup/adding-initializing-gum/raylib-raylib-cs.md), [Silk.NET](code/getting-started/setup/adding-initializing-gum/silk.net.md), and more. Gum can also be rendered on Skia so it can be used in any environment that supports Skia such as [WPF](code/getting-started/setup/adding-initializing-gum/wpf.md) and Avalonia.

The Gum layout engine can also be included in any .NET project without requiring the use of a particular graphical API.

To download the Gum UI tool and start building your UI, see the [Setup](gum-tool/setup/README.md) page.

### Powerful WYSIWYG Editor

Gum UI includes advanced layout functionality to create and preview your UI

<figure><img src=".gitbook/assets/30_06 53 41.png" alt=""><figcaption><p>Gum UI</p></figcaption></figure>

### Object Oriented Design Focused on Reusable Controls

Gum allows the creation of components which can be instanced and customized in screens and other components

<figure><img src=".gitbook/assets/30_06 54 43.png" alt=""><figcaption><p>Gum Components</p></figcaption></figure>

### Gum Objects Support Multiple Size and Position Units

Adjust an object’s origin, position units, size units, and stacking to create fluid UI

<figure><img src=".gitbook/assets/30_06 50 51.png" alt=""><figcaption><p>Position and Size Units</p></figcaption></figure>

### Simple Integration - Gum Supports Many Runtimes

Grab the NuGet, add a few lines of code, see your Gum project in game! You can use Gum with MonoGame, KNI, FNA, raylib, Silk.NET, SkiaSharp, and many more platforms. For more information on our runtimes, see the [Getting Started](code/getting-started/) page.

<figure><img src=".gitbook/assets/image (29).png" alt=""><figcaption><p>Gum UI in game</p></figcaption></figure>

### Interact with Gum in Code

Gum objects can be created and modified in code. Create fully-featured UI by subscribing to common UI events.

```c#
void CustomInitialize()
{
    MyButton.Click += HandleOkButtonClick;
}

private void HandleOkButtonClick(object sender, EventArgs args)
{
    // do your logic here
}
```

### Time-Tested and Reliable

Gum has been used in commercial projects of all sizes - check them out in our [Showcase](gum-tool/showcase.md) page.

### Need Help?

Gum is actively maintained and provides lots of ways to get answers:

* Check the rest of the documentation
* Join the [Discord chat](https://discord.gg/EvqwmSQuBz) (shared discord with FlatRedBall)
* Create an [issue on Github](https://github.com/vchelaru/Gum/issues)
