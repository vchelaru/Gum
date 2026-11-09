# Stride

## Introduction

This page assumes you have an existing Stride project. This can be an empty project or an existing game.

## Adding Gum NuGet package

The easiest way to add Gum to your project is to use the NuGet package. Open your project in your preferred IDE, or add Gum through the command line.

Add the Gum.Stride NuGet package ([https://www.nuget.org/packages/Gum.Stride](https://www.nuget.org/packages/Gum.Stride))

The block below includes the base package and two add-ons marked **optional**: shader file support and arithmetic expression support. If you don't need an add-on, skip its line here and delete the matching line from the initialization code in [Initializing Gum](#initializing-gum) below.

Modify csproj:

```xml
<PackageReference Include="Gum.Stride" Version="*" />
<PackageReference Include="Gum.SkiaSharp.ShadowDusk" Version="*" /> <!-- Optional: .fx and .slang shader files -->
<PackageReference Include="Gum.Expressions" Version="*" /> <!-- Optional: arithmetic expressions in variable references -->
```

Or add through command line:

```bash
dotnet add package Gum.Stride
dotnet add package Gum.SkiaSharp.ShadowDusk # Optional: .fx and .slang shader files
dotnet add package Gum.Expressions          # Optional: arithmetic expressions in variable references
```

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

As with the NuGet packages above, the shader and expression projects are marked **optional**; skip a project if you don't need it, and delete its matching line from the initialization code in [Initializing Gum](#initializing-gum).

Add the following projects to your solution:

* \<Gum Root>/Runtimes/StrideGum/StrideGum.csproj
* \<Gum Root>/Runtimes/SkiaGum.ShadowDusk/SkiaGum.ShadowDusk.csproj, **Optional:** .fx and .slang shader files
* \<Gum Root>/Runtimes/GumExpressions/GumExpressions.csproj, **Optional:** arithmetic expressions in variable references

`StrideGum.csproj` already references `GumCommon` and `SkiaGum` itself, so you do not need to add either separately.

Next, add project references in your game project for the pieces you use. Your project might look like this depending on the location of the Gum repository relative to your game project:

```xml
<ProjectReference Include="..\Gum\Runtimes\StrideGum\StrideGum.csproj" />
<ProjectReference Include="..\Gum\Runtimes\SkiaGum.ShadowDusk\SkiaGum.ShadowDusk.csproj" />
<ProjectReference Include="..\Gum\Runtimes\GumExpressions\GumExpressions.csproj" />
```

## Initializing Gum

Stride draws through a `GraphicsCompositor`, and Gum draws as part of that pipeline, so create the compositor before calling `Initialize`. Stride also renders the scene through a camera, so add one or the window stays empty. The following is a complete `Program.cs`. Lines marked **Optional** match the optional packages above, so delete the lines for any package you skipped.

```csharp
using Gum;
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

    GumUI.Initialize(game, "Content/GumProject/GumProject.gumj");
    GumUI.UseShadowDusk(); // Optional: .fx and .slang shader files
    Gum.Expressions.GumExpressionService.Initialize(); // Optional: arithmetic expressions in variable references
    GumUI.UseKeyboardDefaults();
    GumUI.UseGamepadDefaults();
}
```

`Add2DCamera` suits a UI-only or 2D game. Use `Add3DCamera` instead if your game draws a 3D scene. Gum draws in screen space and ignores the camera, so either one works for the UI.

`Initialize` adds Gum's scene renderer to the compositor, and Stride runs Gum's update and draw every frame from there. Stride is the one runtime where you never call `Update` and `Draw` yourself, so the per-frame calls the other setup pages show have no equivalent here.

You can add controls as soon as `Initialize` returns. The project path is optional: call `Initialize(game)` alone to build the UI in code. To get the same file with a Gum project already set up, run [`gumcli new MyGame -p stride`](../../../../cli/new.md).

### Placing the Scene Renderer Yourself (Optional)

`Initialize` registers one `GumSceneRenderer`, which is all a single UI layer needs. Pass `registerSceneRenderer: false` to place `GumSceneRenderer` instances yourself, either to control where Gum draws among your other renderers or to add more than one Gum draw pass:

```csharp
// Initialize
GumService.Default.Initialize(game, registerSceneRenderer: false);
game.AddSceneRenderer(new GumSceneRenderer());
```

For more detail, see the documentation on `GumService.Initialize` in your IDE.

## About Shader Support (Optional)

`Gum.SkiaSharp.ShadowDusk` lets a render target container's `SourceShaderFile` point at an `.fx` or `.slang` shader file. For loading, errors, and examples, see [Render Target Shaders on SkiaSharp](../../../standard-visuals/containerruntime.md#render-target-shaders-on-skiasharp).

## About Expression Support (Optional)

If your Gum project uses arithmetic expressions in variable references (such as `Width = OtherInstance.Width + 20`), `Gum.Expressions` provides full expression evaluation at runtime. Without it, simple variable references like `Width = OtherInstance.Width` still work. For more information, see the [Runtime Variable References](../../../styling/runtime-variable-references.md) page.

## Adding a Button (Testing the Setup)

Gum can be tested by adding a Button after Gum is initialized. To do so, add code to create a `Button` as shown in the following block of code after the `Initialize` call in `Start`:

```csharp
// Initialize
var button = new Button();
button.AddToRoot();
button.Width = 200;
button.Anchor(Anchor.Center);
button.Click += (_, _) => button.Text = $"Clicked\n{System.DateTime.Now}";
```

`Button` comes from `Gum.Forms.Controls`, `Anchor` from `Gum.Wireframe`.

For a working project with a larger demo (`Label`, `TextBox`, `CheckBox`, and `ListBox` in a `StackPanel`), see the Gum Stride sample:

{% embed url="https://github.com/vchelaru/Gum/tree/main/Samples/StrideGum" %}
