# raylib (raylib-cs)

## Introduction

Gum can be used with raylib using raylib-cs (C# bindings for raylib). At the time of this writing, raylib-cs supports:

* Gum layout engine
* Gum visual objects (runtimes) like SpriteRuntime, NineSliceRuntime, and TextRuntime
* Gum Forms (partial support)

If you are interested in using raylib, please join the discord channel and provide feedback on this initial implementation.

## Setup

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
3. Search for and install `Raylib-cs` . As of August 2025 the newest version is 7.0.1.
4. Search for and install `Gum.raylib`. As of August 2025 the newest version is 2025.8.4.1
{% endtab %}
{% endtabs %}

Once you have your project set up, modify the Program.cs file so it contains the following code:

```csharp
using Gum.Forms.Controls;
using Gum.Wireframe;
using Raylib_cs;
using RaylibGum;
namespace raylibExample1;

public class Program
{
    static GumService GumUI => GumService.Default;

    // STAThread is required if you deploy using NativeAOT on Windows - See https://github.com/raylib-cs/raylib-cs/issues/301
    [STAThread]
    public static void Main()
    {
        const int screenWidth = 800;
        const int screenHeight = 450;

        Raylib.InitWindow(screenWidth, screenHeight, "Gum Sample");

        // This tells Gum to use the entire screen
        GraphicalUiElement.CanvasWidth = screenWidth;
        GraphicalUiElement.CanvasHeight = screenHeight;

        GumUI.Initialize();

        var button = new Button();
        button.AddToRoot();
        button.Width = 200;
        button.Anchor(Anchor.Center);
        button.Click += (_,_) => button.Text = $"Clicked\n{DateTime.Now}";

        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Raylib_cs.Color.SkyBlue);

            GumUI.Update(0);
            GumUI.Draw();

            Raylib.EndDrawing();
        }
        Raylib.CloseWindow();
    }
}
```

Run the game to see a functional button that responds to clicks.

<figure><img src="../.gitbook/assets/10_07 36 10.gif" alt=""><figcaption><p>Button in raylib responding to clicks</p></figcaption></figure>

## Next Steps

Once you have Gum working in your project, you can begin working with the controls that Gum offers.

Most of the Forms controls (such as Button, ListBox, and CheckBox) are available in raylib. You can see more details by looking at the [Forms Controls](monogame/tutorials/code-only-gum-forms-tutorial/forms-controls.md) tutorial.

{% hint style="warning" %}
As of August 2025 the raylib implementation is missing a few controls. Specifically the controls that are not present are:

* PasswordBox
* TextBox

Additionally, raylib Gum does not currently read input from keyboards or gamepads.

If your game needs these capabilities, or if you would like to help contribute to develop them, please post a on our [GitHub issues](https://github.com/vchelaru/Gum/issues) or [join our Discord](https://discord.gg/uQSam6w36d).
{% endhint %}
