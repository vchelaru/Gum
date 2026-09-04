# Stride

## Introduction

This page assumes you have an existing Stride project. This can be an empty project or an existing game.

## Adding Gum NuGet package

The easiest way to add Gum to your project is to use the NuGet package. Open your project in your preferred IDE, or add Gum through the command line.

Add the Gum.Stride NuGet package ([https://www.nuget.org/packages/Gum.Stride](https://www.nuget.org/packages/Gum.Stride))

Modify csproj:

```xml
<PackageReference Include="Gum.Stride" Version="2026.9.3.3-preview.1" />
```

Or add through command line:

```bash
dotnet add package Gum.Stride --prerelease
```

{% hint style="info" %}
`Gum.Stride` has published preview versions only so far, so the version is spelled out above. To find the package in a search, pass `--prerelease` on the command line, or check **Include prerelease** in the Visual Studio NuGet window.
{% endhint %}

`Gum.Stride` renders through SkiaSharp and adds real Forms input (mouse, keyboard, gamepad, focus) through `Stride.Input`. Stride owns the window and the render pipeline, and Gum draws into the frame Stride composites.

`Gum.Stride` leaves the choice of window backend to you, so your project also needs a Stride host package. The Gum sample uses the Community Toolkit's Windows host:

```xml
<PackageReference Include="Stride.CommunityToolkit.Windows" Version="1.0.0-preview.63" />
```

Your project must target `net10.0`, since Stride's own packages ship `net10.0` builds only.

{% hint style="warning" %}
Don't name your project `StrideGum`, that's the assembly name inside the `Gum.Stride` package. A same-named project produces a same-named output DLL that silently overwrites the runtime's copy in your `bin` folder, causing a `TypeLoadException` at runtime with no build warning. Gum's build now catches this for you: if your `AssemblyName` collides, the build fails with an error telling you to change it.
{% endhint %}

## Adding Source (Optional)

You can directly link your project to source instead of a NuGet package for improved debuggability, access to fixes and features before NuGet packages are published, or if you are interested in contributing.

To add source, first clone the Gum repository: [https://github.com/vchelaru/Gum](https://github.com/vchelaru/Gum)

If you have already added the Gum NuGet package to your project, remove it.

Add the following project to your solution:

* \<Gum Root>/Runtimes/StrideGum/StrideGum.csproj

`StrideGum.csproj` already references `GumCommon` and `SkiaGum` itself, so you do not need to add either separately.

Next, add StrideGum as a project reference in your game project. Your project might look like this depending on the location of the Gum repository relative to your game project:

```xml
<ProjectReference Include="..\Gum\Runtimes\StrideGum\StrideGum.csproj" />
```

## Initializing Gum

Stride draws through a `GraphicsCompositor`, and Gum draws as part of that pipeline, so create the compositor before calling `Initialize`. Stride also renders the scene through a camera, so add one or the window stays empty:

```csharp
using Gum;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.Engine;

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.AddGraphicsCompositor().AddCleanUIStage();
    game.Add2DCamera();
    GumService.Default.Initialize(game);
}
```

`Add2DCamera` suits a UI-only or 2D game. Use `Add3DCamera` instead if your game draws a 3D scene. Gum draws in screen space and ignores the camera, so either one works for the UI.

`Initialize` adds Gum's scene renderer to the compositor, and Stride runs Gum's update and draw every frame from there. Stride is the one runtime where you never call `Update` and `Draw` yourself, so the per-frame calls the other setup pages show have no equivalent here.

You can add controls as soon as `Initialize` returns.

To load a Gum project (a `.gumx` file) at the same time, pass its path:

```csharp
// Initialize
GumService.Default.Initialize(game, "Content/GumProject/GumProject.gumx");
```

### Placing the Scene Renderer Yourself (Optional)

`Initialize` registers one `GumSceneRenderer`, which is all a single UI layer needs. Pass `registerSceneRenderer: false` to place `GumSceneRenderer` instances yourself, either to control where Gum draws among your other renderers or to add more than one Gum draw pass:

```csharp
// Initialize
GumService.Default.Initialize(game, registerSceneRenderer: false);
game.AddSceneRenderer(new GumSceneRenderer());
```

For more detail, see the documentation on `GumService.Initialize` in your IDE.

## Adding Expression Support (Optional)

If your Gum project uses arithmetic expressions in variable references (such as `Width = OtherInstance.Width + 20`), you can add the `Gum.Expressions` NuGet package for full expression evaluation at runtime. Without this package, simple variable references like `Width = OtherInstance.Width` still work.

Add the NuGet package:

```bash
dotnet add package Gum.Expressions
```

Then call `GumExpressionService.Initialize()` after `GumService.Default.Initialize`. Expression support is typically used with a Gum project that has variable references defined in the tool:

```csharp
// Initialize
GumService.Default.Initialize(game, "Content/GumProject/GumProject.gumx");
Gum.Expressions.GumExpressionService.Initialize();
```

If linking to source instead of NuGet, add `<Gum Root>/Runtimes/GumExpressions/GumExpressions.csproj` to your solution.

For more information, see the [Runtime Variable References](../../../styling/runtime-variable-references.md) page.

## Adding a Button (Testing the Setup)

Gum can be tested by adding a Button after Gum is initialized. To do so, add code to create a `Button` as shown in the following block of code after Gum is initialized:

```csharp
// Initialize
GumService.Default.Initialize(game);

var button = new Button();
button.AddToRoot();
button.Width = 200;
button.Anchor(Anchor.Center);
button.Click += (_, _) => button.Text = $"Clicked\n{System.DateTime.Now}";
```

`Button` comes from `Gum.Forms.Controls`, `Anchor` from `Gum.Wireframe`.

For a working project with a larger demo (`Label`, `TextBox`, `CheckBox`, and `ListBox` in a `StackPanel`), see the Gum Stride sample:

{% embed url="https://github.com/vchelaru/Gum/tree/main/Samples/StrideGum" %}
