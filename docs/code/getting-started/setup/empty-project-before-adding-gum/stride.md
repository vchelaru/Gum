# Stride

## Introduction

Gum can be used in Stride projects by importing Gum's `Gum.Stride` NuGet package. `Gum.Stride` renders through SkiaSharp and adds Forms input through `Stride.Input`; Stride still owns the window and the render pipeline.

## Creating a New Project

Before using Gum, you should first verify that you can create a normal windowed Stride project. For the full API reference, see the Stride documentation: [https://doc.stride3d.net/](https://doc.stride3d.net/)

Stride games can be created two ways: with the Stride Game Studio editor, or as a plain console project using the Stride Community Toolkit ([https://stride3d.github.io/stride-community-toolkit/](https://stride3d.github.io/stride-community-toolkit/)). The steps below use the Community Toolkit, matching the Gum Stride sample.

{% tabs %}
{% tab title="Visual Studio" %}
First, create an empty console project

1. Open Visual Studio
2. Select File -> New Project, or select the **Create a new project** option in the popup window.
3. Select the option to create a **Console App**
4. Enter a name, select a location, then click **Next**
5. Select **.NET 10.0** as the **Framework**, then click **Create**

Next, add the needed NuGet package:

1. Expand your game project in the Solution Explorer
2. Right-click on **Dependencies** and select **Manage NuGet Packages**
3. Check **Include prerelease**, since the Community Toolkit publishes preview versions
4. Search for and install `Stride.CommunityToolkit.Windows`
{% endtab %}
{% endtabs %}

Stride's packages ship `net10.0` builds only, so your project must target `net10.0`.

Once you have your project set up, it might look similar to the following code block, which opens a window showing a lit 3D scene:

```csharp
using Stride.CommunityToolkit.Engine;
using Stride.Engine;

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.AddGraphicsCompositor();
    game.Add3DCamera();
    game.AddDirectionalLight();
}
```

Next, you can begin adding Gum to your project. For more information see the [Adding/Initializing Gum](../adding-initializing-gum/stride.md) page.
