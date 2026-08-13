# SkiaSharp (General Canvas)

## Introduction

This page is for projects that render with SkiaSharp but don't fit one of Gum's dedicated Skia hosts (.NET MAUI, WPF, or Silk.NET). It assumes you already have your own `SKCanvas` rendering in your project, gotten however your platform requires. Skia itself is just a 2D graphics API, not a windowing or input system, so beyond "get an `SKCanvas` on screen," Gum can't offer platform-specific setup guidance here, that part is entirely up to your own project.

{% hint style="info" %}
If your project is .NET MAUI, WPF, or Silk.NET, use that page instead: [.NET MAUI](.net-maui.md), [WPF](wpf.md), [Silk.NET](silk.net.md). Each of those wires up a canvas view (or window) for you, and Silk.NET also wires up real mouse/keyboard input. This page is for everything else.
{% endhint %}

{% hint style="warning" %}
This setup is rendering and layout only. There is no supported way to wire mouse, keyboard, or Forms control interactivity (hover, click, focus) here, not even as DIY glue code. If you need interactive Forms controls on SkiaSharp, use the Silk.NET, MAUI, or WPF setup instead, each of which has a real input story.
{% endhint %}

{% hint style="info" %}
A fuller, Forms-integrated Skia standalone setup is planned. This page will be updated once that lands; until then it stays intentionally minimal, with no dependency on Gum's Forms code.
{% endhint %}

## Adding Gum NuGet package

The easiest way to add Gum to your project is to use the NuGet package. Open your project in your preferred IDE, or add Gum through the command line.

Add the Gum.SkiaSharp NuGet package ([https://www.nuget.org/packages/Gum.SkiaSharp](https://www.nuget.org/packages/Gum.SkiaSharp))

Modify csproj:

```xml
<PackageReference Include="Gum.SkiaSharp" />
```

Or add through command line:

```bash
dotnet add package Gum.SkiaSharp
```

{% hint style="warning" %}
Don't name your project `SkiaGum`, that's the assembly name inside the `Gum.SkiaSharp` package. A same-named project produces a same-named output DLL that silently overwrites the runtime's copy in your `bin` folder, causing a `TypeLoadException` at runtime with no build warning. Gum's build now catches this for you: if your `AssemblyName` collides, the build fails with an error telling you to change it.
{% endhint %}

## Adding Source (Optional)

You can directly link your project to source instead of a NuGet package for improved debuggability, access to fixes and features before NuGet packages are published, or if you are interested in contributing.

To add source, first clone the Gum repository: [https://github.com/vchelaru/Gum](https://github.com/vchelaru/Gum)

If you have already added the Gum NuGet package to your project, remove it.

Add the following project to your solution:

* \<Gum Root>/Runtimes/SkiaGum/SkiaGum.csproj

`SkiaGum.csproj` already references `GumCommon` itself, so you do not need to add `GumCommon` separately.

Next, add SkiaGum as a project reference in your game project. Your project might look like this depending on the location of the Gum repository relative to your game project:

```xml
<ProjectReference Include="..\Gum\Runtimes\SkiaGum\SkiaGum.csproj" />
```

## Initializing Gum

`Gum.SkiaSharp` is a rendering and layout library only, it does not include a `GumService`, and this page doesn't point at one either. Write a small initialization function directly in your own project instead, no Gum-owned file to copy or keep in sync:

```csharp
// Class scope
InteractiveGue root = null!;
double previousTotalSeconds;
```

```csharp
void InitializeGum(SKCanvas canvas, int width, int height, string? gumProjectFile = null)
{
    SystemManagers.Default = new SystemManagers();
    SystemManagers.Default.Canvas = canvas;
    SystemManagers.Default.Initialize();
    SystemManagers.Default.Renderer.ClearsCanvas = false;

    if (!string.IsNullOrEmpty(gumProjectFile))
    {
        var gumProject = GumProjectSave.Load(gumProjectFile);
        ObjectFinder.Self.GumProjectSave = gumProject;
        gumProject.Initialize();

        var absolutePath = FileManager.IsRelative(gumProjectFile)
            ? FileManager.MakeAbsolute(gumProjectFile)
            : gumProjectFile;
        FileManager.RelativeDirectory = FileManager.GetDirectory(absolutePath);
    }

    GraphicalUiElement.CanvasWidth = width;
    GraphicalUiElement.CanvasHeight = height;

    root = new ContainerRuntime();
    root.AddToManagers(SystemManagers.Default);
}
```

Call it once you have an `SKCanvas`, passing a `.gumx` project path only if you're loading one (omit it for a code-only setup):

```csharp
// Initialize
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary;
using SkiaSharp;
using ToolsUtilities;

var bounds = canvas.DeviceClipBounds;
InitializeGum(canvas, bounds.Width, bounds.Height, "Content/GumProject/GumProject.gumx");
```

Each frame, animate and then draw:

```csharp
// Update
double delta = totalSecondsSinceStart - previousTotalSeconds;
previousTotalSeconds = totalSecondsSinceStart;
root.AnimateSelf(delta);
```

```csharp
// Draw
SystemManagers.Default.Draw();
```

Whenever your canvas is resized, tell Gum so layout re-runs against the new size:

```csharp
void HandleResize(int newWidth, int newHeight)
{
    GraphicalUiElement.CanvasWidth = newWidth;
    GraphicalUiElement.CanvasHeight = newHeight;
    root.UpdateLayout();
}
```

{% hint style="info" %}
Gum does not clear the canvas for you (`SystemManagers.Default.Renderer.ClearsCanvas` is set to `false` above), so your own draw code is expected to clear or paint the background before calling `SystemManagers.Default.Draw()`.
{% endhint %}

{% hint style="info" %}
There's no `AddToRoot()` extension method available here, that method resolves the active root through `IGumService.Default`, which this minimal setup doesn't implement. Add top-level elements with `root.Children.Add(...)` instead, as shown below.
{% endhint %}

## Adding Expression Support (Optional)

If your Gum project uses arithmetic expressions in variable references (such as `Width = OtherInstance.Width + 20`), you can add the `Gum.Expressions` NuGet package for full expression evaluation at runtime. Without this package, simple variable references like `Width = OtherInstance.Width` still work.

Add the NuGet package:

```bash
dotnet add package Gum.Expressions
```

Then call `GumExpressionService.Initialize()` after `InitializeGum`. Expression support is typically used with a Gum project that has variable references defined in the tool:

```csharp
// Initialize
InitializeGum(canvas, width, height, "Content/GumProject/GumProject.gumx");
GumExpressionService.Initialize();
```

If linking to source instead of NuGet, add `<Gum Root>/Runtimes/GumExpressions/GumExpressions.csproj` to your solution.

For more information, see the [Runtime Variable References](../../../styling/runtime-variable-references.md) page.

## Adding a Circle and Text (Testing the Setup)

Gum can be tested by adding a couple of renderables after Gum is initialized:

```csharp
// Initialize
InitializeGum(canvas, width, height);

var circle = new ColoredCircleRuntime();
circle.Color = SKColors.Red;
circle.Width = 200;
circle.Height = 200;
root.Children.Add(circle);

var text = new TextRuntime();
text.Text = "SkiaGum on a general canvas!";
text.Dock(Gum.Wireframe.Dock.Top);
root.Children.Add(text);
```

After adding these, trigger a redraw through whatever mechanism your host uses to repaint the canvas. You should see a red circle with text docked above it.
