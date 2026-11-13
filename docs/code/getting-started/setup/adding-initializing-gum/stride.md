# Stride

## Introduction

This page assumes you have an existing Stride project. This can be an empty project or an existing game.

## Starting From a New Project

If you plan to use a Gum project, the easiest start is a new game that Gum creates for you. In the Gum tool, select **File** > **New Project**, check **Create C# project**, and choose Stride in **Platform**. Or run [`gumcli new MyGame --platform stride`](../../../../cli/new.md). The game already references Gum, loads the Gum project and copies it to the output folder, so you can skip the rest of this page. See [Creating a Project](../../../../gum-tool/setup/creating-a-project.md).

The rest of this page adds Gum to a game you already have.

## Adding Gum NuGet package

The easiest way to add Gum to your project is to use the NuGet package. Open your project in your preferred IDE, or add Gum through the command line.

Add the Gum.Stride NuGet package ([https://www.nuget.org/packages/Gum.Stride](https://www.nuget.org/packages/Gum.Stride))

Modify csproj:

```xml
<PackageReference Include="Gum.Stride" Version="*" />
```

Or add through command line:

```bash
dotnet add package Gum.Stride
```

Extra features come as separate packages, listed in [Optional Extras](#optional-extras).

`Gum.Stride` renders through SkiaSharp and adds real Forms input (mouse, keyboard, gamepad, focus) through `Stride.Input`. Stride owns the window and the render pipeline, and Gum draws into the frame Stride composites.

`Gum.Stride` leaves the choice of window backend to you, so your project also needs a Stride host package. The Gum sample uses the Community Toolkit's Windows host:

```xml
<PackageReference Include="Stride.CommunityToolkit.Windows" Version="1.0.0-preview.66" />
```

Your project must target `net10.0` at minimum, since Stride's own packages ship `net10.0` builds only. Direct3D11 and Direct3D12 need `net10.0-windows7.0` instead; see [Skia Render Path](#skia-render-path) below.

{% hint style="warning" %}
Don't name your project `StrideGum`, that's the assembly name inside the `Gum.Stride` package. A same-named project produces a same-named output DLL that silently overwrites the runtime's copy in your `bin` folder, causing a `TypeLoadException` at runtime with no build warning. Gum's build now catches this for you: if your `AssemblyName` collides, the build fails with an error telling you to change it.
{% endhint %}

## Skia Render Path

{% hint style="info" %}
Available in October 2026, or now if building Gum from source.
{% endhint %}

Gum renders Stride's UI through SkiaSharp, drawn directly into a GPU texture with no CPU round trip. It uses the graphics API your Stride project already builds against, chosen independently of Gum through Stride's own `StrideGraphicsApi` property (`Direct3D11` is Stride's Windows default). Direct3D11, Direct3D12 and Vulkan are supported. There is no CPU fallback: any other graphics API makes `Initialize` throw a `NotSupportedException`.

Direct3D11 and Direct3D12 require your project to target `net10.0-windows7.0`:

```xml
<TargetFramework>net10.0-windows7.0</TargetFramework>
```

Vulkan works from plain `net10.0`, which Vulkan on Linux and macOS requires anyway, since `net10.0-windows7.0` is a Windows-only TFM.

The `Gum.Stride` NuGet package brings in the GPU renderers automatically; you don't add a separate package reference.

## Adding Source (Optional)

You can directly link your project to source instead of a NuGet package for improved debuggability, access to fixes and features before NuGet packages are published, or if you are interested in contributing.

To add source, first clone the Gum repository: [https://github.com/vchelaru/Gum](https://github.com/vchelaru/Gum)

If you have already added the Gum NuGet package to your project, remove it.

Add the following project to your solution:

* \<Gum Root>/Runtimes/StrideGum/StrideGum.csproj

`StrideGum.csproj` already references `GumCommon` and `SkiaGum` itself, so you do not need to add either separately.

Next, add a project reference in your game project. Your project might look like this depending on the location of the Gum repository relative to your game project:

```xml
<ProjectReference Include="..\Gum\Runtimes\StrideGum\StrideGum.csproj" />
```

To link an extra from [Optional Extras](#optional-extras), add its project instead of its package:

* ShadowDusk: \<Gum Root>/Runtimes/SkiaGum.ShadowDusk/SkiaGum.ShadowDusk.csproj
* Expressions: \<Gum Root>/Runtimes/GumExpressions/GumExpressions.csproj

## Placing the Scene Renderer Yourself (Optional)

`Initialize` registers one `GumSceneRenderer`, which is all a single UI layer needs. Pass `registerSceneRenderer: false` to place `GumSceneRenderer` instances yourself, either to control where Gum draws among your other renderers or to add more than one Gum draw pass:

```csharp
// Initialize
GumUI.Initialize(game, registerSceneRenderer: false);
game.AddSceneRenderer(new GumSceneRenderer());
```

For more detail, see the documentation on `GumService.Initialize` in your IDE.

## Initializing Gum

Stride draws through a `GraphicsCompositor`, and Gum draws as part of that pipeline, so create the compositor before calling `Initialize`. Stride also renders the scene through a camera, so add one or the window stays empty. The following is a complete `Program.cs` that also shows a button. It needs only `Gum.Stride` and the Stride host package, and builds its UI in code, so it runs without a Gum project.

{% hint style="info" %}
Available in preview NuGet packages (use a `-preview` version), or now if building Gum from source.
{% endhint %}

```csharp
using Gum;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.Engine;

using Game game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    // Stride draws through a GraphicsCompositor and a camera, so both must exist before Gum initializes.
    game.AddGraphicsCompositor().AddCleanUIStage();
    game.Add2DCamera();

    GumService GumUI = GumService.Default;

    GumUI.Initialize(game);
    GumUI.UseKeyboardDefaults();
    GumUI.UseGamepadDefaults();

    var button = new Button();
    button.AddToRoot();
    button.Width = 200;
    button.Anchor(Anchor.Center);
    button.Click += (_, _) => button.Text = $"Clicked\n{System.DateTime.Now}";
}
```

`Add2DCamera` suits a UI-only or 2D game. Use `Add3DCamera` instead if your game draws a 3D scene. Gum draws in screen space and ignores the camera, so either one works for the UI.

`Initialize` adds Gum's scene renderer to the compositor, and Stride runs Gum's update and draw every frame from there. Stride is the one runtime where you never call `Update` and `Draw` yourself, so the per-frame calls the other setup pages show have no equivalent here.

You can add controls as soon as `Initialize` returns. The button in the center of the window confirms Gum works. Delete it once you start building your game.

## Optional Extras

Each extra is a separate package. Add the package, then add its line right after `GumUI.Initialize`.

| Extra | Package | Line after `Initialize` |
| --- | --- | --- |
| [ShadowDusk](../../../standard-visuals/containerruntime.md#render-target-shaders-on-skiasharp): `.fx` and `.slang` shader files | `Gum.SkiaSharp.ShadowDusk` | `GumUI.UseShadowDusk();` |
| [Expressions](../../../styling/runtime-variable-references.md): arithmetic in variable references | `Gum.Expressions` | `GumUI.UseExpressions();` |
| [Themes](../../../styling/themes/README.md): restyles Forms controls created in code | One package per theme, such as `Gum.Themes.DarkPro.Skia` (add with `--prerelease`) | `Gum.Themes.DarkPro.DarkProTheme.Apply();` |

{% hint style="info" %}
The `.Skia` theme packages are available in December 2026, or now if building Gum from source.
{% endhint %}

## Loading Your Gum Project

To load a Gum project, pass its `.gumj` path to `Initialize`:

```csharp
// Initialize
GumUI.Initialize(game, "Content/GumProject/GumProject.gumj");
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

## Sample Project


For a working project with a larger demo (`Label`, `TextBox`, `CheckBox`, and `ListBox` in a `StackPanel`), see the Gum Stride sample:

{% embed url="https://github.com/vchelaru/Gum/tree/main/Samples/StrideGum" %}
