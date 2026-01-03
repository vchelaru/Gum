# raylib (raylib-cs)

## Introduction

This page assumes you have an existing raylib-cs project. This can be an empty project or an existing game.

The raylib-cs runtime library is still actively developed and the maintainers are prioritizing bugs and features which are needed immediately by the community. If run into a bug or missing feature please let us know on Discord or GitHub issues so we can prioritize fixes for your project.

## Adding Gum NuGet package

The easiest way to add Gum to your project is to use the NuGet package. Open your project in your preferred IDE, or add Gum through the command line.

Add the Gum.raylib NuGet package ([https://www.nuget.org/packages/Gum.raylib](https://www.nuget.org/packages/Gum.raylib))

Modify csproj:

```xml
<PackageReference Include="Gum.raylib" />
```

Or add through command line:

```bash
dotnet add package Gum.raylib
```

## Adding Source (Optional)

You can directly link your project to source instead of a NuGet package for improved debuggability, access to fixes and features before NuGet packages are published, or if you are interested in contributing.

To add source, first clone the Gum repository: [https://github.com/vchelaru/Gum](https://github.com/vchelaru/Gum)

If you have already added the Gum NuGet package to your project, remove it.

Add the following projects to your solution:

* \<Gum Root>/Runtimes/RaylibGum/RaylibGum.csproj
* \<GumRoot>/GumCommon/GumCommon.csproj

Next, add RaylibGum as a project reference in your game project. Your project might look like this depending on the location of the Gum repository relative to your game project:

```xml
<ProjectReference Include="..\Gum\Runtimes\RaylibGum\RaylibGum.csproj" />
```

## Adding Gum to Program

Gum can be added to a Program class with a few lines of code. Projects are encouraged to create a local GumService property called GumUI for convenience.

Add code to your Program class to Initialize, Update, and Draw Gum as shown in the following code block:

<pre class="language-csharp"><code class="lang-csharp"><strong>using Gum.Forms.Controls;
</strong><strong>using Gum.Wireframe;
</strong><strong>using RaylibGum;
</strong>using Raylib_cs;
namespace raylibExample1;

public class Program
{
<strong>    static GumService GumUI => GumService.Default;
</strong>
    // STAThread is required if you deploy using NativeAOT on Windows - See https://github.com/raylib-cs/raylib-cs/issues/301
    [STAThread]
    public static void Main()
    {
        const int screenWidth = 800;
        const int screenHeight = 450;

        Raylib.InitWindow(screenWidth, screenHeight, "Gum Sample");

<strong>        // This tells Gum to use the entire screen
</strong><strong>        GraphicalUiElement.CanvasWidth = screenWidth;
</strong><strong>        GraphicalUiElement.CanvasHeight = screenHeight;
</strong>
<strong>        GumUI.Initialize();
</strong>
        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Raylib_cs.Color.SkyBlue);

<strong>            GumUI.Update(0);
</strong><strong>            GumUI.Draw();
</strong>
            Raylib.EndDrawing();
        }
        Raylib.CloseWindow();
    }
}
</code></pre>

## Adding a Button (Testing the Setup)

Gum can be tested by adding a Button after Gum is initialized. To do so, add code to create a `Button` as shown in the following block of code after Gum is initialized:

```csharp
public static void Main()
{
    //...
    GumUI.Initialize();
    
    var button = new Button();
    button.AddToRoot();
    button.Width = 200;
    button.Anchor(Anchor.Center);
    button.Click += (_,_) => button.Text = $"Clicked\n{DateTime.Now}";
    //additional code omitted
```

<figure><img src="../../../../.gitbook/assets/10_07 36 10.gif" alt=""><figcaption><p>Button in raylib responding to clicks</p></figcaption></figure>

{% hint style="warning" %}
As of August 2025 the raylib implementation is missing a few controls. Specifically the controls that are not present are:

* PasswordBox
* TextBox

Additionally, raylib Gum does not currently read input from keyboards or gamepads.

If your game needs these capabilities, or if you would like to help contribute to develop them, please post a on our [GitHub issues](https://github.com/vchelaru/Gum/issues) or [join our Discord](https://discord.gg/uQSam6w36d).
{% endhint %}
