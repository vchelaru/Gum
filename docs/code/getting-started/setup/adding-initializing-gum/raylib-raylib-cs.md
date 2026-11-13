# raylib (raylib-cs)

## Introduction

This page assumes you have an existing raylib-cs project. This can be an empty project or an existing game.

The raylib-cs runtime library is still actively developed and the maintainers are prioritizing bugs and features which are needed immediately by the community. If run into a bug or missing feature please let us know on Discord or GitHub issues so we can prioritize fixes for your project.

## Starting From a New Project

If you plan to use a Gum project, the easiest start is a new game that Gum creates for you. In the Gum tool, select **File** > **New Project**, check **Create C# project**, and choose raylib in **Platform**. Or run [`gumcli new MyGame --platform raylib`](../../../../cli/new.md). The game already references Gum, loads the Gum project and copies it to the output folder, so you can skip the rest of this page. See [Creating a Project](../../../../gum-tool/setup/creating-a-project.md).

The rest of this page adds Gum to a game you already have.

## Adding Gum NuGet package

The easiest way to add Gum to your project is to use the NuGet package. Open your project in your preferred IDE, or add Gum through the command line.

Add the Gum.raylib NuGet package ([https://www.nuget.org/packages/Gum.raylib](https://www.nuget.org/packages/Gum.raylib))

Modify csproj:

```xml
<PackageReference Include="Gum.raylib" Version="*" />
```

Or add through command line:

```bash
dotnet add package Gum.raylib
```

The [Full Program](#full-program) below needs only this package. Extra features come as separate packages, listed in [Optional Extras](#optional-extras).

{% hint style="warning" %}
Don't name your project `RaylibGum`, that's the assembly name inside the `Gum.raylib` package. A same-named project produces a same-named output DLL that silently overwrites the runtime's copy in your `bin` folder, causing a `TypeLoadException` at runtime with no build warning. Gum's build now catches this for you: if your `AssemblyName` collides, the build fails with an error telling you to change it.
{% endhint %}

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

To link an extra from [Optional Extras](#optional-extras), add its project instead of its package:

* KernSmith: \<Gum Root>/Integrations/KernSmith/KernSmith.RaylibGum/KernSmith.RaylibGum.csproj
* ShadowDusk: \<Gum Root>/Runtimes/RaylibGum.ShadowDusk/RaylibGum.ShadowDusk.csproj
* Expressions: \<Gum Root>/Runtimes/GumExpressions/GumExpressions.csproj

## Full Program

The following is a complete `Program.cs` that opens a window, initializes Gum, shows a button and runs the game loop. It needs only the base package and builds its UI in code, so it runs without a Gum project.

{% hint style="info" %}
Available in preview NuGet packages (use a `-preview` version), or now if building Gum from source.
{% endhint %}

```csharp
using Gum;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Raylib_cs;

const int screenWidth = 800;
const int screenHeight = 450;

Raylib.InitWindow(screenWidth, screenHeight, "My Game");
Raylib.SetTargetFPS(60);

GumService GumUI = GumService.Default;

GumUI.Initialize();
GumUI.UseKeyboardDefaults();
GumUI.UseGamepadDefaults();

var button = new Button();
button.AddToRoot();
button.Width = 200;
button.Anchor(Anchor.Center);
button.Click += (_, _) => button.Text = $"Clicked\n{System.DateTime.Now}";

while (!Raylib.WindowShouldClose())
{
    double gameTime = Raylib.GetTime();
    GumUI.Update(gameTime);

    Raylib.BeginDrawing();
    Raylib.ClearBackground(Raylib_cs.Color.SkyBlue);
    GumUI.Draw();
    Raylib.EndDrawing();
}

Raylib.CloseWindow();
```

The button in the center of the window confirms Gum works. Delete it once you start building your game.

## Optional Extras

Each extra is a separate package. Add the package, then add its line right after `GumUI.Initialize`. The window must already exist.

| Extra | Package | Line after `Initialize` |
| --- | --- | --- |
| [KernSmith](../../../files-and-fonts/font-strategies.md#dynamic-kernsmith-generation) (**Recommended**): fonts built at runtime, no .fnt files | `KernSmith.RaylibGum` | `GumUI.UseKernSmith();` |
| [ShadowDusk](../../../standard-visuals/containerruntime.md#loading-shader-files-with-sourceshaderfile): `.fx` and `.slang` shader files | `Gum.raylib.ShadowDusk` | `GumUI.UseShadowDusk();` |
| [Expressions](../../../styling/runtime-variable-references.md): arithmetic in variable references | `Gum.Expressions` | `GumUI.UseExpressions();` |

{% hint style="warning" %}
**On web:** KernSmith builds fonts with FreeType by default, which is native code and cannot run in a browser. Also add the `KernSmith.Rasterizers.StbTrueType` package, register it in your web target's `Program.cs` before `GumUI.Initialize()`, and pass its backend to `UseKernSmith`. Otherwise Gum falls back to its built in 18 pixel font with no error.

```csharp
using System.Runtime.CompilerServices;
using KernSmith.Rasterizers.StbTrueType;

RuntimeHelpers.RunClassConstructor(typeof(StbTrueTypeRasterizer).TypeHandle);
```

```csharp
// Initialize
GumUI.UseKernSmith(KernSmith.RasterizerBackend.StbTrueType);
```
{% endhint %}

## Loading Your Gum Project

To load a Gum project, pass its `.gumj` path to `Initialize`:

```csharp
// Initialize
GumUI.Initialize("Content/GumProject/GumProject.gumj");
```

Then copy the Gum project to the output folder by adding this to your `.csproj`:

```xml
<ItemGroup>
    <None Update="Content\GumProject\**\*.*">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
</ItemGroup>
```

To create the Gum project and show its screens, see [Loading a Gum Project](../loading-a-gum-project-.gumx.md).
