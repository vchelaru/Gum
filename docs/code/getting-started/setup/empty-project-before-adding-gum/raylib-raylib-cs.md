# raylib (raylib-cs)

## Introduction

Gum can be used with raylib using raylib-cs (C# bindings for raylib). At the time of this writing, raylib-cs supports:

* Gum layout engine
* Gum visual objects (runtimes) like SpriteRuntime, NineSliceRuntime, and TextRuntime
* Gum Forms (partial support)

If you are interested in using raylib, please join the discord channel and provide feedback on this initial implementation.

## Creating an New Project

Before using Gum, you should first verify that you can create a normal raylib-cs project. For more information see the raylib-cs readme: [https://github.com/raylib-cs/raylib-cs](https://github.com/raylib-cs/raylib-cs).

If you are starting a branch new raylib project from scratch, you can follow these steps:

{% tabs %}
{% tab title="Visual Studio" %}
First, create an empty console project

1. Open Visual Studio
2. Select File -> New Project, or select the **Create a new project** option in the popup window.
3. Select the option to create a **Console App**
4. Enter a name, select a location, then click **Next**
5. Select the desired **Framework** and other options, then click **Create**

Next, add the needed NuGet packages:

1. Expand your game project in the Solution Explorer
2. Right-click on **Dependencies** and select **Manage NuGet Packages**
3. Search for and install `Raylib-cs` . As of November 2025 the newest version is 7.0.2.
{% endtab %}
{% endtabs %}

{% hint style="warning" %}
If you name your project RaylibGum (with any capitalization) you may get a runtime error about not being able to access the GumService type. Use any other name for your project to avoid this problem.
{% endhint %}

Once you have your project set up, it might look similar to the following code block:

```csharp
using Raylib_cs;
namespace raylibExample1;

public class Program
{
    // STAThread is required if you deploy using NativeAOT on Windows - See https://github.com/raylib-cs/raylib-cs/issues/301
    [STAThread]
    public static void Main()
    {
        const int screenWidth = 800;
        const int screenHeight = 450;

        Raylib.InitWindow(screenWidth, screenHeight, "Gum Sample");


        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Raylib_cs.Color.SkyBlue);

            Raylib.EndDrawing();
        }
        Raylib.CloseWindow();
    }
}
```
