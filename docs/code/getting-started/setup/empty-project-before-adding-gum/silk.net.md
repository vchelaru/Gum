# Silk.NET

## Introduction

Gum can be used in Silk.NET projects by importing Gum's `Gum.SilkNet` NuGet package. `Gum.SilkNet` renders through SkiaSharp and adds real Forms input via `Silk.NET.Input`; your project still owns window creation and the render loop.

## Creating an New Project

Before using Gum, you should first verify that you can create a normal windowed Silk.NET project. For the full API reference, see the Silk.NET documentation: [https://dotnet.github.io/Silk.NET/docs/v3/](https://dotnet.github.io/Silk.NET/docs/v3/)

If you are starting a brand new Silk.NET project from scratch, you can follow these steps:

{% tabs %}
{% tab title="Visual Studio" %}
First, create an empty console project

1. Open Visual Studio
2. Select File -> New Project, or select the **Create a new project** option in the popup window.
3. Select the option to create a **Console App**
4. Enter a name, select a location, then click **Next**
5. Select the desired **Framework** and other options, then click **Create**

Next, add the needed NuGet package:

1. Expand your game project in the Solution Explorer
2. Right-click on **Dependencies** and select **Manage NuGet Packages**
3. Search for and install `Silk.NET.Windowing`
{% endtab %}
{% endtabs %}

Once you have your project set up, it might look similar to the following code block — a window that opens and clears to a solid color every frame:

```csharp
using Silk.NET.Windowing;
using Silk.NET.Maths;

namespace SilkNetExample1;

public class Program
{
    public static void Main()
    {
        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(800, 480);
        options.Title = "Gum Sample";

        using var window = Window.Create(options);

        window.Render += delta =>
        {
            // Per-frame rendering goes here.
        };

        window.Run();
    }
}
```

Next, you can begin adding Gum to your project. For more information see the [Adding/Initializing Gum](../adding-initializing-gum/silk.net.md) page.
